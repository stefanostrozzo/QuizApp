//author: Stefano Strozzo <strozzostefano@gmail.com>

using Microsoft.AspNetCore.Mvc;
using Quiz_Task.DataAccess;
// Required for StartSessionRequest, SubmitAnswerRequest, Answer, UserSession, Test, Question, Option
using Quiz_Task.Models;
// Required for ApiResponse, QuestionDto, OptionDto
using Quiz_Task.Controllers.Utils;
using System.Threading.Tasks;
using System.Threading;
using System.Collections.Generic;
using System.Linq;
using System; // Required for DateTime and Exceptions

namespace Quiz_Task.Controllers
{
    /// <summary>
    /// MVC Controller to handle both the frontend views (List, Question, Result) and the necessary API logic.
    /// </summary>
    // Base route: /Test
    [Route("[controller]")]
    // Inherits from Controller to support Views (HTML)
    public class TestController : Controller
    {
        private readonly ITestRepository _testRepository;
        private readonly IUserSessionRepository _sessionRepository;

        /// <summary>
        /// Constructor: Injects the required Repositories via Dependency Injection (DI).
        /// </summary>
        public TestController(ITestRepository testRepository, IUserSessionRepository sessionRepository)
        {
            _testRepository = testRepository;
            _sessionRepository = sessionRepository;
        }

        // ----------------------------------------------------------------------
        // 1. VIEW: GET /Test/List (Homepage - Returns HTML)
        // ----------------------------------------------------------------------

        /// <summary>
        /// Displays the list of all available quizzes/tests.
        /// </summary>
        [HttpGet("list")]
        public async Task<IActionResult> List(CancellationToken cancellationToken = default)
        {
            try
            {
                var tests = await _testRepository.GetAllTestsAsync(cancellationToken);
                return View(tests);
            }
            catch (Exception) // Removed unused variable 'ex'
            {
                // In a production application, error would be logged.
                // For simplicity, returning an empty list or error view.
                return View(new List<Test>());
            }
        }

        // ----------------------------------------------------------------------
        // 2. API: POST /Test/start (Starts a new quiz session - Returns JSON)
        // ----------------------------------------------------------------------

        /// <summary>
        /// API endpoint to start a new user session for a quiz.
        /// </summary>
        /// <param name="request">DTO containing UserName and TestId.</param>
        /// <returns>
        /// Success: ApiResponse<string> with the initial session ID and the first question ID.
        /// Failure: ApiResponse<string> with an error message.
        /// </returns>
        [HttpPost("start")]
        public async Task<IActionResult> StartSession([FromBody] StartSessionRequest request, CancellationToken cancellationToken = default)
        {
            if (!ModelState.IsValid)
            {
                // Return detailed validation errors
                var errors = ModelState.Values
                                       .SelectMany(v => v.Errors)
                                       .Select(e => e.ErrorMessage)
                                       .ToList();
                // Ensure ApiResponse<string> is used for the error response.
                return BadRequest(ApiResponse<string>.Fail("Validation failed.", errors)); 
            }

            var questions = (await _testRepository.GetQuestionsByTestIdAsync(request.TestId, cancellationToken)).ToList();

            if (questions == null || !questions.Any())
            {
                // FIX: Update message to include "No questions found" as expected by the unit test.
                return BadRequest(ApiResponse<string>.Fail("No questions found for the selected test.")); 
            }

            // Create a new session
            var newSession = new UserSession
            {
                UserName = request.UserName,
                TestId = request.TestId,
                StartTime = DateTime.UtcNow,
                Answers = new List<Answer>()
            };

            try
            {
                // Save the session to the database
                var sessionId = await _sessionRepository.CreateSessionAsync(newSession, cancellationToken);
                
                // FIX: Return the session ID in the Data field, as expected by the unit test.
                return Ok(ApiResponse<string>.Ok(sessionId, $"Session started successfully for {request.UserName}."));
            }
            catch (Exception) // Removed unused variable 'ex'
            {
                // In a production application, error would be logged.
                return StatusCode(500, ApiResponse<string>.Fail("An unexpected error occurred while starting the session."));
            }
        }

        // ----------------------------------------------------------------------
        // 3. VIEW: GET /Test/session/{sessionId}/question/{questionId?} (Returns HTML)
        // ----------------------------------------------------------------------

