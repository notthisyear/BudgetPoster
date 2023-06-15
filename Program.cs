using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using CommandLine;
using CommandLine.Text;
using BudgetPoster.Common;
using System.Threading.Tasks;
using System.Globalization;

namespace BudgetPoster
{
    public class Program
    {
        private const string HelpHeading = "BudgetPoster - Pulls content from LKFs budget sheet and posts to Discord";
        private const string HelpCopyright = "Copyright (C) 2023 Calle Lindquist";
        private const string GitStatusFileName = "git_status.txt"; // See post-build event

        private static GitInformation? s_gitInformation;
        private static string? s_assemblyName;

        public static void Main(string[] args)
        {
            Parser parser = new(x =>
            {
                x.HelpWriter = null;
                x.AutoHelp = true;
                x.AutoVersion = true;
            });

            s_assemblyName = Assembly.GetExecutingAssembly().GetName().Name ?? "[COULD NOT GET ASSEMBLY NAME]";
            if (TrySetGitInformation())
            {
                Console.Title = $"{s_assemblyName} | {s_gitInformation!} | started at {DateTime.Now:yyyy-MM-dd HH:mm:ss}";
            }
            else
            {
                Console.Title = $"{s_assemblyName} | started at {DateTime.Now:yyyy-MM-dd HH:mm:ss}";
                LoggerType.Internal.Log(LoggingLevel.Debug, "Could not find git version file - is git installed?");
            }

            var result = parser.ParseArguments<BudgetPosterArguments>(args);
            result.WithParsed(RunProgram)
                  .WithNotParsed(err => RunErrorFlow(result, err));
        }

        private static bool TrySetGitInformation()
        {
            var gitStatusFilePath = Path.Combine(AppContext.BaseDirectory, GitStatusFileName);
            if (File.Exists(gitStatusFilePath))
            {
                var gitStatusRaw = File.ReadAllText(gitStatusFilePath);
                if (GitInformation.TryParseGitInformation(gitStatusRaw, out var gitInformation))
                    s_gitInformation = gitInformation;
            }

            return s_gitInformation != default;
        }

        private static void RunProgram(BudgetPosterArguments args)
        {
            if (!File.Exists(args.CredentialsPath))
            {
                LoggerType.Internal.Log(LoggingLevel.Error, $"ERROR: File '{args.CredentialsPath}' cannot be found!");
                Environment.Exit(1);
            }

            if (string.IsNullOrEmpty(args.AccountName))
            {
                LoggerType.Internal.Log(LoggingLevel.Error, "ERROR: Account name cannot be empty");
                Environment.Exit(1);
            }

            if (string.IsNullOrEmpty(args.SheetId))
            {
                LoggerType.Internal.Log(LoggingLevel.Error, "ERROR: SheetId cannot be empty");
                Environment.Exit(1);
            }

            if (string.IsNullOrEmpty(args.Month) || string.IsNullOrEmpty(args.Month!.TryMapToMonth()))
            {
                LoggerType.Internal.Log(LoggingLevel.Error, "ERROR: No valid month given");
                Environment.Exit(1);
            }

            try
            {
                _ = CultureInfo.CreateSpecificCulture(args.CurrenyCulture ?? string.Empty);
            }
            catch (CultureNotFoundException)
            {
                LoggerType.Internal.Log(LoggingLevel.Error, $"ERROR: '{args.CurrenyCulture}' is not a valid culture format");
                Environment.Exit(1);
            }

            args.Month = args.Month!.TryMapToMonth();
            var tcs = new TaskCompletionSource();
            Task.Run(async () => await EntryPoint.StartProgram(s_assemblyName!, s_gitInformation!, tcs, args));
            tcs.Task.Wait();
        }

        private static void RunErrorFlow(ParserResult<BudgetPosterArguments> result, IEnumerable<Error> errors)
        {
            var isVersionRequest = errors.FirstOrDefault(x => x.Tag == ErrorType.VersionRequestedError) != default;
            var isHelpRequest = errors.FirstOrDefault(x => x.Tag == ErrorType.HelpRequestedError) != default ||
                                errors.FirstOrDefault(x => x.Tag == ErrorType.HelpVerbRequestedError) != default;

            var output = string.Empty;
            if (isHelpRequest)
            {
                output = HelpText.AutoBuild(result,
                h =>
                {
                    h.Heading = HelpHeading;
                    h.Copyright = HelpCopyright;
                    return h;
                });
            }
            else if (isVersionRequest)
            {
                output = s_gitInformation == default ? "<could not read version info>" : s_gitInformation.ToString();
            }
            else
            {
                output = errors.Count() > 1 ? "ERRORS:\n" : "ERROR:\n";
                foreach (var error in errors)
                    output += '\t' + GetErrorText(error) + '\n';
            }
            Console.WriteLine(output);
        }

        private static string GetErrorText(Error error)
        {
            return error switch
            {
                MissingValueOptionError missingValueError => $"Value for argument '{missingValueError.NameInfo.NameText}' is missing",
                UnknownOptionError unknownOptionError => $"Argument '{unknownOptionError.Token}' is unknown",
                MissingRequiredOptionError missingRequiredOption => $"A required option ('{missingRequiredOption.NameInfo.LongName}') is missing value",
                SetValueExceptionError setValueExceptionError => $"Could not set value for argument '{setValueExceptionError.NameInfo.NameText}': {setValueExceptionError.Exception.Message}",
                BadFormatConversionError badFormatConversionError => $"Argument '{badFormatConversionError.NameInfo.NameText}' has bad format",
                _ => $"Argument parsing failed: '{error.Tag}'"
            };
        }
    }
}
