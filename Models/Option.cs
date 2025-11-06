//Author: Stefano Strozzo <strozzostefano@gmail.com>

using MongoDB.Bson.Serialization.Attributes;

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
        [BsonElement("Id")]
        public string Id { get; init; } = string.Empty;

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
