using System.Net.Http;
using System.Threading;
using System.Text.Json;
using System.Threading.Tasks;
using NLog;
using BudgetPoster.Common;
using static BudgetPoster.Common.GeneralExtensionMethods;
using System.Net.Http.Json;

namespace BudgetPoster.Discord
{
    internal class DiscordClient
    {
        #region Private fields
        private struct ExecuteWebhookParams
        {
            public string Content { get; set; }
        }
        private readonly string _discordWebhook;

        // Note: It would be nice to not have to use System.Json here, as we're using Newtonsoft everywhere else...
        private static readonly JsonNamingPolicy s_snakeCaseNamingPolicy = new SnakeCaseNamingPolicy();
        private static readonly JsonSerializerOptions s_serializerOptions = new() { PropertyNamingPolicy = s_snakeCaseNamingPolicy };
        private const int TimeBetweenAttemptsMs = 2000;
        private const int NumberOfMaxAttempts = 10;
        #endregion

        public DiscordClient(string discordWebhook)
        {
            _discordWebhook = discordWebhook;
        }

        public async Task<bool> TryPostUpdateToDiscord(string post)
        {
            ExecuteWebhookParams webHookParams = new() { Content = post };
            var content = JsonContent.Create(webHookParams, options: s_serializerOptions);
            var uri = $"{_discordWebhook}?wait=true";

            using HttpClient client = new();
            HttpResponseMessage? response;
            var numberOfAttempts = 1;

            while (true)
            {
                response = await client.PostAsync(uri, content);
                if (response.IsSuccessStatusCode)
                    break;

                LoggerType.DiscordCommunication.Log(LoggingLevel.Warning, $"Could not post content - {response.FormatHttpResponse()}");
                if (numberOfAttempts == NumberOfMaxAttempts)
                {
                    LoggerType.DiscordCommunication.Log(LoggingLevel.Error, "Max attempts reached, aborting");
                    return false;
                }
                LoggerType.DiscordCommunication.Log(LoggingLevel.Debug, $"Next attempt ({numberOfAttempts++}/{NumberOfMaxAttempts}) in {TimeBetweenAttemptsMs / 1000.0} s");
                await Task.Delay(TimeBetweenAttemptsMs);
            }

            return true;
        }
    }
}
