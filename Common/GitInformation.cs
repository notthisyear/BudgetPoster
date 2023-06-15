using System.Linq;
using System.Text.RegularExpressions;

namespace BudgetPoster.Common
{
    internal partial class GitInformation
    {
        #region Public properties
        public string? VersionTag { get; set; }

        public string? LastCommitHash { get; set; }

        public int NumberOfCommitsAhead { get; set; }

        public bool HasVersionTag => !string.IsNullOrEmpty(VersionTag);

        public bool? IsDirty { get; set; }
        #endregion

        [GeneratedRegex("(?<version>v\\d+\\.\\d+\\.\\d+)-(?<nrofcommitsahead>\\d+)(?<commithash>(\\-g\\w+)|(\\w+))(?<isdirty>(-dirty)|())")]
        private static partial Regex RegexIncludingVersion();

        [GeneratedRegex("(?<commithash>\\w+)(?<isdirty>(-dirty)|())")]
        private static partial Regex RegexExcludingVersion();

        [GeneratedRegex("v\\d+\\.\\d+\\.\\d+")]
        private static partial Regex CheckForVersionRegex();

        public override string ToString()
        {
            if (!HasVersionTag)
                return $"{LastCommitHash}{(IsDirty == null ? " <dirty status unknown>" : IsDirty == true ? " (dirty)" : string.Empty)}";

            if (NumberOfCommitsAhead == 0 && IsDirty == false)
                return VersionTag!;

            if (NumberOfCommitsAhead == 0 && IsDirty != false)
                return $"{VersionTag} [{GetTestStatusText()}]";

            if (NumberOfCommitsAhead > 0)
                return $"{VersionTag} [{GetTestStatusText()}-{NumberOfCommitsAhead}] ({LastCommitHash})";

            return "<invalid git information>";
        }

        public static bool TryParseGitInformation(string rawContent, out GitInformation? gitInformation)
        {
            var hasVersionTag = CheckForVersionRegex().IsMatch(rawContent);
            var extractingRegex = hasVersionTag ? RegexIncludingVersion() : RegexExcludingVersion();

            gitInformation = default;
            var result = extractingRegex.Match(rawContent);

            if (!result.Success)
                return false;

            gitInformation = new();
            foreach (var group in result.Groups.Cast<Group>())
            {
                switch (group.Name)
                {
                    case "version":
                        gitInformation.VersionTag = group.Success ? group.Value : string.Empty;
                        break;
                    case "nrofcommitsahead":
                        gitInformation.NumberOfCommitsAhead = group.Success ? int.Parse(group.Value) : -1;
                        break;
                    case "commithash":
                        // Remove the preceding "-g" if present
                        if (group.Success)
                            gitInformation.LastCommitHash = hasVersionTag ? group.Value[2..] : group.Value;
                        else
                            gitInformation.LastCommitHash = string.Empty;
                        break;
                    case "isdirty":
                        gitInformation.IsDirty = group.Success ? new bool?(!string.IsNullOrEmpty(group.Value)) : null;
                        break;
                }
            }
            return true;
        }

        private string GetTestStatusText()
            => IsDirty == null ? "<dirty status unknown>" : IsDirty == true ? "testd" : "test";
    }
}
