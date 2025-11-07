// File: MongoTestRepository.cs

//author: Stefano Strozzo <strozzostefano@gmail.com>

using MongoDB.Driver;
using Quiz_Task.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson;
using System;

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
        public MongoTestRepository(MongoDbSettings settings)
        {
            var client = new MongoClient(settings.ConnectionString);
            var database = client.GetDatabase(settings.DatabaseName);

            // Initialize collections - use the correct names from settings
            _testsCollection = database.GetCollection<Test>(settings.TestsCollectionName);
            _questionsCollection = database.GetCollection<Question>(settings.QuestionsCollectionName);

            // --- BEST PRACTICE: Create Indexes for optimal queries ---
            try
            {
                var indexKeysTestId = Builders<Question>.IndexKeys
                    .Ascending(q => q.TestId);
                _questionsCollection.Indexes.CreateOne(new CreateIndexModel<Question>(indexKeysTestId));
            }
            catch (Exception ex)
            {
                // Note: Log this exception in a real environment
                Console.WriteLine($"Error creating index: {ex.Message}");
            }
        }

        // --- Implementation of ITestRepository methods ---

        /// <summary>
        /// Retrieves all tests asynchronously.
        /// </summary>
        public async Task<IReadOnlyList<Test>> GetAllTestsAsync(CancellationToken cancellationToken = default)
        {
            return await _testsCollection.Find(_ => true).ToListAsync(cancellationToken);
        }

        /// <summary>
        /// Retrieves all questions for a specific Test ID asynchronously.
        /// </summary>
        public async Task<IReadOnlyList<Question>> GetQuestionsByTestIdAsync(string testId, CancellationToken cancellationToken = default)
        {
            // Note: The original code handled potential ObjectId parsing errors, maintaining that robustness.
            try
            {
                // Convert string to ObjectId for comparison (Best Practice)
                var objectId = new ObjectId(testId);
                return await _questionsCollection.Find(q => q.TestId.Equals(testId)).ToListAsync(cancellationToken);
            }
            catch (Exception)
            {
                // Fallback: Try querying as a string (handling possible non-standard IDs)
                return await _questionsCollection.Find(q => q.TestId.ToString() == testId).ToListAsync(cancellationToken);
            }
        }

        /// <summary>
        /// Retrieves a specific Question by its ID asynchronously.
        /// </summary>
        public async Task<Question?> GetQuestionByIdAsync(string questionId, CancellationToken cancellationToken = default)
        {
            try
            {
                return await _questionsCollection
                    .Find(q => q.Id == questionId)
                    .FirstOrDefaultAsync(cancellationToken);
            }
            catch (Exception)
            {
                return null;
            }
        }

        /// <summary>
        /// Retrieves a specific Test by its ID asynchronously.
        /// </summary>
        public async Task<Test?> GetTestByIdAsync(string testId, CancellationToken cancellationToken = default)
        {
            try
            {
                var filter = Builders<Test>.Filter.Eq(t => t.Id, testId);
                return await _testsCollection.Find(filter).FirstOrDefaultAsync(cancellationToken);
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}