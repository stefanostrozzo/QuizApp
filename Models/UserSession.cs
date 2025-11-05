//Author: Stefano Strozzo <strozzostefano@gmail.com>

using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Quiz_Task.Models
{
    /// <summary>
    /// Represents a user session for a quiz/test to monitor progress and results
    /// </summary>
    public class UserSession
    {
        /// <summary>
        /// Session ID
        /// </summary>
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        /// <summary>
        /// Username that was inserted
        /// </summary>
        public string UserName { get; set; }

        /// <summary>
        /// Selected Test ID
        /// </summary>
        public string TestId { get; set; }

        /// <summary>
        /// Optional start time of the session
        /// </summary>
        public DateTime? StartTime { get; set; }

        /// <summary>
        /// Optional end time of the session
        /// </summary>
        public DateTime? EndTime { get; set; }

        /// <summary>
        /// Score achieved in the test
        /// </summary>
        public int Score { get; set; }

        /// <summary>
        /// List of answers provided by the user during the session
        /// </summary>
        public List<Answer> Answers { get; set; } = new List<Answer>();
    }
}
