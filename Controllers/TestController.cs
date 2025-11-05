//author: Stefano Strozzo <strozzostefano@gmail.com>

using Microsoft.AspNetCore.Mvc;
using Quiz_Task.DataAccess;
using Quiz_Task.Models; // Necessario per StartSessionRequest, SubmitAnswerRequest, Answer, UserSession, Test
using Quiz_Task.Controllers.Utils; // Necessario per ApiResponse, QuestionDto, OptionDto
using System.Threading.Tasks;
using System.Threading;
using System.Collections.Generic;
using System.Linq;

namespace Quiz_Task.Controllers
{
    /// <summary>
    /// MVC Controller to handle both the frontend views (List, Question, Result) and the necessary API logic.
    /// </summary>
    // Rotta base: /Test
    [Route("[controller]")]
    // Eredita da Controller per supportare le View (HTML)
    public class TestController : Controller
    {
        // RISOLTO ERRORE 2: Usiamo i nomi originali e ci assicuriamo che vengano assegnati nel costruttore.
        private readonly ITestRepository _testRepository;
        private readonly IUserSessionRepository _sessionRepository;

        /// <summary>
        /// Constructor: Injects the required Repositories via Dependency Injection (DI).
        /// </summary>
        // RISOLTO ERRORE 2: Il costruttore assegna tutti i campi readonly non nullable.
        public TestController(ITestRepository testRepository, IUserSessionRepository sessionRepository)
        {
            _testRepository = testRepository;
            _sessionRepository = sessionRepository;
        }

        // ----------------------------------------------------------------------
        // 1. VIEW: GET /Test/List (Homepage - Ritorna HTML)
        // ----------------------------------------------------------------------

        /// <summary>
        /// Retrieves all available tests and passes them to the List View (Homepage).
        /// </summary>
        [HttpGet("List")]
        public async Task<IActionResult> List(CancellationToken cancellationToken)
        {
            try
            {
                // Usa il metodo asincrono
                var tests = await _testRepository.GetAllTestsAsync(cancellationToken);

                // Restituisce la View 'Views/Test/List.cshtml'
                return View(tests.ToList());
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"An error occurred while retrieving tests: {ex.Message}");
            }
        }

        // ----------------------------------------------------------------------
        // 2. API: POST /Test/start-session (Logica API - Ritorna JSON)
        // ----------------------------------------------------------------------

        /// <summary>
        /// Handles the request to start a new quiz session.
        /// </summary>
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
                // 1. Verifica che il Test esista
                var allTests = await _testRepository.GetAllTestsAsync(cancellationToken);
                if (!allTests.Any(t => t.Id == request.TestId))
                {
                    return NotFound(ApiResponse<string>.Fail($"Test with ID '{request.TestId}' not found."));
                }

                // 2. Crea il nuovo oggetto Sessione
                var newSession = new UserSession
                {
                    UserName = request.UserName,
                    TestId = request.TestId,
                    StartTime = DateTime.UtcNow,
                    Score = 0,
                    Answers = new List<Answer>()
                };

                // 3. Salva la sessione nel DB in modo asincrono
                var sessionId = await _sessionRepository.CreateSessionAsync(newSession, cancellationToken);

