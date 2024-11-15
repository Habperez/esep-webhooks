using System;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using Newtonsoft.Json;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace EsepWebhook
{
    public class Function
    {
        private static readonly HttpClient HttpClient = new HttpClient();

        /// <summary>
        /// A Lambda function that processes a GitHub webhook and posts information to Slack.
        /// </summary>
        /// <param name="request">APIGatewayProxyRequest from API Gateway</param>
        /// <param name="context">Lambda Context</param>
        /// <returns>APIGatewayProxyResponse</returns>
        public async Task<APIGatewayProxyResponse> FunctionHandler(APIGatewayProxyRequest request, ILambdaContext context)
        {
            try
            {
                // Log the incoming request body
                context.Logger.LogLine("Received GitHub webhook event.");
                context.Logger.LogLine($"Request body: {request.Body}");

                // Deserialize the incoming GitHub payload
                dynamic json = JsonConvert.DeserializeObject(request.Body);
                if (json.issue == null || json.issue.html_url == null)
                {
                    return new APIGatewayProxyResponse
                    {
                        StatusCode = 400,
                        Body = "Invalid GitHub payload: issue or html_url is missing."
                    };
                }

                string issueUrl = json.issue.html_url;

                // Prepare payload to send to Slack
                string slackPayload = $"{{'text':'Issue Created: {issueUrl}'}}";

                // Get Slack URL from environment variable
                string slackUrl = Environment.GetEnvironmentVariable("SLACK_URL");
                if (string.IsNullOrEmpty(slackUrl))
                {
                    throw new Exception("Missing Slack URL in environment variables.");
                }

                // Post to Slack
                var webRequest = new HttpRequestMessage(HttpMethod.Post, slackUrl)
                {
                    Content = new StringContent(slackPayload, Encoding.UTF8, "application/json")
                };

                HttpResponseMessage response = await HttpClient.SendAsync(webRequest);
                response.EnsureSuccessStatusCode();

                return new APIGatewayProxyResponse
                {
                    StatusCode = 200,
                    Body = "Posted to Slack successfully!"
                };
            }
            catch (Exception ex)
            {
                context.Logger.LogLine($"Error: {ex.Message}");

                return new APIGatewayProxyResponse
                {
                    StatusCode = 500,
                    Body = "Internal server error"
                };
            }
        }
    }
}
