using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Net;
using System.Net.Http;
using System.Text;


namespace Company.Function
{
    public static class getResumeFunction
    {
        [FunctionName("getResumeFunction")]
        public static HttpResponseMessage Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequest req,
            [CosmosDB(DatabaseName = "CloudResume", ContainerName = "Counter", Connection = "CloudResume", Id = "index", PartitionKey = "index")] Counter counter,
            [CosmosDB(DatabaseName = "CloudResume", ContainerName = "Counter", Connection = "CloudResume", Id = "index", PartitionKey = "index")] out Counter updatedCounter,
             ILogger log)
        {
            log.LogInformation("GetResumeCounter was Triggered.");

            updatedCounter = counter;
            updatedCounter.Count += 1;

            var jsonToReturn = JsonConvert.SerializeObject(updatedCounter); 
            
            return new HttpResponseMessage(System.Net.HttpStatusCode.OK)

            {
                Content = new StringContent(jsonToReturn, Encoding.UTF8,"Application/json")
            };
        }
    }
}
