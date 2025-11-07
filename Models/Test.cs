//Author: Stefano Strozzo <strozzostefano@gmail.com>

using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Quiz_Task.Models
{
    /// <summary>
    /// Represents a Test/Quiz entity
    /// </summary>
    public record Test
    {
        /// <summary>
        /// The ID of the Test. Used as the primary ID in MongoDB.
        /// </summary>
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; init; } = default!;

        /// <summary>
        /// Title of the Test.
        /// </summary>
        public string Title { get; init; } = default!;

        /// <summary>
        /// Total number of questions in the test.
        /// </summary>
        public int TotalQuestions { get; init; }
    }
}