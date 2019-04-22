using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;


namespace TestDurable
{
    public static class Function2
    {
        [FunctionName("Function2")]
        public static async Task<List<string>> RunOrchestrator(
            [OrchestrationTrigger] DurableOrchestrationContext context)
        {
            var outputs = new List<string>();

            // Replace "hello" with the name of your Durable Activity Function.
            outputs.Add(await context.CallActivityAsync<string>("Function2_Hello", "Tokyo"));
            //outputs.Add(await context.CallActivityAsync<string>("Function2_Hello", "Seattle"));
            //outputs.Add(await context.CallActivityAsync<string>("Function2_Hello", "London"));

            // returns ["Hello Tokyo!", "Hello Seattle!", "Hello London!"]
            return outputs;
        }

        [FunctionName("Function2_Hello")]
        public static async Task<string> SayHello([ActivityTrigger] string name, ILogger log)
        {
            //log.LogInformation($"Saying hello to {name}.");
            //return $"Hello {name}!";
            Stopwatch sw = new Stopwatch();
            try
            {
                log.LogInformation($"Saying hello to {name}.");

               
                sw.Start();

                //don't instantiate inside sqlconnection use static connection one for all
                SqlConnection connection = new SqlConnection("Server=tcp:dev-test-perf.database.windows.net;Initial Catalog=TestDB;Persist Security Info=False;User ID=Saillesh;Password=Angular@2018;MultipleActiveResultSets=True;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;");
                SqlCommand cmd = new SqlCommand();
                connection.Open();
                cmd.Connection = connection;
                cmd.CommandTimeout = 0;
                cmd.CommandType = System.Data.CommandType.StoredProcedure;
                cmd.CommandText = "sp_getAuthors";

                log.LogInformation("Executing sp");
                SqlDataReader reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    log.LogInformation(reader["Id"].ToString() + ", ");
                    log.LogInformation(reader["Author_name"].ToString() + ", ");
                    log.LogInformation(reader["country"].ToString() + ", ");

                }
                connection.Close();
                sw.Stop();
            }
            catch (Exception ex)
            {
                log.LogInformation(ex.Message);
            }
            return $"Hello Total time {sw.Elapsed.Minutes}!";
        }

        //[FunctionName("Function2_HttpStart")]
        //public static async Task<HttpResponseMessage> HttpStart(
        //    [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")]HttpRequestMessage req,
        //    [OrchestrationClient]DurableOrchestrationClient starter,
        //    ILogger log)
        //{
        //    // Function input comes from the request content.
        //    string instanceId = await starter.StartNewAsync("Function2", null);

        //    log.LogInformation($"Started orchestration with ID = '{instanceId}'.");

        //    return starter.CreateCheckStatusResponse(req, instanceId);
        //}


        //install dependency Microsoft.Azure.WebJobs.Extensions.ServiceBus
        [FunctionName("ServiceBusStarter")]
        public static async Task RunFromServiceBusQueue(
    [ServiceBusTrigger("durablequeue", Connection = "ServiceBus")] string message,
    [OrchestrationClient] DurableOrchestrationClient starter,
    ILogger log)
        {
            
            string instanceId = await starter.StartNewAsync("Function2", null);
            log.LogInformation($"Started orchestration with ID = '{instanceId}'.");
        }

    }
}