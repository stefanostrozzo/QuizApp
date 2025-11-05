//author: Stefano Strozzo <strozzostefano@gmail.com>

using System.Collections.Generic;

namespace Quiz_Task.Controllers.Utils
{
    /// <summary>
    /// DTO representing a single option, sanitized of the 'IsCorrect' flag.
    /// </summary>
    public record OptionDto
    {
        public string Id { get; init; } = default!;
        public string Text { get; init; } = default!;
    }

    /// <summary>
    /// DTO representing a Question to be sent to the client.
    /// It ensures that the correct answer is NOT included in the response.
    /// </summary>
    public record QuestionDto
    {
        /// <summary>
        /// The unique ID of the question.
        /// </summary>
        public string Id { get; init; } = default!;

        /// <summary>
        /// The text of the question.
        /// </summary>
        public string Text { get; init; } = default!;

        /// <summary>
        /// The sequence number of the current question in the test (e.g., 5th out of 10).
        /// Used for the progress bar.
        /// </summary>
        public int SequenceNumber { get; init; }

        /// <summary>
        /// The total number of questions in the test.
        /// </summary>
        public int TotalQuestions { get; init; }

        /// <summary>
        /// Sanitized list of options (without IsCorrect flag).
        /// </summary>
        public IReadOnlyList<OptionDto> Options { get; init; } = new List<OptionDto>();
    }
}