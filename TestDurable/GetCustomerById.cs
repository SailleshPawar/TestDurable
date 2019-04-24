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

namespace TestDurable
{

    public class ValidationPackage
    {
        public bool validToken { get; set; }
        public String principalName { get; set; }
        public String scope { get; set; }
        public String firstName { get; set; }
        public String lastName { get; set; }
        public String appID { get; set; }
        public long issuedAt { get; set; }
        public long expiresAt { get; set; }
        public String token { get; set; }

        public ValidationPackage()
        {
            validToken = false;
        }
    }

  

    public static class GetCustomerById
    {

        public static ValidationPackage ExtractClaims(string jwt, JwtSecurityTokenHandler handler)
        {
            ValidationPackage validationPackage = new ValidationPackage();

            validationPackage.token = jwt;

            var token = handler.ReadJwtToken(jwt);

            // !!!!!!!! Hardcoded Scope !!!!!!!!! \\
            validationPackage.scope = "user_impersonation";

            try
            {
                //Extract the payload of the JWT
                var claims = token.Claims;

                foreach (Claim c in claims)
                {
                    switch (c.Type)
                    {
                        case "sub":
                        case "upn":
                            // Make sure it's an email address ...
                            if (c.Value.Contains('@'))
                                validationPackage.principalName = c.Value;
                            //log.Info(Logger.Header() + "upn=" + c.Value);
                            break;

                        case "Firstname":
                            validationPackage.firstName = c.Value;
                            //log.Info(Logger.Header() + "Firstname=" + c.Value);
                            break;

                        case "Lastname":
                            validationPackage.lastName = c.Value;
                            //log.Info(Logger.Header() + "Lastname=" + c.Value);
                            break;

                        case "client_id":
                        case "aud":
                            validationPackage.appID = c.Value;
                            //log.Info(Logger.Header() + "aud=" + c.Value);
                            break;

                        case "iat":
                            validationPackage.issuedAt = Convert.ToInt64(c.Value);
                            //log.Info(Logger.Header() + "iat=" + c.Value);
                            break;

                        case "exp":
                            validationPackage.expiresAt = Convert.ToInt64(c.Value);
                            //log.Info(Logger.Header() + "exp=" + c.Value);
                            break;

                        case "scp":
                            validationPackage.scope = c.Value;
                            //log.Info(Logger.Header() + "scp=" + c.Value);
                            break;
                    }
                }
            }
            catch (Exception e)
            {
                validationPackage.validToken = false;
            }

            var currentTimestamp = (long)(DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0)).TotalSeconds;

            if ((validationPackage.expiresAt - currentTimestamp) > 0)
                validationPackage.validToken = true;

            return validationPackage;
        }

        [FunctionName("GetCustomerById")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequestMessage req,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");
            ValidationPackage validationPackage = null;
            AuthenticationHeaderValue jwtInput = req.Headers.Authorization;

            if (jwtInput != null)
            {
                String jwt = "";
                if (jwtInput.ToString().StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                {
                    jwt = jwtInput.ToString().Substring("Bearer ".Length).Trim();
                }

                JwtSecurityTokenHandler handler = new JwtSecurityTokenHandler();

                if (!string.IsNullOrEmpty(jwt))
                {
                    try
                    {
                        validationPackage = ExtractClaims(jwt, handler);
                    }
                    catch (Exception e)
                    {
                       // log.Error("Exception caught while validating token", e);
                       
                    }
                }
                else
                {
                    //log.Error("Auth Token begins with [" + jwtInput.ToString().Substring(0, 12) + "...]");
                   
                }
            }

            

           var customers= Builder<Customer>
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





    }

}
