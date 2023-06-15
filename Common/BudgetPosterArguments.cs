using CommandLine;

namespace BudgetPoster.Common
{
    internal class BudgetPosterArguments
    {

        [Option(longName: "credentials-path", Required = true, HelpText = "Path to the credentials file")]
        public string? CredentialsPath { get; set; }

        [Option(longName: "account-name", Required = true, HelpText = "The account name to try to authorize as")]
        public string? AccountName { get; set; }

        [Option(longName: "sheet-id", Required = true, HelpText = "The budget Google sheet ID to target")]
        public string? SheetId { get; set; }

        [Option(longName: "month", Required = true, HelpText = "The month to post")]
        public string? Month { get; set; }

        [Option(longName: "currency-formatting", Default = "sv-SE", HelpText = "Culture to use when formatting currency")]
        public string? CurrenyCulture { get; set; }

        [Option(longName: "tab-width", Default = (uint)3, HelpText = "The tab width to use when printing the results")]
        public uint TabWidth { get; set; }

        [Option(longName: "discord-webhook-url", HelpText = "A Discord webhook URI to post budget updates to. If not given, the update is written to the console")]
        public string? DiscordWebhookUrl { get; set; }
    }
}