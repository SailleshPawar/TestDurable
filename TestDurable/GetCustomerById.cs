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
using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Net.Http;
using TestDurable;
using System.Security.Claims;
using System.Net;

namespace TestDurable
{

    public class ValidationPackage
    {
        public bool validToken { get; set; }
        public string principalName { get; set; }
        public string scope { get; set; }
        public string appID { get; set; }
        public long issuedAt { get; set; }
        public long expiresAt { get; set; }
        public string token { get; set; }

        public ValidationPackage()
        {
            validToken = false;
        }
    }

  

   
    public static class GetCustomerById
    {

       
        [FunctionName("GetCustomerById")]
        [FunctionAuthorize]
        [ErrorHandler]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequestMessage req,
            ILogger log)
        {

            string authorizationStatus = req.Headers.GetValues("AuthorizationStatus").FirstOrDefault();         
            if (Convert.ToInt32(authorizationStatus).Equals((int)HttpStatusCode.Accepted))
            {
                log.LogInformation("C# HTTP trigger function processed a request.");
                var customers = Builder<Customer>
                .CreateListOfSize(10)
                .All().Build();
                //int Id =Convert.ToInt32( req.Query["Id"]);
                int Id = 1;

                var customer = customers.Where(x => x.Id == Id);

                //string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                //dynamic data = JsonConvert.DeserializeObject(requestBody);
                //name = name ?? data?.name;

                return customer != null
                    ? (ActionResult)new OkObjectResult(customer)
                    : new BadRequestObjectResult("Please pass a name on the query string or in the request body");
            }
            else

            {
                return new UnauthorizedResult();
            }
        }





    }

}
