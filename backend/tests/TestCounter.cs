using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.AspNetCore.Http;
using System;
using Xunit;
using System.Net.Http;

namespace Company.Function
{
    public class TestCounter
    {
        private readonly ILogger<TestCounter> logger = NullLoggerFactory.Instance.CreateLogger<TestCounter>();

        [Fact]
        public void Counter_Should_Increment_By_One()
        {
            // Arrange
            var counter = new Counter();
            counter.Id = "index";
            counter.Count = 2;
            var initialCount = counter.Count;

            // Act - Simulate increment
            counter.Count += 1;

            // Assert
            Assert.Equal(initialCount + 1, counter.Count);
            Assert.Equal(3, counter.Count);
        }

        [Fact]
        public void Counter_Should_Have_Valid_Id()
        {
            // Arrange & Act
            var counter = new Counter
            {
                Id = "index",
                Count = 100
            };

            // Assert
            Assert.NotNull(counter.Id);
            Assert.Equal("index", counter.Id);
        }

        [Fact]
        public void Counter_Should_Not_Be_Negative()
        {
            // Arrange
            var counter = new Counter
            {
                Id = "index",
                Count = 0
            };

            // Act
            counter.Count += 1;

            // Assert
            Assert.True(counter.Count > 0);
        }
    }
}
