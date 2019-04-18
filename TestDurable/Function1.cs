using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;

namespace TestDurable
{

    public class RequestClass
    {
        public int clientId { get; set; }
        public string Name { get; set; }
    }
    public static class Function1
    {
        [FunctionName("Function1")]
        public static async Task<List<string>> RunOrchestrator(
            [OrchestrationTrigger] DurableOrchestrationContext context)
        {
            var outputs = new List<string>();
            RequestClass orderData = context.GetInput<RequestClass>();


            // Replace "hello" with the name of your Durable Activity Function.
            outputs.Add(await context.CallActivityAsync<string>("Function1_Hello", orderData));
            //outputs.Add(await context.CallActivityAsync<string>("Function1_Hello", "Seattle"));
            //outputs.Add(await context.CallActivityAsync<string>("Function1_Hello", "London"));

            // returns ["Hello Tokyo!", "Hello Seattle!", "Hello London!"]
            return outputs;
        }

        [FunctionName("Function1_Hello")]
        public static async Task<string> SayHello([ActivityTrigger] RequestClass name, TraceWriter log)
        {
            try
            {
                log.Info($"Saying hello to {name}.");

                Stopwatch sw = new Stopwatch();
                sw.Start();
                SqlConnection connection = new SqlConnection("");
                SqlCommand cmd = new SqlCommand();
                connection.Open();
                cmd.Connection = connection;
                cmd.CommandTimeout = 0;
                cmd.CommandType = System.Data.CommandType.StoredProcedure;
                cmd.CommandText = "sp_getAuthors";

                log.Info("Executing sp");
                SqlDataReader reader =await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    log.Info(reader["Id"].ToString() + ", ");
                    log.Info(reader["Author_name"].ToString() + ", ");
                    log.Info(reader["country"].ToString() + ", ");

                }
                connection.Close();
                sw.Stop();
            }
            catch (Exception ex)
            {
                log.Info(ex.Message);
            }
            return $"Hello {name.Name}!";
        }

        [FunctionName("Function1_HttpStart")]
        public static async Task<HttpResponseMessage> HttpStart(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")]HttpRequestMessage req,
            [OrchestrationClient]DurableOrchestrationClient starter,
            TraceWriter log)
        {

            // Function input comes from the request content.
            RequestClass eventData = await req.Content.ReadAsAsync<RequestClass>();
            string instanceId = await starter.StartNewAsync("Function1", eventData);
           
            log.Info($"Started orchestration with ID = '{instanceId}'.");

            return starter.CreateCheckStatusResponse(req, instanceId);
        }
    }
}