using System;
using System.Net;
using System.Net.Http;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Company.Function
{
    public static class HealthCheck
    {
        [FunctionName("HealthCheck")]
        public static HttpResponseMessage Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "health")] HttpRequest req,
            [CosmosDB(databaseName: "CloudResume", containerName: "Counter", Connection = "CloudResume", Id = "index", PartitionKey = "index")] Counter counter,
            ILogger log)
        {
            log.LogInformation("Health check endpoint called.");

            var health = new
            {
                status = "healthy",
                timestamp = DateTime.UtcNow,
                service = "Azure Resume API",
                version = "1.0.0",
                checks = new
                {
                    database = counter != null ? "connected" : "disconnected",
                    api = "operational"
                }
            };

            var jsonResponse = JsonConvert.SerializeObject(health, Formatting.Indented);

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(jsonResponse, Encoding.UTF8, "application/json")
            };
        }
    }
}