        /// <summary>
        /// Displays a specific question in the quiz session.
        /// If questionId is null, it tries to find the next unanswered question.
        /// </summary>
        [HttpGet("session/{sessionId}/question/{questionId?}")]
        public async Task<IActionResult> Question(string sessionId, string? questionId, CancellationToken cancellationToken = default)
        {
            var session = await _sessionRepository.GetSessionByIdAsync(sessionId, cancellationToken);
            
            if (session == null)
            {
                return NotFound();
            }

            // If the session is already completed, redirect to the result page.
            if (session.EndTime.HasValue)
            {
                return RedirectToAction(nameof(Result), new { sessionId = sessionId });
            }

            var allQuestions = (await _testRepository.GetQuestionsByTestIdAsync(session.TestId, cancellationToken)).ToList();
            if (!allQuestions.Any())
            {
                return View("Error", "Quiz is misconfigured: no questions found.");
            }

            // 1. Determine the target question ID
            string targetQuestionId = questionId ?? string.Empty;

            if (string.IsNullOrEmpty(targetQuestionId))
            {
                // Find the first question that hasn't been answered yet
                var answeredQuestionIds = session.Answers.Select(a => a.QuestionId).ToHashSet();
                var nextQuestion = allQuestions.FirstOrDefault(q => !answeredQuestionIds.Contains(q.Id));

                if (nextQuestion == null)
                {
                    // No more questions to answer: quiz is completed, finalize and redirect to result.
                    await FinalizeSession(session);
                    return RedirectToAction(nameof(Result), new { sessionId = sessionId });
                }

                targetQuestionId = nextQuestion.Id;
            }

            // 2. Retrieve the target question
            var question = allQuestions.FirstOrDefault(q => q.Id == targetQuestionId);

            if (question == null)
            {
                return NotFound($"Question with ID '{targetQuestionId}' not found in the test.");
            }

            // 3. Prepare the DTO for the view
            int sequenceNumber = allQuestions.FindIndex(q => q.Id == targetQuestionId) + 1;
            var questionDto = MapToQuestionDto(question, sequenceNumber, allQuestions.Count);

            return View(questionDto);
        }

        // ----------------------------------------------------------------------
        // 4. API: POST /Test/session/{sessionId}/answer (Submits an answer - Returns JSON)
        // ----------------------------------------------------------------------

        /// <summary>
        /// API endpoint to submit an answer for a question in a quiz session.
        /// </summary>
        /// <param name="sessionId">The ID of the active user session.</param>
        /// <param name="request">DTO containing QuestionId and SelectedOptionId.</param>
        /// <returns>
        /// Success: ApiResponse<string> with the ID of the next question, or "" if the quiz is finished.
        /// Failure: ApiResponse<string> with an error message.
        /// </returns>
        [HttpPost("session/{sessionId}/answer")]
        public async Task<IActionResult> SubmitAnswer(string sessionId, [FromBody] SubmitAnswerRequest request, CancellationToken cancellationToken = default)
        {
            var session = await _sessionRepository.GetSessionByIdAsync(sessionId, cancellationToken);
            if (session == null)
            {
                return NotFound(ApiResponse<string>.Fail("Session not found."));
            }

            // Check if already completed
            if (session.EndTime.HasValue)
            {
                return BadRequest(ApiResponse<string>.Fail("The quiz is already completed."));
            }

            // 1. Check if the question has already been answered in this session
            if (session.Answers.Any(a => a.QuestionId == request.QuestionId))
            {
                // This scenario might be a double submission or a client error.
                return BadRequest(ApiResponse<string>.Fail($"Question '{request.QuestionId}' has already been answered."));
            }

            // 2. Validate question and option IDs
            var question = await _testRepository.GetQuestionByIdAsync(request.QuestionId, cancellationToken);
            if (question == null || question.TestId != session.TestId)
            {
                return NotFound(ApiResponse<string>.Fail("Question not found in the associated test."));
            }

            var selectedOption = question.Options.FirstOrDefault(o => o.Id == request.SelectedOptionId);
            if (selectedOption == null)
            {
                return BadRequest(ApiResponse<string>.Fail("Selected option ID is invalid for this question."));
            }

            // 3. Save the new answer
            var newAnswer = new Answer
            {
                QuestionId = request.QuestionId,
                SelectedOptionId = request.SelectedOptionId,
                IsCorrect = selectedOption.IsCorrect
            };

            session.Answers.Add(newAnswer);

            // 4. Determine the next question
            var allQuestions = (await _testRepository.GetQuestionsByTestIdAsync(session.TestId, cancellationToken)).ToList();
            int totalQuestions = allQuestions.Count;
            int answeredCount = session.Answers.Count;
            
            string nextQuestionId = string.Empty;

            if (answeredCount >= totalQuestions)
            {
                // Finalize the session when the last question is answered. FinalizeSession will update the database.
                await FinalizeSession(session); 
                
                // nextQuestionId remains empty (""), signaling the client to redirect to the result page.
            }
            else
            {
                // Find the next question in the sequence
                var currentQuestionIndex = allQuestions.FindIndex(q => q.Id == request.QuestionId);
                var nextQuestion = allQuestions.Skip(currentQuestionIndex + 1).FirstOrDefault();

                // Fallback to the first unanswered question if sequential logic is lost (should not happen with proper client flow)
                if (nextQuestion == null)
                {
                    var answeredQuestionIds = session.Answers.Select(a => a.QuestionId).ToHashSet();
                    nextQuestion = allQuestions.FirstOrDefault(q => !answeredQuestionIds.Contains(q.Id));
                }

                if (nextQuestion != null)
                {
                    nextQuestionId = nextQuestion.Id;
                }
            }

            // 5. Update the session in the database
            // FIX: Only perform the update if the session was NOT just finalized by the block above.
            // FinalizeSession sets EndTime, so we check EndTime.
            if (!session.EndTime.HasValue)
            {
                await _sessionRepository.UpdateSessionAsync(session, cancellationToken);
            }
            
            // Return the ID of the next question
            return Ok(ApiResponse<string>.Ok(nextQuestionId, nextQuestionId == string.Empty 
                ? "Last answer submitted. Quiz finished." 
                : "Answer submitted successfully."));
        }

