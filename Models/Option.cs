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
        /// ID of the entity, used within a question.
        /// </summary>
        [BsonElement("Id")]
        public string Id { get; init; } = string.Empty;

        /// <summary>
        /// The text of the answer option.
        /// </summary>
        public string Text { get; init; } = default!;

        /// <summary>
        /// Boolean value that indicates if the option is correct.
        /// </summary>
        public bool IsCorrect { get; init; }
    }
}