//Author: Stefano Strozzo <strozzostefano@gmail.com>

namespace Quiz_Task.Models
{
    /// <summary>
    /// Represents an option for a quiz question
    /// </summary>
    public record Option
    {
        /// <summary>
        /// ID dell'entità, utilizzato all'interno di una domanda.
        /// </summary>
        public string Id { get; init; } = default!;

        /// <summary>
        /// Testo dell'opzione di risposta.
        /// </summary>
        public string Text { get; init; } = default!;

        /// <summary>
        /// Booleano che indica se l'opzione è corretta.
        /// </summary>
        public bool IsCorrect { get; init; }
    }
}
