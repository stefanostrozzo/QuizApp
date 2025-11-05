//author: Stefano Strozzo <strozzostefano@gmail.com>

using System.ComponentModel.DataAnnotations;

namespace Quiz_Task.Models
{
    /// <summary>
    /// DTO to handle the input when a user submits an answer.
    /// </summary>
    public record SubmitAnswerRequest
    {
        /// <summary>
        /// ID of the question the user is answering.
        /// </summary>
        [Required(ErrorMessage = "Question ID is required.")]
        public string QuestionId { get; init; } = default!;

        /// <summary>
        /// ID of the option the user selected.
        /// </summary>
        [Required(ErrorMessage = "Selected Option ID is required.")]
        public string SelectedOptionId { get; init; } = default!;
    }
}