//author: Stefano Strozzo <strozzostefano@gmail.com>

using MongoDB.Driver;
using Quiz_Task.Models;
using System.Threading;
using System.Threading.Tasks;

namespace Quiz_Task.DataAccess
{
    /// <summary>
    /// MongoUserSessionRepository implements IUserSessionRepository using MongoDB as the data store.
    /// </summary>
    public class MongoUserSessionRepository : IUserSessionRepository
    {
        private readonly IMongoCollection<UserSession> _sessions;

        /// <summary>
        /// Initializes the repository and DB connection.
        /// </summary>
        /// <param name="settings">The MongoDbSettings object provided by DI.</param>
        public MongoUserSessionRepository(MongoDbSettings settings)
        {
            var client = new MongoClient(settings.ConnectionString);
            var database = client.GetDatabase(settings.DatabaseName);

            _sessions = database.GetCollection<UserSession>("UserSessions");

            //Create Indexes for performance optimization (as per project requirement)
            var indexKeysUserTest = Builders<UserSession>.IndexKeys
                .Ascending(s => s.UserName)
                .Ascending(s => s.TestId);
            _sessions.Indexes.CreateOne(new CreateIndexModel<UserSession>(indexKeysUserTest));

            // Index for queries searching final results (e.g., test statistics)
            var indexKeysEndTime = Builders<UserSession>.IndexKeys.Ascending(s => s.EndTime);
            _sessions.Indexes.CreateOne(new CreateIndexModel<UserSession>(indexKeysEndTime));
        }

        /// <summary>
        /// Creates a new user session in the data source asynchronously.
        /// </summary>
        public async Task<string> CreateSessionAsync(UserSession session, CancellationToken cancellationToken = default)
        {
            // InsertOneAsync is the async equivalent of InsertOne()
            await _sessions.InsertOneAsync(session, options: null, cancellationToken);
            return session.Id;
        }

        /// <summary>
        /// Retrieves a user session by its ID asynchronously.
        /// </summary>
        public async Task<UserSession?> GetSessionByIdAsync(string sessionId, CancellationToken cancellationToken = default)
        {
            var filter = Builders<UserSession>.Filter.Eq(s => s.Id, sessionId);
            // FirstOrDefaultAsync is used to retrieve a single document.
            return await _sessions.Find(filter).FirstOrDefaultAsync(cancellationToken);
        }

        /// <summary>
        /// Updates an existing user session in the database asynchronously.
        /// </summary>
        public async Task UpdateSessionAsync(UserSession session, CancellationToken cancellationToken = default)
        {
            var filter = Builders<UserSession>.Filter.Eq(s => s.Id, session.Id);

            // CORREZIONE: Passiamo una nuova istanza di ReplaceOptions (l'overload corretto), risolvendo l'ambiguità.
            // ReplaceOneAsync sostituisce l'intero documento.
            await _sessions.ReplaceOneAsync(filter, session, new ReplaceOptions(), cancellationToken);
        }
    }
}