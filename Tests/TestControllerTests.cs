using Xunit;
using Moq;
using Microsoft.AspNetCore.Mvc;
using Quiz_Task.Controllers;
using Quiz_Task.DataAccess;
using Quiz_Task.Models;
using Quiz_Task.Controllers.Utils;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System;
using System.Threading;

// Note: Ensure you have a reference to the Quiz_Task project and the following NuGet packages:
// xunit, Moq, Microsoft.AspNetCore.Mvc.ViewFeatures

namespace Quiz_Task.Tests.Unit.Controllers
{
    // Rimosso MockUserSession per non alterare i modelli originali.

    /// <summary>
    /// Unit tests for the TestController, ensuring its logic is correct.
    /// The repositories (data access layer) are mocked to isolate the controller's logic.
    /// </summary>
    public class TestControllerTests
    {
        private readonly Mock<ITestRepository> _mockTestRepository;
        private readonly Mock<IUserSessionRepository> _mockSessionRepository;
        private readonly TestController _controller;

        // Common dummy data
        private readonly string TestId = "test-1";
        private readonly string Question1Id = "q-1";
        private readonly string Option1CorrectId = "opt-A";
        private readonly string SessionId = "sess-abc";

        public TestControllerTests()
        {
            // 1. Arrange: Initialize mocks and the controller with dependencies
            _mockTestRepository = new Mock<ITestRepository>();
            _mockSessionRepository = new Mock<IUserSessionRepository>();
            _controller = new TestController(_mockTestRepository.Object, _mockSessionRepository.Object);
        }

        private List<Question> GetMockQuestions()
        {
            return new List<Question>
            {
                new Question
                {
                    Id = Question1Id,
                    TestId = TestId,
                    Text = "What is 2+2?",
                    Options = new List<Option>
                    {
                        new Option { Id = Option1CorrectId, Text = "4", IsCorrect = true },
                        new Option { Id = "opt-B", Text = "3", IsCorrect = false }
                    }
                },
                new Question
                {
                    Id = "q-2",
                    TestId = TestId,
                    Text = "What is the capital of France?",
                    Options = new List<Option>
                    {
                        new Option { Id = "opt-C", Text = "Berlin", IsCorrect = false },
                        new Option { Id = "opt-D", Text = "Paris", IsCorrect = true }
                    }
                }
            };
        }

        // ----------------------------------------------------------------------
        // ACTION: List (GET /Test/List)
        // ----------------------------------------------------------------------

        /// <summary>
        /// Test: Checks if the List action correctly fetches all tests and passes them to the view.
        /// </summary>
        [Fact]
        public async Task List_ShouldReturnViewWithAllTests()
        {
            // Arrange
            var mockTests = new List<Test> { new Test { Id = TestId, Title = "Quiz 1", TotalQuestions = 2 } };
            _mockTestRepository.Setup(r => r.GetAllTestsAsync(It.IsAny<CancellationToken>()))
                               .ReturnsAsync(mockTests.AsReadOnly());

            // Act
            // Assuming List() does not take CancellationToken based on the lack of error, or handles it internally.
            var result = await _controller.List();

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsAssignableFrom<IReadOnlyList<Test>>(viewResult.Model);
            Assert.Single(model);
            Assert.Equal("Quiz 1", model.First().Title);
        }

        // ----------------------------------------------------------------------
        // API: StartSession (POST /Test/StartSession)
        // ----------------------------------------------------------------------