                // 4. Restituisce il risultato JSON
                return StatusCode(201, Json(ApiResponse<string>.Ok(sessionId, "Session successfully created.")));
            }
            catch (Exception ex)
            {
                return StatusCode(500, Json(ApiResponse<string>.Fail("An internal server error occurred during session creation.", new List<string> { ex.Message })));
            }
        }

        // ----------------------------------------------------------------------
        // 3. API: POST /Test/session/{sessionId}/answer (Logica API - Ritorna JSON)
        // ----------------------------------------------------------------------

        /// <summary>
        /// Records the user's answer for the current question and updates the session.
        /// </summary>
        [HttpPost("session/{sessionId}/answer")]
        public async Task<IActionResult> SubmitAnswer(string sessionId, [FromBody] SubmitAnswerRequest request, CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                return BadRequest(Json(ApiResponse<bool>.Fail("Invalid input data.", errors)));
            }

            try
            {
                // 1. Recupera la sessione
                var session = await _sessionRepository.GetSessionByIdAsync(sessionId, cancellationToken);
                if (session == null)
                {
                    return NotFound(Json(ApiResponse<bool>.Fail($"Session with ID '{sessionId}' not found.")));
                }

                if (session.EndTime.HasValue)
                {
                    return BadRequest(Json(ApiResponse<bool>.Fail("The session is already completed. Cannot submit new answers.")));
                }

                // 2. Controlla se l'utente ha già risposto a questa domanda
                if (session.Answers.Any(a => a.QuestionId == request.QuestionId))
                {
                    return BadRequest(Json(ApiResponse<bool>.Fail($"Question with ID '{request.QuestionId}' has already been answered.")));
                }

                // 3. Recupera la domanda COMPLETA dal DB per la verifica (Usa il tipo nullable Question?)
                Question? fullQuestion = await _testRepository.GetQuestionByIdAsync(request.QuestionId, cancellationToken);
                if (fullQuestion == null)
                {
                    // RISOLTO ERRORE 3: La funzione GetQuestionByIdAsync è definita con Question? (nullable), gestiamo null qui.
                    return NotFound(Json(ApiResponse<bool>.Fail($"Question with ID '{request.QuestionId}' not found.")));
                }

                // 4. Trova l'opzione selezionata
                var selectedOption = fullQuestion.Options.FirstOrDefault(o => o.Id == request.SelectedOptionId);
                if (selectedOption == null)
                {
                    return BadRequest(Json(ApiResponse<bool>.Fail($"Selected Option ID '{request.SelectedOptionId}' is invalid for Question ID '{request.QuestionId}'.")));
                }

                // 5. Verifica la correttezza
                bool isCorrect = selectedOption.IsCorrect;

                // 6. Aggiungi la risposta alla sessione e aggiorna nel DB
                var newAnswer = new Answer
                {
                    QuestionId = request.QuestionId,
                    SelectedOptionId = request.SelectedOptionId,
                    IsCorrect = isCorrect
                };
                session.Answers.Add(newAnswer);

                await _sessionRepository.UpdateSessionAsync(session, cancellationToken);

                // 7. Restituisce il risultato
                return Ok(Json(ApiResponse<bool>.Ok(isCorrect, "Answer submitted successfully.")));
            }
            catch (Exception ex)
            {
                return StatusCode(500, Json(ApiResponse<bool>.Fail("An internal server error occurred while submitting the answer.", new List<string> { ex.Message })));
            }
        }

        // ----------------------------------------------------------------------
        // 4. API: GET /Test/session/{sessionId}/next-question (Logica API - Ritorna JSON)
        // ----------------------------------------------------------------------

        /// <summary>
        /// Retrieves the next unanswered question for the given session.
        /// </summary>
        [HttpGet("session/{sessionId}/next-question")]
        public async Task<IActionResult> GetNextQuestion(string sessionId, CancellationToken cancellationToken)
        {
            try
            {
                var session = await _sessionRepository.GetSessionByIdAsync(sessionId, cancellationToken);
                if (session == null)
                {
                    return NotFound(Json(ApiResponse<QuestionDto>.Fail($"Session with ID '{sessionId}' not found.")));
                }

                if (session.EndTime.HasValue)
                {
                    return Ok(Json(ApiResponse<string>.Ok(session.Id, "Session completed. Proceed to results.")));
                }

                // 1. Recupera tutte le domande per il Test della sessione
                var allQuestions = await _testRepository.GetQuestionsByTestIdAsync(session.TestId, cancellationToken);
                var answeredQuestionIds = session.Answers.Select(a => a.QuestionId).ToHashSet();

                // 2. Trova la prossima domanda da rispondere
                var nextQuestion = allQuestions
                    .Where(q => !answeredQuestionIds.Contains(q.Id))
                    .OrderBy(q => q.Text)
                    .FirstOrDefault();

                // 3. Verifica se il quiz è terminato
                if (nextQuestion == null)
                {
                    // Finalizza la sessione (calcola punteggio e imposta EndTime)
                    await FinalizeSession(session);

                    // Ritorna l'ID della sessione per permettere al frontend di reindirizzare ai risultati
                    return Ok(Json(ApiResponse<string>.Ok(session.Id, "Quiz finished. Proceed to final results.")));
                }

                // 4. Mappa e restituisce la domanda (rimuovendo le risposte corrette)
                int sequenceNumber = session.Answers.Count + 1;
                int totalQuestions = allQuestions.Count;
                var questionDto = MapToQuestionDto(nextQuestion, sequenceNumber, totalQuestions);

                return Ok(Json(ApiResponse<QuestionDto>.Ok(questionDto)));
            }
            catch (Exception ex)
            {
                return StatusCode(500, Json(ApiResponse<QuestionDto>.Fail("An internal server error occurred while retrieving the next question.", new List<string> { ex.Message })));
            }
        }

        // ----------------------------------------------------------------------
        // HELPER METHOD 1: Mapping Question -> QuestionDto (Sanitizzazione)
        // ----------------------------------------------------------------------

        /// <summary>
        /// Maps the Question entity to a QuestionDto, intentionally omitting the IsCorrect flag from options.
        /// </summary>
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
        /// </summary>
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