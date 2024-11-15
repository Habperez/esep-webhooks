using System;
using System.Net.Http;
using System.Text;
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
        /// Handles incoming webhook from GitHub and sends a notification to Slack.
        /// </summary>
        /// <param name="input">The incoming GitHub event payload.</param>
        /// <param name="context">Lambda context provided by AWS.</param>
        /// <returns>Response message indicating success or failure.</returns>
        public string FunctionHandler(object input, ILambdaContext context)
        {
            context.Logger.LogLine($"Received input: {input}");

            try
            {
                // Deserialize the GitHub payload
                dynamic json = JsonConvert.DeserializeObject(input.ToString());

                // Extract the issue URL
                if (json.issue == null || json.issue.html_url == null)
                {
                    return "Invalid GitHub payload: 'issue' or 'html_url' is missing.";
                }
                string issueUrl = json.issue.html_url;

                // Prepare the Slack message payload
                string payload = $"{{\"text\":\"Issue Created: {issueUrl}\"}}";

                // Get Slack webhook URL from environment variables
                string slackUrl = Environment.GetEnvironmentVariable("SLACK_URL");
                if (string.IsNullOrEmpty(slackUrl))
                {
                    throw new Exception("Slack URL not set in environment variables.");
                }

                // Send the request to Slack
                var webRequest = new HttpRequestMessage(HttpMethod.Post, slackUrl)
                {
                    Content = new StringContent(payload, Encoding.UTF8, "application/json")
                };

                HttpResponseMessage response = HttpClient.Send(webRequest);
                response.EnsureSuccessStatusCode();

                return "Posted to Slack successfully!";
            }
            catch (Exception ex)
            {
                context.Logger.LogLine($"Error: {ex.Message}");
                return "Failed to post to Slack.";
            }
        }
    }
}