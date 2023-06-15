# BudgetPoster
A small program that fetches a budget sheet and optionally posts it to a Discord webhook. If the Discord webhook URL is not posted, the post is simply printed to the console.

The program will try to fetch all data up until the specified month, as well as an initial bank balance value and print a summary for the requested month, as well a total tally.

To run the program, a Google account where the Google Sheet API is enabled is required. Follow the steps under "*Set up your environment*" [at this link](https://developers.google.com/sheets/api/quickstart/python) to enable the API and download the necessary `credentials.json` file. 

Next, a webhook must be created at the desired Discord server. Follow the steps [at this link](https://support.discord.com/hc/en-us/articles/228383668-Intro-to-Webhooks) to set it up. Note the `Webhook URL`.

## Running the program
To run the program, simple clone this repository and build it with `dotnet build`. The build command should restore the required NuGet packages automatically.

Typical usage:

```
BudgetPoster --credentials-path <path/to/credentials>
             --account-name <firstname.lastname@example.com> 
             --sheet-id <google-sheet-id>
             --month <month> 
             --discord-webhook-url <https://discord.com/api/webhook/id/token> 
```

The `discord-webhook-url` is optional and if not given, the result is posted to the console. The `month` argument can be given as either the full name of the month, its three-letter abbreviation or its number. Both English and Swedish is supported.

Addtional options include:

- `currency-formatting` - Currency formatting in different locale. The name of the format should be given as defined in [BCP 47](https://www.rfc-editor.org/info/bcp47) and defaults to `sv-SE`.
- `tab-width` - The tab width to use when printing the result to the console. The default value is 3.

The first time the program runs, the authentication flow will open a browser where the user must log in to the relevant Google account and allow the program to access the sheet. It will generate a token file that gets re-used until it expires.

## Sheet-specific settings
In `BudgetSheetConstants.cs`, a number of parameters specific to the target sheet is set. Update these to suit your use case and rebuild the program.