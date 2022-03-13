using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace HealthCheckFunctionApp
{
    public class CheckItems
    {
        public string CheckItemId { get; set; }
        public string Alias { get; set; }
        public string DateTimeOfEntry { get; set; }
        public string Symptoms { get; set; }
    }

    public static class HealthCheck
    {
        [FunctionName("GetHealthInfo")]
        public static IActionResult GetHealthInfo(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "/{alias}")] HttpRequest req,
            [CosmosDB(
                databaseName: "HealthCheck",
                collectionName: "CheckItems",
                ConnectionStringSetting = "ConnectionStrings:cosmos-poc-julio",
                SqlQuery = "Select * From c Where Alias = {alias}")] IEnumerable<CheckItems> checkItems,
            ILogger log)
        {
            return new JsonResult(checkItems);
        }


        [FunctionName("PostHealthInfo")]
        public static async Task<IActionResult> PostHealthInfo(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req,
            [CosmosDB(
                databaseName: "HealthCheck",
                collectionName: "CheckItems",
                ConnectionStringSetting = "ConnectionStrings:cosmos-poc-julio")] IAsyncCollector<dynamic> document,
            ILogger log)
        {
            try
            {
                //string name = req.Query["name"];
                string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                dynamic data = JsonConvert.DeserializeObject(requestBody);

                CheckItems checkItems = new CheckItems
                {
                    CheckItemId = new Guid().ToString(),
                    Alias = data?.Alias,
                    DateTimeOfEntry = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss"),
                    Symptoms = data?.Symptoms
                };

                await document.AddAsync(checkItems);
                return new OkResult();
            }
            catch (Exception ex)
            {
                log.LogError(ex.Message);
                log.LogError(ex.ToString());
                return new StatusCodeResult(500); //internal server error
            }
        }
    }
}
