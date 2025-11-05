//Author: Stefano Strozzo <strozzostefano@gmail.com>

namespace Quiz_Task.Models
{
    /// <summary>
    /// Represents an answer to a quiz question.
    /// </summary>
    public record Answer
    {
        /// <summary>
        /// Gets the ID of the question being answered.
        /// </summary>
        public string QuestionId { get; init; } = default!;

        /// <summary>
        /// Gets the ID of the selected answer option.
        /// </summary>
        public string SelectedOptionId { get; init; } = default!;

        /// <summary>
        /// Boolean value that indicates if the selected answer is correct.
        /// </summary>
        public bool IsCorrect { get; init; }
    }
}