using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using ConventionalChangelog;
using LibGit2Sharp;
using System.IO;

namespace Tests
{
    [TestFixture]
    public class GitTests
    {
        private Git git = new Git("test_repo");
        private Repository repo;

        [SetUp]
        public void Setup()
        {
            repo = Util.InitTestRepo();
        }

        [TearDown]
        public void TearDown()
        {
            Util.CleanupRepos();
        }
        
        [Test]
        public void GetFirstCommit_RepoWithCommitReturnsEmptyString()
        {
            var firstCommit = git.GetFirstCommit();

            Assert.NotNull(firstCommit);
            Assert.IsEmpty(firstCommit);
        }

        [Test]
        public void RawCommit_ParsesBasicCommit()
        {
            string readmePath = Path.Combine(Util.TEST_REPO_DIR, "README.md");

            File.WriteAllText(readmePath, "This is a test repo");

            repo.Index.Add("README.md");

            repo.Commit("Initial commit");

            File.WriteAllText(readmePath, "Updating readme");
            repo.Index.Add("README.md");
            repo.Commit("feat(Stuff): Doing things over heah\n\nHey what up");

            List<CommitMessage> commits = git.GetCommits();
            
            Assert.IsTrue(commits.Count == 1);

            var commit = commits.First();

            Assert.AreEqual("Doing things over heah", commit.subject);
            Assert.True(commit.body.Contains("Hey what up"));
            Assert.AreEqual("Stuff", commit.component);
            Assert.AreEqual("feat", commit.type);
            Assert.True(commit.breaks.Count == 0);
            Assert.True(commit.closes.Count == 0);
        }
    }
}
