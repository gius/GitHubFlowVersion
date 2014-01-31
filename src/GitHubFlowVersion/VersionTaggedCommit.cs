using LibGit2Sharp;

namespace GitHubFlowVersion
{
    public class VersionTaggedCommit
    {
        private readonly Commit _commit;
        private readonly SemanticVersion _semVer;

        public VersionTaggedCommit(Commit commit, SemanticVersion semVer)
        {
            _commit = commit;
            _semVer = semVer;
        }

        public Commit Commit
        {
            get { return _commit; }
        }

        public SemanticVersion SemVer
        {
            get { return _semVer; }
        }

        public static VersionTaggedCommit Create(Commit commit, string semVer)
        {
            SemanticVersion version;
            if (SemanticVersionParser.TryParse(semVer, out version))
            {
                return new VersionTaggedCommit(commit, version);
            }
            return null;
        }
    }
}