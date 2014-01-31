using System;
using System.Linq;
using LibGit2Sharp;

namespace GitHubFlowVersion
{
    public class GusFlowNextSemverCalculator : INextSemverCalculator
    {
        private readonly INextVersionTxtFileFinder _nextVersionTxtFileFinder;
        private readonly ILastTaggedReleaseFinder _lastTaggedReleaseFinder;
        private static readonly string[] SuffixesWithNoIncrease = new string[] { "rc", "patch" };

        public GusFlowNextSemverCalculator(
            INextVersionTxtFileFinder nextVersionTxtFileFinder,
            ILastTaggedReleaseFinder lastTaggedReleaseFinder)
        {
            _nextVersionTxtFileFinder = nextVersionTxtFileFinder;
            _lastTaggedReleaseFinder = lastTaggedReleaseFinder;
        }

        public SemanticVersion NextVersion()
        {
            SemanticVersion lastRelease = _lastTaggedReleaseFinder.GetVersion().SemVer;
            SemanticVersion fileVersion = _nextVersionTxtFileFinder.GetNextVersion(lastRelease);
            if (fileVersion <= lastRelease)
            {
                if (IncreaseVersion(lastRelease))
                {
                    return new SemanticVersion(lastRelease.Major, lastRelease.Minor + 1, 0, lastRelease.Suffix);
                }
                else
                {
                    return lastRelease;
                }
            }

            return fileVersion.WithSuffix(lastRelease.Suffix);
        }

        private static bool IncreaseVersion(SemanticVersion semVer)
        {
            return !string.IsNullOrEmpty(semVer.Suffix) && !SuffixesWithNoIncrease.Any(x => semVer.Suffix.StartsWith(x));
        }
    }
}