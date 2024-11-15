using System;
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

        public async Task<APIGatewayProxyResponse> FunctionHandler(APIGatewayProxyRequest request, ILambdaContext context)
        {
            try
            {
                // Log the incoming request body
                context.Logger.LogLine("Received GitHub webhook event.");
                context.Logger.LogLine($"Request body: {request.Body}");

                // Deserialize the incoming GitHub payload
                dynamic json = JsonConvert.DeserializeObject(request.Body);

                // Ensure the payload contains the issue object with html_url
                if (json.action != null && json.issue != null && json.issue.html_url != null)
                {
                    string issueUrl = json.issue.html_url;
                    context.Logger.LogLine($"Issue URL: {issueUrl}");

                    // Prepare the payload to send to Slack
                    string slackPayload = $"{{\"text\":\"Issue Created: {issueUrl}\"}}";

                    // Get Slack URL from environment variable
                    string slackUrl = Environment.GetEnvironmentVariable("SLACK_URL");
                    if (string.IsNullOrEmpty(slackUrl))
                    {
                        throw new Exception("Missing Slack URL in environment variables.");
                    }

                    // Send the payload to Slack
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
                else
                {
                    return new APIGatewayProxyResponse
                    {
                        StatusCode = 400,
                        Body = "Invalid GitHub payload: 'issue' or 'html_url' is missing."
                    };
                }
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