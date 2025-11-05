//Author: Stefano Strozzo <strozzostefano@gmail.com>

using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Quiz_Task.Models
{
    /// <summary>
    /// Represents a question in a quiz
    /// </summary>
    public record Question
    {
        /// <summary>
        /// ID della domanda. Utilizzato come ID primario in MongoDB.
        /// </summary>
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; init; } = default!;

        /// <summary>
        /// ID del Test a cui appartiene la domanda.
        /// </summary>
        public string TestId { get; init; } = default!;

        /// <summary>
        /// Testo della domanda.
        /// </summary>
        public string Text { get; init; } = default!;

        /// <summary>
        /// Lista delle possibili opzioni di risposta per la domanda.
        /// </summary>
        public List<Option> Options { get; init; } = new List<Option>();
    }
}