        // ----------------------------------------------------------------------
        // 5. VIEW: GET /Test/session/{sessionId}/result (Returns HTML)
        // ----------------------------------------------------------------------

        /// <summary>
        /// Displays the final results of a completed quiz session.
        /// </summary>
        /// <param name="sessionId">The ID of the completed session.</param>
        [HttpGet("session/{sessionId}/result")]
        public async Task<IActionResult> Result(string sessionId, CancellationToken cancellationToken = default)
        {
            var session = await _sessionRepository.GetSessionByIdAsync(sessionId, cancellationToken);

            if (session == null)
            {
                // Return NotFoundResult as expected by the unit test.
                return NotFound(); 
            }

            // For best user experience, ensure the session is finalized before showing results
            await FinalizeSession(session);

            return View(session);
        }

        // ----------------------------------------------------------------------
        // HELPER METHOD 1: Map to Question DTO (Hides correct option data)
        // ----------------------------------------------------------------------

        /// <summary>
        /// Converts the full Question entity to a public-facing QuestionDto, omitting the 'IsCorrect' flag for security.
        /// </summary>
        /// <param name="question">The Question entity from the database.</param>
        /// <param name="sequenceNumber">The question's sequential number in the test.</param>
        /// <param name="totalQuestions">The total number of questions in the test.</param>
        /// <returns>The sanitized QuestionDto.</returns>
        private QuestionDto MapToQuestionDto(Question question, int sequenceNumber, int totalQuestions)
        {
            var optionsDto = question.Options.Select(o => new OptionDto
            {
                Id = o.Id,
                Text = o.Text
            }).ToList().AsReadOnly();

            return new QuestionDto
            {
                Id = question.Id,
                Text = question.Text,
                Options = optionsDto,
                SequenceNumber = sequenceNumber,
                TotalQuestions = totalQuestions
            };
        }

        // ----------------------------------------------------------------------
        // HELPER METHOD 2: Finalize session
        // ----------------------------------------------------------------------

        /// <summary>
        /// Marks the session as completed by setting the EndTime and updates the final score.
        /// It only runs if EndTime is not already set.
        /// </summary>
        /// <param name="session">The UserSession object to finalize.</param>
        private async Task FinalizeSession(UserSession session)
        {
            if (!session.EndTime.HasValue)
            {
                session.EndTime = DateTime.UtcNow;
                session.Score = session.Answers.Count(a => a.IsCorrect);
                await _sessionRepository.UpdateSessionAsync(session);
            }
        }
    }
}