//author: Stefano Strozzo <strozzostefano@gmail.com>

using System.Collections.Generic;
using System.Linq;

namespace Quiz_Task.Controllers.Utils
{
    /// <summary>
    /// Utility class to standardize API responses.
    /// Guarantees that every response has a consistent structure (Success, Message, Data, Errors).
    /// </summary>
    /// <typeparam name="T">The data type of the response payload.</typeparam>
    // Note on Best Practice: Using a record here (public record ApiResponse<T>)
    // might be preferable if the class is intended to be purely immutable data.
    public class ApiResponse<T>
    {
        /// <summary>
        /// Indicates whether the API call was successful.
        /// </summary>
        public bool Success { get; }

        /// <summary>
        /// A friendly, user-facing message explaining the result or the error.
        /// </summary>
        public string Message { get; }

        /// <summary>
        /// The payload data returned by the API call, if successful. Default (null) if failed.
        /// </summary>
        public T? Data { get; } // BEST PRACTICE: Use T? to clearly indicate Data might be null on failure.

        /// <summary>
        /// A list of detailed technical error messages, if the API call failed.
        /// </summary>
        public List<string>? Errors { get; }

        /// <summary>
        /// Private constructor to initialize all properties.
        /// Forces the use of static factory methods (Ok and Fail) for object creation,
        /// ensuring proper initialization and immutability of the response object.
        /// </summary>
        /// <param name="success">Indicates if the response is successful.</param>
        /// <param name="message">The response message.</param>
        /// <param name="data">The response data.</param>
        /// <param name="errors">A list of error messages, if any.</param>
        private ApiResponse(bool success, string message, T? data, List<string>? errors)
        {
            Success = success;
            Message = message;
            Data = data;
            Errors = errors;
        }

        /// <summary>
        /// Static factory method to create a successful response.
        /// Recommended usage: ApiResponse<T>.Ok(data, "Success Message");
        /// </summary>
        /// <param name="data">The data to be included in the response.</param>
        /// <param name="message">An optional success message. Defaults to "Success".</param>
        /// <returns>A new ApiResponse instance with Success = true.</returns>
        public static ApiResponse<T> Ok(T data, string message = "Success")
        {
            // In case of success, the Errors list is explicitly set to null
            return new ApiResponse<T>(true, message, data, null);
        }

        /// <summary>
        /// Static factory method to create an error response.
        /// Recommended usage: ApiResponse<T>.Fail("Friendly Error", technicalErrorList);
        /// </summary>
        /// <param name="message">The friendly error message for the user.</param>
        /// <param name="errors">An optional list of detailed technical error messages.</param>
        /// <returns>A new ApiResponse instance with Success = false.</returns>
        public static ApiResponse<T> Fail(string message, List<string>? errors = null)
        {
            // If no detailed errors are provided, use the main message as the technical error for consistency.
            List<string> errorList = errors != null && errors.Any() ? errors : new List<string> { message };

            // In case of error, Data is set to default (null for reference types)
            return new ApiResponse<T>(false, message, default(T), errorList);
        }
    }
}