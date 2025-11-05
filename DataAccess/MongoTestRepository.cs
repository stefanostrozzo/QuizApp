//author: Stefano Strozzo <strozzostefano@gmail.com>

using MongoDB.Driver;
using Quiz_Task.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Quiz_Task.DataAccess
{
    /// <summary>
    /// MongoTestRepository implements ITestRepository to interact with MongoDB.
    /// </summary>
    public class MongoTestRepository : ITestRepository
    {
        private readonly IMongoCollection<Test> _testsCollection;
        private readonly IMongoCollection<Question> _questionsCollection;

        /// <summary>
        /// Initializes the repository and DB connection.
        /// </summary>
        /// <param name="settings">The MongoDbSettings object provided by DI.</param>
        public MongoTestRepository(MongoDbSettings settings) // Modificato per accettare MongoDbSettings
        {
            var client = new MongoClient(settings.ConnectionString);
            var database = client.GetDatabase(settings.DatabaseName);

            // Initialize collections
            _testsCollection = database.GetCollection<Test>("Tests");
            _questionsCollection = database.GetCollection<Question>("Questions");

            // --- BEST PRACTICE: Create Indexes for optimal queries (as per project requirement) ---

            // Index to efficiently retrieve questions by TestId, which is frequently queried.
            // This is crucial since "the database will 'grow' very large over time."
            var indexKeysTestId = Builders<Question>.IndexKeys.Ascending(q => q.TestId);
            _questionsCollection.Indexes.CreateOne(new CreateIndexModel<Question>(indexKeysTestId));
        }

        /// <summary>
        /// Retrieves all available tests from the data source asynchronously.
        /// </summary>
        public async Task<IReadOnlyList<Test>> GetAllTestsAsync(CancellationToken cancellationToken = default)
        {
            var tests = await _testsCollection.Find(_ => true)
                                              .ToListAsync(cancellationToken);

            return tests.AsReadOnly();
        }

        /// <summary>
        /// Retrieves questions for a specific Test ID asynchronously.
        /// </summary>
        public async Task<IReadOnlyList<Question>> GetQuestionsByTestIdAsync(string testId, CancellationToken cancellationToken = default)
        {
            var questions = await _questionsCollection.Find(q => q.TestId == testId)
                                                      .ToListAsync(cancellationToken);

            return questions.OrderBy(q => q.Text).ToList().AsReadOnly();
        }

        /// <summary>
        /// Retrieves a specific Question by its ID asynchronously
        /// </summary>
        public async Task<Question?> GetQuestionByIdAsync(string questionId, CancellationToken cancellationToken = default)
        {
            // FirstOrDefaultAsync returns the document or null if not found (handling C# Nullable Reference Types).
            return await _questionsCollection.Find(q => q.Id == questionId).FirstOrDefaultAsync(cancellationToken);
        }
    }
}