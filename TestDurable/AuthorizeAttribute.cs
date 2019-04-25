using Microsoft.AspNetCore.Authorization;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http;

namespace TestDurable
{


    public class ErrorHandlerAttribute : FunctionExceptionFilterAttribute
    {
        public override Task OnExceptionAsync(
            FunctionExceptionContext exceptionContext, CancellationToken cancellationToken)
        {
            // custom error handling logic could be written here
            // (e.g. write a queue message, send a notification, etc.)

            exceptionContext.Logger.LogError(
                $"ErrorHandler called. Function '{exceptionContext.FunctionName}" +
                ":{exceptionContext.FunctionInstanceId} failed.");

            return Task.CompletedTask;
        }
    }

        public class FunctionAuthorizeAttribute : FunctionInvocationFilterAttribute
    {
        public FunctionAuthorizeAttribute()
        {
        }

        public override Task OnExecutingAsync(FunctionExecutingContext executingContext, CancellationToken cancellationToken)
        {
            var workItem = executingContext.Arguments.First().Value as HttpRequestMessage;
            ValidationPackage validationPackage = new ValidationPackage();
            AuthenticationHeaderValue jwtInput = workItem.Headers.Authorization;

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

            if(!validationPackage.validToken)
            {
                workItem.Headers.Add("AuthorizationStatus",  Convert.ToInt32(HttpStatusCode.Unauthorized).ToString());
            }
            else
            {
                workItem.Headers.Add("AuthorizationStatus", Convert.ToInt32(HttpStatusCode.Accepted).ToString());
           
            }

            return base.OnExecutingAsync(executingContext, cancellationToken);
        }


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

    }
}
