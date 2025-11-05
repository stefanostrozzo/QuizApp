//Author: Stefano Strozzo <strozzostefano@gmail.com>

namespace Quiz_Task.Models
{
    /// <summary>
    /// Represents the settings required to connect to a MongoDB database
    /// </summary>
    public class MongoDbSettings
    {
        /// <summary>
        /// Connection string to connect to the MongoDB database
        /// </summary>
        public string ConnectionString { get; set; }

        /// <summary>
        /// Database name within the MongoDB server
        /// </summary>
        public string DatabaseName { get; set; }
    }
}
