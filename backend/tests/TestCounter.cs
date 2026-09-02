using Xunit;

namespace Company.Function
{
    public class TestCounter
    {
        [Fact]
        public void Counter_Should_Increment_By_One()
        {
            var counter = new Counter { Id = "index", Count = 2 };
            var initialCount = counter.Count;

            counter.Count += 1;

            Assert.Equal(initialCount + 1, counter.Count);
            Assert.Equal(3, counter.Count);
        }

        [Fact]
        public void Counter_Should_Have_Valid_Id()
        {
            var counter = new Counter { Id = "index", Count = 100 };

            Assert.NotNull(counter.Id);
            Assert.Equal("index", counter.Id);
        }

        [Fact]
        public void Counter_Defaults_Id_To_Index()
        {
            var counter = new Counter();

            Assert.Equal("index", counter.Id);
        }
    }
}
