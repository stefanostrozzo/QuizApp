//author: Stefano Strozzo <strozzostefano@gmail.com>

using MongoDB.Driver;
using Quiz_Task.Models;
using System; // Aggiungi questo using per Console
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson;

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

            // Initialize collections - usa i nomi corretti dalle settings
            _testsCollection = database.GetCollection<Test>(settings.TestsCollectionName);
            _questionsCollection = database.GetCollection<Question>(settings.QuestionsCollectionName);

            // --- BEST PRACTICE: Create Indexes for optimal queries ---
            try
            {
                var indexKeysTestId = Builders<Question>.IndexKeys.Ascending(q => q.TestId);
                _questionsCollection.Indexes.CreateOne(new CreateIndexModel<Question>(indexKeysTestId));
                Console.WriteLine("✅ Index created successfully");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ Index creation warning: {ex.Message}");
                // Non blocchiamo l'app se l'indice esiste già
            }
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
            try
            {
                // Converti la stringa in ObjectId per il confronto
                var objectId = new ObjectId(testId);
                return await _questionsCollection.Find(q => q.TestId.Equals(testId)).ToListAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                // Se la conversione fallisce, prova come stringa
                Console.WriteLine($"Error converting TestId {testId}: {ex.Message}");
                return await _questionsCollection.Find(q => q.TestId.ToString() == testId).ToListAsync(cancellationToken);
            }
        }

        /// <summary>
        /// Retrieves a specific Question by its ID asynchronously
        /// </summary>
        public async Task<Question?> GetQuestionByIdAsync(string questionId, CancellationToken cancellationToken = default)
        {
            try
            {
                Console.WriteLine($"🔍 Looking for question with ID: {questionId}");

                var question = await _questionsCollection
                    .Find(q => q.Id == questionId)
                    .FirstOrDefaultAsync(cancellationToken);

                Console.WriteLine($"✅ Question found: {question != null}");

                return question;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error retrieving question {questionId}: {ex.Message}");
                return null;
            }
        }
    }
}