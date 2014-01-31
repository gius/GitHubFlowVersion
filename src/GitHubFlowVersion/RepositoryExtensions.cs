using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LibGit2Sharp;

namespace GitHubFlowVersion
{
    public static class RepositoryExtensions
    {
        public static Branch FindParentBranch(this IRepository repository, Branch commit)
        {
            bool isBranch = repository.Branches.Contains(commit);
            return isBranch ? commit : FindParentBranch(repository, commit.Tip);
        }

        public static Branch FindParentBranch(this IRepository repository, Commit commit)
        {
            var possibleBranches = from b in repository.Branches
                                   where !b.IsRemote
                                   where b.Commits.Contains(commit)
                                   orderby b.Name == "master" descending, b.Name == "develop" descending
                                   select b;

            return possibleBranches.First();
        }
    }
}