        /// <summary>
        /// Test: Checks the successful start of a new session.
        /// </summary>
        [Fact]
        public async Task StartSession_ShouldReturnSuccessWithSessionId_OnValidRequest()
        {
            // Arrange
            var request = new StartSessionRequest { UserName = "John Doe", TestId = TestId };
            var mockQuestions = GetMockQuestions();

            _mockTestRepository.Setup(r => r.GetQuestionsByTestIdAsync(TestId, It.IsAny<CancellationToken>()))
                               .ReturnsAsync(mockQuestions.AsReadOnly());

            // The repository should be called to create the session
            _mockSessionRepository.Setup(r => r.CreateSessionAsync(It.IsAny<UserSession>(), It.IsAny<CancellationToken>()))
                                  .ReturnsAsync(SessionId);

            // Act
            var result = await _controller.StartSession(request, CancellationToken.None);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var apiResponse = Assert.IsType<ApiResponse<string>>(okResult.Value);
            Assert.True(apiResponse.Success);
            Assert.Equal(SessionId, apiResponse.Data);

            // Verify that the session was created with the correct data
            _mockSessionRepository.Verify(
                r => r.CreateSessionAsync(
                    It.Is<UserSession>(s =>
                        s.UserName == request.UserName &&
                        s.TestId == request.TestId &&
                        s.Answers.Count == 0 &&
                        s.StartTime.HasValue),
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }

        /// <summary>
        /// Test: Checks validation failure when the model state is invalid.
        /// </summary>
        [Fact]
        public async Task StartSession_ShouldReturnValidationError_OnInvalidModelState()
        {
            // Arrange
            _controller.ModelState.AddModelError("UserName", "Required");
            var request = new StartSessionRequest { UserName = "", TestId = TestId };

            // Act
            var result = await _controller.StartSession(request, CancellationToken.None);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var apiResponse = Assert.IsType<ApiResponse<string>>(badRequestResult.Value);
            Assert.False(apiResponse.Success);
            Assert.Contains("Validation failed", apiResponse.Message);
            Assert.NotNull(apiResponse.Errors);
        }

        /// <summary>
        /// Test: Checks if an error is returned when no questions are found for the test.
        /// </summary>
        [Fact]
        public async Task StartSession_ShouldReturnError_WhenNoQuestionsFound()
        {
            // Arrange
            var request = new StartSessionRequest { UserName = "John Doe", TestId = TestId };
            _mockTestRepository.Setup(r => r.GetQuestionsByTestIdAsync(TestId, It.IsAny<CancellationToken>()))
                               .ReturnsAsync(new List<Question>().AsReadOnly()); // Empty list

            // Act
            var result = await _controller.StartSession(request, CancellationToken.None);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var apiResponse = Assert.IsType<ApiResponse<string>>(badRequestResult.Value);
            Assert.False(apiResponse.Success);
            Assert.Contains("No questions found", apiResponse.Message);
            _mockSessionRepository.Verify(r => r.CreateSessionAsync(It.IsAny<UserSession>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        // ----------------------------------------------------------------------
        // ACTION: Question (GET /Test/Session/{sessionId}/Question/{questionId})
        // ----------------------------------------------------------------------

        /// <summary>
        /// Test: Checks if the Question action correctly retrieves the question and returns the view.
        /// This also implicitly tests the private MapToQuestionDto helper method.
        /// </summary>
        [Fact]
        public async Task Question_ShouldReturnViewWithQuestionDto_ForValidSessionAndQuestion()
        {
            // Arrange
            var mockQuestions = GetMockQuestions();
            // UserSession non ha TotalQuestionsCount, ma il test può usare List<Question>.Count per simularlo
            var mockSession = new UserSession { Id = SessionId, TestId = TestId, Answers = new List<Answer>() };
            var mockTest = new Test { Id = TestId, TotalQuestions = mockQuestions.Count }; // Usiamo Test per il conteggio totale

            _mockSessionRepository.Setup(r => r.GetSessionByIdAsync(SessionId, It.IsAny<CancellationToken>()))
                                  .ReturnsAsync(mockSession);
            _mockTestRepository.Setup(r => r.GetQuestionsByTestIdAsync(TestId, It.IsAny<CancellationToken>()))
                               .ReturnsAsync(mockQuestions.AsReadOnly());
            _mockTestRepository.Setup(r => r.GetTestByIdAsync(TestId, It.IsAny<CancellationToken>())) // Mock per recuperare il conteggio
                               .ReturnsAsync(mockTest);

            // Act
            // Aggiunge CancellationToken.None per risolvere l'errore: Nessun overload del metodo 'Question' accetta 2 argomenti
            var result = await _controller.Question(SessionId, Question1Id, CancellationToken.None);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<QuestionDto>(viewResult.Model);

            // Questi assert sono validi se il controller recupera TotalQuestions dal modello Test
            Assert.Equal(Question1Id, model.Id);
            Assert.Equal(2, model.TotalQuestions);
            Assert.Equal(1, model.SequenceNumber); // First question (1-based index)
            Assert.Equal(2, model.Options.Count);
            Assert.Equal(Option1CorrectId, model.Options.First().Id);
        }

        /// <summary>
        /// Test: Checks for a redirect to the result page if the user tries to access a question 
        /// that has already been answered (meaning the session should be complete).
        /// Note: The actual check for question completion is done in the controller's logic 
        /// by comparing current answers count to total question count.
        /// </summary>
        [Fact]
        public async Task Question_ShouldRedirectToResult_IfQuizIsAlreadyCompleted()
        {
            // Arrange
            // Setup a UserSession object that is explicitly marked as completed.
            // The controller logic relies on EndTime being set to determine finalization status.
            var completedSession = new UserSession
            {
                Id = SessionId,
                TestId = TestId,
                UserName = "TestUser",
                StartTime = DateTime.UtcNow.AddHours(-1),
                // Crucial: Setting EndTime simulates the session completion process.
                EndTime = DateTime.UtcNow,

                // Define a mock Answer to satisfy the structure of a completed session.
                Answers = new List<Answer>
        {
            new Answer
            {
                QuestionId = Question1Id,
                SelectedOptionId = Option1CorrectId,
                IsCorrect = true
            }
        },
                Score = 1 // Set a score reflecting the answer count
            };

            // Configure the repository to return the pre-completed session.
            _mockSessionRepository.Setup(r => r.GetSessionByIdAsync(SessionId, It.IsAny<CancellationToken>()))
                                  .ReturnsAsync(completedSession);

            // Note: Dependencies like ITestRepository mocks are omitted as the guard clause 
            // for session completion should execute before any further data lookups.

            // Act
            // Attempt to access a question page with the completed session ID.
            var result = await _controller.Question(SessionId, Question1Id);

            // Assert
            // The controller MUST return a RedirectToActionResult to the Result action.
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);

            // Verify redirection target using nameof() for refactoring robustness.
            Assert.Equal(nameof(TestController.Result), redirectResult.ActionName);
            Assert.Equal(SessionId, redirectResult.RouteValues?["sessionId"]);
            Assert.Null(redirectResult.ControllerName); // Redirection is internal to the same controller
        }

        // ----------------------------------------------------------------------
        // API: SubmitAnswer (POST /Test/Session/{sessionId}/Answer)
        // ----------------------------------------------------------------------

        /// <summary>
        /// Test: Checks the submission of a correct answer and subsequent request for the next question.
        /// </summary>
        [Fact]
        public async Task SubmitAnswer_ShouldRecordCorrectAnswerAndReturnNextQuestionId()
        {
            // Arrange
            var mockQuestions = GetMockQuestions();
            var mockSession = new UserSession { Id = SessionId, TestId = TestId, Answers = new List<Answer>() };
            var mockTest = new Test { Id = TestId, TotalQuestions = mockQuestions.Count }; // Usiamo Test per il conteggio totale

            var request = new SubmitAnswerRequest { QuestionId = Question1Id, SelectedOptionId = Option1CorrectId };

            _mockSessionRepository.Setup(r => r.GetSessionByIdAsync(SessionId, It.IsAny<CancellationToken>()))
                                  .ReturnsAsync(mockSession);
            _mockTestRepository.Setup(r => r.GetQuestionsByTestIdAsync(TestId, It.IsAny<CancellationToken>()))
                               .ReturnsAsync(mockQuestions.AsReadOnly());
            _mockTestRepository.Setup(r => r.GetQuestionByIdAsync(Question1Id, It.IsAny<CancellationToken>()))
                               .ReturnsAsync(mockQuestions.First());
            _mockTestRepository.Setup(r => r.GetTestByIdAsync(TestId, It.IsAny<CancellationToken>()))
                               .ReturnsAsync(mockTest);

            // Act
            var result = await _controller.SubmitAnswer(SessionId, request, CancellationToken.None);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var apiResponse = Assert.IsType<ApiResponse<string>>(okResult.Value);

            // Check API response for the next question ID
            Assert.True(apiResponse.Success);
            Assert.Equal(mockQuestions[1].Id, apiResponse.Data); // Next question ID is q-2

            // Verify session update was called with the correct Answer
            _mockSessionRepository.Verify(
                r => r.UpdateSessionAsync(
                    It.Is<UserSession>(s =>
                        s.Answers.Count == 1 &&
                        s.Answers.First().QuestionId == Question1Id &&
                        s.Answers.First().IsCorrect == true),
                    It.IsAny<CancellationToken>()),
                Times.Once);

            // Verify FinalizeSession was NOT called since the quiz is not complete
            _mockSessionRepository.Verify(r => r.UpdateSessionAsync(It.Is<UserSession>(s => s.EndTime.HasValue), It.IsAny<CancellationToken>()), Times.Never);
        }

        /// <summary>
        /// Test: Checks the submission of a wrong answer, it should still proceed to the next question.
        /// </summary>
        [Fact]
        public async Task SubmitAnswer_ShouldRecordWrongAnswerAndReturnNextQuestionId()
        {
            // Arrange
            var mockQuestions = GetMockQuestions();
            var mockSession = new UserSession { Id = SessionId, TestId = TestId, Answers = new List<Answer>() };
            var mockTest = new Test { Id = TestId, TotalQuestions = mockQuestions.Count };

            // Selecting the wrong option 'opt-B'
            var request = new SubmitAnswerRequest { QuestionId = Question1Id, SelectedOptionId = "opt-B" };

            _mockSessionRepository.Setup(r => r.GetSessionByIdAsync(SessionId, It.IsAny<CancellationToken>()))
                                  .ReturnsAsync(mockSession);
            _mockTestRepository.Setup(r => r.GetQuestionsByTestIdAsync(TestId, It.IsAny<CancellationToken>()))
                               .ReturnsAsync(mockQuestions.AsReadOnly());
            _mockTestRepository.Setup(r => r.GetQuestionByIdAsync(Question1Id, It.IsAny<CancellationToken>()))
                               .ReturnsAsync(mockQuestions.First());
            _mockTestRepository.Setup(r => r.GetTestByIdAsync(TestId, It.IsAny<CancellationToken>()))
                               .ReturnsAsync(mockTest);

            // Act
            var result = await _controller.SubmitAnswer(SessionId, request, CancellationToken.None);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var apiResponse = Assert.IsType<ApiResponse<string>>(okResult.Value);

            // Check API response for the next question ID
            Assert.True(apiResponse.Success);
            Assert.Equal(mockQuestions[1].Id, apiResponse.Data); // Next question ID is q-2

            // Verify session update was called with the correct Answer
            _mockSessionRepository.Verify(
                r => r.UpdateSessionAsync(
                    It.Is<UserSession>(s =>
                        s.Answers.Count == 1 &&
                        s.Answers.First().QuestionId == Question1Id &&
                        s.Answers.First().IsCorrect == false), // Expect IsCorrect = false
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }

        /// <summary>
        /// Test: Checks quiz completion and FinalizeSession is called when the last answer is submitted.
        /// </summary>
        [Fact]
        public async Task SubmitAnswer_ShouldFinalizeSession_WhenLastAnswerIsSubmitted()
        {
            // Arrange
            var mockQuestions = GetMockQuestions();
            // Session starts with 1 answer already, submitting the second one will complete the quiz
            var initialAnswers = new List<Answer>
            {
                new Answer { QuestionId = "q-0", SelectedOptionId = "opt-Z", IsCorrect = true }
            };
            var mockSession = new UserSession
            {
                Id = SessionId,
                TestId = TestId,
                Answers = initialAnswers,
                StartTime = DateTime.UtcNow.AddMinutes(-5) // Needed for FinalizeSession logic
            };
            var mockTest = new Test { Id = TestId, TotalQuestions = 2 }; // Usiamo Test per il conteggio totale

            // The last answer is for question 'q-1', which is the second and final question (since TotalQuestions is 2)
            var request = new SubmitAnswerRequest { QuestionId = Question1Id, SelectedOptionId = Option1CorrectId };

            _mockSessionRepository.Setup(r => r.GetSessionByIdAsync(SessionId, It.IsAny<CancellationToken>()))
                                  .ReturnsAsync(mockSession);
            _mockTestRepository.Setup(r => r.GetQuestionsByTestIdAsync(TestId, It.IsAny<CancellationToken>()))
                               .ReturnsAsync(mockQuestions.AsReadOnly());
            _mockTestRepository.Setup(r => r.GetQuestionByIdAsync(Question1Id, It.IsAny<CancellationToken>()))
                               .ReturnsAsync(mockQuestions.First()); // Question q-1 details
            _mockTestRepository.Setup(r => r.GetTestByIdAsync(TestId, It.IsAny<CancellationToken>()))
                               .ReturnsAsync(mockTest);

            // Act
            var result = await _controller.SubmitAnswer(SessionId, request, CancellationToken.None);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var apiResponse = Assert.IsType<ApiResponse<string>>(okResult.Value);

            // Data should be empty or null (string.Empty for the next question ID when completed)
            Assert.True(apiResponse.Success);
            Assert.Equal(string.Empty, apiResponse.Data); // Empty string signals completion

            // Verify that UpdateSessionAsync was called for the finalization
            _mockSessionRepository.Verify(
                r => r.UpdateSessionAsync(
                    It.Is<UserSession>(s =>
                        s.EndTime.HasValue && // FinalizeSession logic check
                        s.Score == 2 &&
                        s.Answers.Count == 2), // Total answers is 2
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }

        // ----------------------------------------------------------------------
        // ACTION: Result (GET /Test/Session/{sessionId}/Result)
        // ----------------------------------------------------------------------

        /// <summary>
        /// Test: Checks if the Result action correctly retrieves the completed session and passes it to the view.
        /// </summary>
        [Fact]
        public async Task Result_ShouldReturnViewWithSession_ForValidCompletedSession()
        {
            // Arrange
            var mockSession = new UserSession
            {
                Id = SessionId,
                TestId = TestId,
                Answers = new List<Answer> { new Answer { IsCorrect = true } },
                StartTime = DateTime.UtcNow.AddMinutes(-1),
                EndTime = DateTime.UtcNow,
                Score = 1
            };

            _mockSessionRepository.Setup(r => r.GetSessionByIdAsync(SessionId, It.IsAny<CancellationToken>()))
                                  .ReturnsAsync(mockSession);

            // Act
            var result = await _controller.Result(SessionId);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<UserSession>(viewResult.Model);
            Assert.Equal(SessionId, model.Id);
            Assert.True(model.EndTime.HasValue);
        }

        /// <summary>
        /// Test: Checks for a NotFound result if the session ID is invalid.
        /// </summary>
        [Fact]
        public async Task Result_ShouldReturnNotFound_ForInvalidSessionId()
        {
            // Arrange
            _mockSessionRepository.Setup(r => r.GetSessionByIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                                  .ReturnsAsync((UserSession?)null); // Session not found

            // Act
            var result = await _controller.Result("invalid-id");

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }
    }
}