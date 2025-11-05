//author: Stefano Strozzo <strozzostefano@gmail.com>

using Quiz_Task.Models;
using System.Threading;
using System.Threading.Tasks;

namespace Quiz_Task.DataAccess
{
    /// <summary>
    /// Interface for User Session repository to handle data access operations.
    /// </summary>
    public interface IUserSessionRepository
    {
        /// <summary>
        /// Creates a new user session in the data source asynchronously.
        /// </summary>
        /// <param name="session">The UserSession object to create.</param>
        /// <param name="cancellationToken">Optional cancellation token.</param>
        /// <returns>A task that represents the asynchronous operation. The task result is the ID of the newly created session.</returns>
        Task<string> CreateSessionAsync(UserSession session, CancellationToken cancellationToken = default);

        /// <summary>
        /// Retrieves a user session by its ID asynchronously.
        /// </summary>
        /// <param name="sessionId">The ID of the session to retrieve.</param>
        /// <param name="cancellationToken">Optional cancellation token.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the UserSession, or null if not found.</returns>
        Task<UserSession?> GetSessionByIdAsync(string sessionId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Updates an existing user session in the data source asynchronously.
        /// </summary>
        /// <param name="session">The UserSession object with updated values.</param>
        /// <param name="cancellationToken">Optional cancellation token.</param>
        /// <returns>A Task representing the asynchronous update operation.</returns>
        Task UpdateSessionAsync(UserSession session, CancellationToken cancellationToken = default);
    }
}