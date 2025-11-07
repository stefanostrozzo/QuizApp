using Xunit;
using System.Collections.Generic;
using Quiz_Task.Controllers.Utils;

namespace Quiz_Task.Tests.Unit.Utils
{
    /// <summary>
    /// Unit tests for the ApiResponse<T> utility class.
    /// Focuses on ensuring the static factory methods (Ok, Fail) create responses with the correct state.
    /// </summary>
    public class ApiResponseTests
    {
        /// <summary>
        /// Test case for the static Ok factory method with simple data and default message.
        /// </summary>
        [Fact]
        public void Ok_ShouldCreateSuccessResponse_WithDataAndDefaultMessage()
        {
            // Arrange
            const string expectedData = "session-123";

            // Act
            var response = ApiResponse<string>.Ok(expectedData);

            // Assert
            Assert.True(response.Success);
            Assert.Equal("Success", response.Message);
            Assert.Equal(expectedData, response.Data);
            Assert.Null(response.Errors); // Should be null on success
        }

        /// <summary>
        /// Test case for the static Fail factory method without a detailed errors list.
        /// </summary>
        [Fact]
        public void Fail_ShouldCreateFailureResponse_WithoutDetailedErrors()
        {
            // Arrange
            const string errorMessage = "Validation failed.";

            // Act
            var response = ApiResponse<int>.Fail(errorMessage);

            // Assert
            Assert.False(response.Success);
            Assert.Equal(errorMessage, response.Message);
            Assert.Equal(default(int), response.Data); // Data should be default value (0 for int)
            Assert.NotNull(response.Errors);
            Assert.Single(response.Errors);
            Assert.Equal(errorMessage, response.Errors[0]); // Message should be included in errors list
        }

        /// <summary>
        /// Test case for the static Fail factory method with a detailed errors list.
        /// </summary>
        [Fact]
        public void Fail_ShouldCreateFailureResponse_WithDetailedErrors()
        {
            // Arrange
            const string friendlyMessage = "A system error occurred.";
            var technicalErrors = new List<string> { "DB connection lost", "Null reference exception" };

            // Act
            var response = ApiResponse<bool>.Fail(friendlyMessage, technicalErrors);

            // Assert
            Assert.False(response.Success);
            Assert.Equal(friendlyMessage, response.Message);
            Assert.Equal(default(bool), response.Data); // Data should be default value (false for bool)
            Assert.NotNull(response.Errors);
            Assert.Equal(2, response.Errors.Count);
            Assert.Contains("DB connection lost", response.Errors);
            Assert.Contains("Null reference exception", response.Errors);
        }
    }
}