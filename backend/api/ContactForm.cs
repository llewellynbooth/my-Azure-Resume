using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Company.Function
{
    public class ContactMessage
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("email")]
        public string Email { get; set; }

        [JsonProperty("subject")]
        public string Subject { get; set; }

        [JsonProperty("message")]
        public string Message { get; set; }

        [JsonProperty("timestamp")]
        public DateTime Timestamp { get; set; }

        [JsonProperty("ipAddress")]
        public string IpAddress { get; set; }
    }

    public static class ContactForm
    {
        [FunctionName("ContactForm")]
        public static async Task<HttpResponseMessage> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "contact")] HttpRequest req,
            [CosmosDB(databaseName: "CloudResume", containerName: "Messages", Connection = "CloudResume")] IAsyncCollector<ContactMessage> messagesOut,
            ILogger log)
        {
            log.LogInformation("Contact form submission received.");

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            dynamic data = JsonConvert.DeserializeObject(requestBody);

            // Validation
            if (data?.name == null || data?.email == null || data?.message == null)
            {
                return new HttpResponseMessage(HttpStatusCode.BadRequest)
                {
                    Content = new StringContent("{\"error\":\"Name, email, and message are required.\"}", Encoding.UTF8, "application/json")
                };
            }

            // Basic email validation
            string email = data.email.ToString();
            if (!email.Contains("@") || !email.Contains("."))
            {
                return new HttpResponseMessage(HttpStatusCode.BadRequest)
                {
                    Content = new StringContent("{\"error\":\"Invalid email address.\"}", Encoding.UTF8, "application/json")
                };
            }

            // Create contact message
            var contactMessage = new ContactMessage
            {
                Id = Guid.NewGuid().ToString(),
                Name = data.name,
                Email = email,
                Subject = data.subject ?? "Contact Form Submission",
                Message = data.message,
                Timestamp = DateTime.UtcNow,
                IpAddress = req.HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown"
            };

            // Save to Cosmos DB
            await messagesOut.AddAsync(contactMessage);

            log.LogInformation($"Contact message saved from {contactMessage.Email}");

            var response = new
            {
                success = true,
                message = "Thank you for your message! I'll get back to you soon.",
                id = contactMessage.Id
            };

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(JsonConvert.SerializeObject(response), Encoding.UTF8, "application/json")
            };
        }
    }
}
