using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using FizzWare.NBuilder;

namespace TestDurable
{
    public static class HelloWorld
    {
        [FunctionName("HelloWorld")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");
            var customers = Builder<Customer>
                      .CreateListOfSize(10)
                      .All().Build();
            int Id = Convert.ToInt32(req.Query["Id"]);

            return customers != null
                ? (ActionResult)new OkObjectResult(customers)
                : new BadRequestObjectResult("Please pass a name on the query string or in the request body");
        }
    }
}
