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
using FizzWare.NBuilder;
using System.Linq;

namespace TestDurable
{

   

    public static class GetCustomerById
    {
        [FunctionName("GetCustomerById")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");
            
           var customers= Builder<Customer>
           .CreateListOfSize(10)
           .All().Build();
            int Id =Convert.ToInt32( req.Query["Id"]);


            var customer = customers.Where(x => x.Id == Id);

            //string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            //dynamic data = JsonConvert.DeserializeObject(requestBody);
            //name = name ?? data?.name;

            return customer != null
                ? (ActionResult)new OkObjectResult(customer)
                : new BadRequestObjectResult("Please pass a name on the query string or in the request body");
        }
    }
}
