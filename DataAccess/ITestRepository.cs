//author: Stefano Strozzo <strozzostefano@gmail.com>

using Quiz_Task.Models;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Quiz_Task.DataAccess
{
    /// <summary>
    /// Interface for Test repository to handle data access operations.
    /// Follows best practices for asynchronous operations in .NET 8.
    /// </summary>
    public interface ITestRepository
    {
        /// <summary>
        /// Method to retrieve all tests from the data source asynchronously.
        /// </summary>
        /// <param name="cancellationToken">Optional token to cancel the operation.</param>
        /// <returns>A task whose result contains a read-only list of tests.</returns>
        Task<IReadOnlyList<Test>> GetAllTestsAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Method to retrieve questions by Test ID asynchronously.
        /// </summary>
        /// <param name="testId">The ID of the test.</param>
        /// <param name="cancellationToken">Optional token to cancel the operation.</param>
        /// <returns>A task whose result contains a read-only list of questions for the specified test.</returns>
        Task<IReadOnlyList<Question>> GetQuestionsByTestIdAsync(string testId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Method to retrieve a question by its ID asynchronously.
        /// </summary>
        /// <param name="questionId">The ID of the question.</param>
        /// <param name="cancellationToken">Optional token to cancel the operation.</param>
        /// <returns>A task whose result contains the Question, or null if not found.</returns>
        Task<Question?> GetQuestionByIdAsync(string questionId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Method to retrieve a test by its ID asynchronously.
        /// </summary>
        /// <param name="testId">The ID of the test.</param>
        /// <param name="cancellationToken">Optional token to cancel the operation.</param>
        /// <returns>A task whose result contains the Test object, or null if not found.</returns>
        Task<Test?> GetTestByIdAsync(string testId, CancellationToken cancellationToken = default);
    }
}