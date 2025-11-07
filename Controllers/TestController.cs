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
        /// Retrieves all available tests and passes them to the List View (Homepage).
        /// </summary>
        [HttpGet("List")]
        public async Task<IActionResult> List(CancellationToken cancellationToken)
        {
            try
            {
                // Use the asynchronous method
                var tests = await _testRepository.GetAllTestsAsync(cancellationToken);

                // Returns the 'Views/Test/List.cshtml' View
                return View(tests.ToList());
            }
            catch (Exception ex)
            {
                // Log the error and return an internal server error status
                // In a real application, logging (e.g., ILogger) should be used here.
                return StatusCode(500, $"An error occurred while retrieving tests: {ex.Message}");
            }
        }

        // ----------------------------------------------------------------------
        // 2. API: POST /Test/start-session (API Logic - Returns JSON)
        // ----------------------------------------------------------------------

        /// <summary>
        /// Handles the request to start a new quiz session.
        /// </summary>
        /// <param name="request">The request body containing UserName and TestId.</param>
        /// <param name="cancellationToken">Token to cancel the operation.</param>
        /// <returns>JSON response with the new SessionId on success.</returns>
        [HttpPost("start-session")]
        public async Task<IActionResult> StartSession([FromBody] StartSessionRequest request, CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                return BadRequest(ApiResponse<string>.Fail("Invalid input data.", errors));
            }

            try
            {
                // 1. Verify that the Test exists
                var allTests = await _testRepository.GetAllTestsAsync(cancellationToken);
                if (!allTests.Any(t => t.Id == request.TestId))
                {
                    return NotFound(ApiResponse<string>.Fail($"Test with ID '{request.TestId}' not found."));
                }

                // 2. Create the new Session object
                var newSession = new UserSession
                {
                    UserName = request.UserName,
                    TestId = request.TestId,
                    StartTime = DateTime.UtcNow,
                    Score = 0,
                    Answers = new List<Answer>()
                };

                // 3. Save the session to the DB asynchronously
                var sessionId = await _sessionRepository.CreateSessionAsync(newSession, cancellationToken);

                // 4. Return the JSON result (Use Ok or Created)
                // Removed redundant Json(...) call
                return Created($"/Test/Session/{sessionId}/Question", ApiResponse<string>.Ok(sessionId, "Session successfully created."));
            }
            catch (Exception ex)
            {
                // Removed redundant Json(...) call
                return StatusCode(500, ApiResponse<string>.Fail("An internal server error occurred during session creation.", new List<string> { ex.Message }));
            }
        }

        /// <summary>
        /// Data Transfer Object for the StartSession request.
        /// </summary>
        public record StartSessionRequest(
            string UserName,
            string TestId
        );

        // ----------------------------------------------------------------------
        // 3. API: POST /Test/SubmitAnswer (API Logic - Returns JSON)
        // ----------------------------------------------------------------------

        /// <summary>
        /// Records the user's answer for the current question and updates the session.
        /// </summary>
        /// <param name="request">The request body containing the SessionId, QuestionId, and SelectedOptionId.</param>
        /// <param name="cancellationToken">Token to cancel the operation.</param>
        /// <returns>JSON response containing the next QuestionId or "RESULT".</returns>
        [HttpPost("SubmitAnswer")]
        public async Task<IActionResult> SubmitAnswer([FromBody] SubmitAnswerRequest request, CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                // Removed redundant Json(...) call
                return BadRequest(ApiResponse<bool>.Fail("Invalid input data.", errors));
            }

            try
            {
                // 1. Retrieve the session
                var session = await _sessionRepository.GetSessionByIdAsync(request.SessionId, cancellationToken);
                if (session == null)
                {
                    // Removed redundant Json(...) call
                    return NotFound(ApiResponse<bool>.Fail($"Session with ID '{request.SessionId}' not found."));
                }

                if (session.EndTime.HasValue)
                {
                    // Removed redundant Json(...) call
                    return BadRequest(ApiResponse<bool>.Fail("The session is already completed. Cannot submit new answers."));
                }

                // 2. Check if the user has already answered this question
                if (session.Answers.Any(a => a.QuestionId == request.QuestionId))
                {
                    // Removed redundant Json(...) call
                    return BadRequest(ApiResponse<bool>.Fail($"Question with ID '{request.QuestionId}' has already been answered."));
                }

                // 3. Retrieve the FULL question from the DB for verification
                Question? fullQuestion = await _testRepository.GetQuestionByIdAsync(request.QuestionId, cancellationToken);
                if (fullQuestion == null)
                {
                    // Removed redundant Json(...) call
                    return NotFound(ApiResponse<bool>.Fail($"Question with ID '{request.QuestionId}' not found."));
                }

                // 4. Find the selected option
                var selectedOption = fullQuestion.Options.FirstOrDefault(o => o.Id == request.SelectedOptionId);
                if (selectedOption == null)
                {
                    // Removed redundant Json(...) call
                    return BadRequest(ApiResponse<bool>.Fail($"Selected Option ID '{request.SelectedOptionId}' is invalid for Question ID '{request.QuestionId}' or the question has no options."));
                }

                // 5. Verify correctness
                bool isCorrect = selectedOption.IsCorrect;

                // 6. Add the answer to the session and update the DB
                var newAnswer = new Answer
                {
                    QuestionId = request.QuestionId,
                    SelectedOptionId = request.SelectedOptionId,
                    IsCorrect = isCorrect
                };
                session.Answers.Add(newAnswer);

                await _sessionRepository.UpdateSessionAsync(session, cancellationToken);

                // 7. Get the next question or indicate that the quiz is finished
                // IMPORTANT: The question order here must be consistent with the Question View logic (step 4 in Question method)
                var allQuestions = await _testRepository.GetQuestionsByTestIdAsync(session.TestId, cancellationToken);

                // Get the next question based on the count of already answered questions
                var nextQuestionIndex = session.Answers.Count; // This will be the index of the question to show next
                var nextQuestion = allQuestions.ElementAtOrDefault(nextQuestionIndex);

                string nextStep;
                if (nextQuestion == null)
                {
                    // Quiz finished
                    await FinalizeSession(session);
                    nextStep = "RESULT"; // Signal the client to redirect to the results page
                }
                else
                {
                    // Next question ID (although the Question view doesn't technically use this ID in the URL,
                    // we return it as confirmation/data, but the client must redirect to the general Question route).
                    nextStep = nextQuestion.Id;
                }

                // 8. Return the result with the next action
                // Removed redundant Json(...) call
                return Ok(ApiResponse<string>.Ok(nextStep, "Answer submitted successfully."));
            }
            catch (Exception ex)
            {
                // Removed redundant Json(...) call
                return StatusCode(500, ApiResponse<bool>.Fail("An internal server error occurred while submitting the answer.", new List<string> { ex.Message }));
            }
        }

        /// <summary>
        /// Data Transfer Object for the SubmitAnswer request.
        /// </summary>
        public record SubmitAnswerRequest(
            string SessionId,
            string QuestionId,
            string SelectedOptionId
        );

        // ----------------------------------------------------------------------
        // 4. API: GET /Test/session/{sessionId}/next-question (API Logic - Returns JSON)
        // ----------------------------------------------------------------------

        // Note: This API endpoint is redundant because the MVC action below serves the same purpose
        // and returns the QuestionDto directly to the view. I'm keeping it for completeness if you
        // planned to use an AJAX-only flow, but for your current setup, the MVC action (6) is used.

        /// <summary>
        /// Retrieves the next unanswered question for the given session.
        /// </summary>
        /// <param name="sessionId">The ID of the user session.</param>
        /// <param name="cancellationToken">Token to cancel the operation.</param>
        /// <returns>JSON response with the next QuestionDto or a signal for results.</returns>
        [HttpGet("session/{sessionId}/next-question")]
        public async Task<IActionResult> GetNextQuestion(string sessionId, CancellationToken cancellationToken)
        {
            try
            {
                var session = await _sessionRepository.GetSessionByIdAsync(sessionId, cancellationToken);
                if (session == null)
                {
                    // Removed redundant Json(...) call
                    return NotFound(ApiResponse<QuestionDto>.Fail($"Session with ID '{sessionId}' not found."));
                }

                if (session.EndTime.HasValue)
                {
                    // Removed redundant Json(...) call
                    return Ok(ApiResponse<string>.Ok(session.Id, "Session completed. Proceed to results."));
                }

                // 1. Retrieve all questions for the session's Test
                var allQuestions = await _testRepository.GetQuestionsByTestIdAsync(session.TestId, cancellationToken);

                // 2. Find the next question to be answered (based on sequential order)
                var currentQuestionIndex = session.Answers.Count;
                var nextQuestion = allQuestions.ElementAtOrDefault(currentQuestionIndex);

                // 3. Check if the quiz is finished
                if (nextQuestion == null)
                {
                    // Finalize the session (calculate score and set EndTime)
                    await FinalizeSession(session);

                    // Return the session ID to allow the frontend to redirect to the results
                    // Removed redundant Json(...) call
                    return Ok(ApiResponse<string>.Ok(session.Id, "Quiz finished. Proceed to final results."));
                }

                // 4. Map and return the question (removing correct answers)
                int sequenceNumber = currentQuestionIndex + 1;
                int totalQuestions = allQuestions.Count;
                var questionDto = MapToQuestionDto(nextQuestion, sequenceNumber, totalQuestions);

                // Removed redundant Json(...) call
                return Ok(ApiResponse<QuestionDto>.Ok(questionDto));
            }
            catch (Exception ex)
            {
                // Removed redundant Json(...) call
                return StatusCode(500, ApiResponse<QuestionDto>.Fail("An internal server error occurred while retrieving the next question.", new List<string> { ex.Message }));
            }
        }

        // ----------------------------------------------------------------------
        // 5. VIEW: GET /Test/Result
        // ----------------------------------------------------------------------

        /// <summary>
        /// Displays the final result page for a completed session.
        /// </summary>
        /// <param name="sessionId">The ID of the completed user session.</param>
        /// <returns>The Result View with the final session data.</returns>
        [HttpGet("Result")]
        public async Task<IActionResult> Result(string sessionId, CancellationToken cancellationToken)
        {
            var session = await _sessionRepository.GetSessionByIdAsync(sessionId, cancellationToken);

            if (session == null)
            {
                return NotFound($"Session with ID '{sessionId}' not found.");
            }

            if (!session.EndTime.HasValue)
            {
                // If the test hasn't been finalized yet, do it now (e.g., if the user jumped directly to /Result)
                await FinalizeSession(session);
            }

            // In a real app, you would fetch Test details to show total questions, etc.
            // For now, we only pass the session object to the view.
            return View(session);
        }

        // ----------------------------------------------------------------------
        // 6. VIEW: GET /Test/Session/{sessionId}/Question
        // ----------------------------------------------------------------------

        /// <summary>
        /// Loads the quiz view, determining the current question based on the number of answers given in the session.
        /// </summary>
        /// <param name="sessionId">The ID of the user session.</param>
        /// <param name="cancellationToken">Token to cancel the operation.</param>
        /// <returns>The Question View with the QuestionDto, or redirects to Result, or returns 404.</returns>
        [HttpGet("Session/{sessionId}/Question")]
        public async Task<IActionResult> Question(string sessionId, CancellationToken cancellationToken)
        {
            // 1. Retrieve the user session
            var session = await _sessionRepository.GetSessionByIdAsync(sessionId, cancellationToken);

            if (session == null)
            {
                // Session not found
                return NotFound($"Session with ID '{sessionId}' not found.");
            }

            // 2. Check if the test is already finished
            if (session.EndTime.HasValue)
            {
                return RedirectToAction("Result", new { sessionId });
            }

            // 3. Retrieve all questions for the session's Test
            // Questions must be sorted consistently (e.g., by ID, SequenceNumber, or Text)
            var allQuestions = await _testRepository.GetQuestionsByTestIdAsync(session.TestId, cancellationToken);

            if (allQuestions == null || !allQuestions.Any())
            {
                return NotFound($"Test with ID '{session.TestId}' found, but no questions are available.");
            }

            // 4. Determine which question to display
            // The index is based on how many answers have been submitted.
            var currentQuestionIndex = session.Answers.Count;
            var questionToShow = allQuestions.ElementAtOrDefault(currentQuestionIndex);

            if (questionToShow == null)
            {
                // If there are no more questions, finalize the session and redirect to the result
                await FinalizeSession(session);
                return RedirectToAction("Result", new { sessionId });
            }

            // 5. Map the question to the DTO (QuestionDto) to hide the correct answer
            var questionDto = MapToQuestionDto(
                questionToShow,
                currentQuestionIndex + 1, // Sequence is 1-based (Question 1 of N)
                allQuestions.Count
            );

            // 6. Return the View with the DTO as the Model
            return View(questionDto);
        }

        // ----------------------------------------------------------------------
        // HELPER METHOD 1: Mapping Question -> QuestionDto (Sanitization)
        // ----------------------------------------------------------------------

        /// <summary>
        /// Maps the Question entity to a QuestionDto, intentionally omitting the IsCorrect flag from options.
        /// </summary>
        /// <param name="question">The source Question entity.</param>
        /// <param name="sequenceNumber">The current sequence number of the question (e.g., 5).</param>
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