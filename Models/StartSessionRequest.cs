//author: Stefano Strozzo <strozzostefano@gmail.com>

using System.ComponentModel.DataAnnotations;

namespace Quiz_Task.Models
{
    /// <summary>
    /// DTO to handle the input when a user starts a new session.
    /// </summary>
    public record StartSessionRequest
    {
        /// <summary>
        /// User's name provided at the start of the quiz.
        /// </summary>
        [Required(ErrorMessage = "User name is required.")]
        public string UserName { get; init; } = default!;

        /// <summary>
        /// ID of the test selected by the user.
        /// </summary>
        [Required(ErrorMessage = "Test ID is required.")]
        public string TestId { get; init; } = default!;
    }
}