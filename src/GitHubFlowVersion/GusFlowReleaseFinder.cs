using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LibGit2Sharp;

namespace GitHubFlowVersion
{
    public class GusFlowReleaseFinder : ILastTaggedReleaseFinder
    {
        private readonly string _workingDirectory;
        private readonly Lazy<VersionTaggedCommit> _lastTaggedRelease;

        public GusFlowReleaseFinder(IRepository gitRepo, IGitHelper gitHelper, string workingDirectory)
        {
            _workingDirectory = workingDirectory;
            _lastTaggedRelease = new Lazy<VersionTaggedCommit>(() => GetVersion(gitRepo, gitHelper));
        }

        public VersionTaggedCommit GetVersion()
        {
            return _lastTaggedRelease.Value;
        }

        private VersionTaggedCommit GetVersion(IRepository gitRepo, IGitHelper gitHelper)
        {
            var head = gitRepo.Head;
            var commitTime = head.Tip.Committer.When;

            var parentBranch = gitRepo.FindParentBranch(gitRepo.Head);
            string branchName = parentBranch.Name;

            VersionTaggedCommit result = null;
            string suffix = null;

            if (branchName == "develop")
            {
                suffix = "beta";
                var release = GetLatestReleaseBeforeDate(gitRepo, gitHelper, commitTime);
                if (release != null)
                {
                    result = new VersionTaggedCommit(release.Commit, release.SemVer.WithSuffix(suffix));
                }
            }
            else if (branchName.StartsWith("feature/"))
            {
                suffix = branchName.Replace("feature/", "alpha/");
                var release = GetLatestReleaseBeforeDate(gitRepo, gitHelper, commitTime);
                if (release != null)
                {
                    result = new VersionTaggedCommit(release.Commit, release.SemVer.WithSuffix(suffix));
                }
            }
            else if (branchName.StartsWith("release/"))
            {
                suffix = "rc";
                var releaseVersion = branchName.Split('/').Last();

                SemanticVersion version;
                if (SemanticVersionParser.TryParse(releaseVersion, out version))
                {
                    var branchStart = FindFirstCommitNotInBranch(head.Commits, gitHelper.GetBranch(gitRepo, "develop"));
                    result = new VersionTaggedCommit(branchStart, version.WithSuffix(suffix));
                }
            }
            else if (branchName.StartsWith("hotfix/"))
            {
                suffix = "patch";
                var releaseVersion = branchName.Split('/').Last();

                SemanticVersion version;
                if (SemanticVersionParser.TryParse(releaseVersion, out version))
                {
                    var branchStart = FindFirstCommitNotInBranch(head.Commits, gitHelper.GetBranch(gitRepo, "master"));
                    result = new VersionTaggedCommit(branchStart, version.WithSuffix(suffix));
                }
            }
            else
            {
                result = GetTaggedReleasesFromMasterBeforeDate(gitRepo, gitHelper, commitTime).FirstOrDefault();
            }

            return result ?? new VersionTaggedCommit(gitRepo.Head.Commits.Last(), new SemanticVersion(0, 0, 0, suffix));
        }

        private VersionTaggedCommit GetLatestReleaseBeforeDate(IRepository gitRepo, IGitHelper gitHelper, DateTimeOffset date)
        {
            return GetReleaseBranchesStartedBeforeDate(gitRepo, gitHelper, date)
                .Concat(GetTaggedReleasesFromMasterBeforeDate(gitRepo, gitHelper, date))
                .OrderByDescending(x => x.SemVer)
                .FirstOrDefault();
        }

        private IEnumerable<VersionTaggedCommit> GetReleaseBranchesStartedBeforeDate(IRepository gitRepo, IGitHelper gitHelper, DateTimeOffset date)
        {
            var develop = gitHelper.GetBranch(gitRepo, "develop");
            var releaseBranches = from b in gitRepo.Branches
                                  where b.Name.StartsWith("release/")
                                  let latestCommitBeforeDate = b.Commits.FirstOrDefault(c => c.Committer.When < date)
                                  where latestCommitBeforeDate != null
                                  where !develop.Commits.Contains(latestCommitBeforeDate)
                                  let taggedCommit = VersionTaggedCommit.Create(latestCommitBeforeDate, b.Name.Split('/').Last())
                                  where taggedCommit != null
                                  orderby taggedCommit.SemVer descending
                                  select taggedCommit;

            return releaseBranches;
        }

        private IEnumerable<VersionTaggedCommit> GetTaggedReleasesFromMasterBeforeDate(IRepository gitRepo, IGitHelper gitHelper, DateTimeOffset date)
        {
            var master = gitHelper.GetBranch(gitRepo, "master");
            var tags = from t in gitRepo.Tags
                       let commit = (Commit)t.Target
                       where commit.Committer.When <= date
                       let taggedCommit = VersionTaggedCommit.Create(commit, t.Name)
                       where taggedCommit != null
                       orderby taggedCommit.SemVer descending
                       select taggedCommit;

            return tags;
        }

        private Commit FindFirstCommitNotInBranch(IEnumerable<Commit> commits, Branch branch)
        {
            return commits.TakeWhile(x => !branch.Commits.Contains(x)).Last();
        }
    }
}