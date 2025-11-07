//Author: Stefano Strozzo <strozzostefano@gmail.com>

using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.Collections.Generic; // Added for explicit List<Option> type

namespace Quiz_Task.Models
{
    /// <summary>
    /// Represents a question in a quiz
    /// </summary>
    public record Question
    {
        /// <summary>
        /// The ID of the question. Used as the primary ID in MongoDB.
        /// </summary>
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; init; } = default!;

        /// <summary>
        /// The ID of the Test to which the question belongs.
        /// </summary>
        [BsonRepresentation(BsonType.ObjectId)]
        public string TestId { get; init; } = default!;

        /// <summary>
        /// The text of the question.
        /// </summary>
        public string Text { get; init; } = default!;

        /// <summary>
        /// List of possible answer options for the question.
        /// </summary>
        public List<Option> Options { get; init; } = new List<Option>();
    }
}