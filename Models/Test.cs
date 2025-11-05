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
        /// ID del Test. Utilizzato come ID primario in MongoDB.
        /// </summary>
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; init; } = default!;

        /// <summary>
        /// Titolo del Test.
        /// </summary>
        public string Title { get; init; } = default!;

        /// <summary>
        /// Descrizione del Test.
        /// </summary>
        public string Description { get; init; } = default!;

        /// <summary>
        /// Numero totale di domande nel test.
        /// </summary>
        public int TotalQuestions { get; init; }
    }
}
