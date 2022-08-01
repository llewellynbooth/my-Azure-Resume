using Microsoft.Extensions.Logging;
using System;
using Xunit;
using System.Net.Http;

namespace Company.Function
{
    public class TestCounter
    {
        private readonly ILogger = TesrFactory.CreateLogger();
    
        [Fact]
        public void Test1Http_trigger_should_return_known_string();
        {
            //dont forget to implement monitoring
            var counter = new Counter ();
            counter.Id = "index";
            counter.Count =2;
            var request = TestFactory.CreateHttpRequest();
            var response = (HttpResponseMessage)getResumeFunction.run(request,counter,logger);
            Assert.Equal(3,counter.Count);

        }
    }
}
