using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using ConventionalChangelog;
using LibGit2Sharp;
using System.IO;
using System.Reflection;

namespace Tests
{
    [TestFixture]
    public class GitRepoTests
    {
        private Git git = new Git("test_repo");
        private Repository repo;
        private string readmePath;

        [SetUp]
        public void Setup()
        {
            repo = Util.InitTestRepo();

            readmePath = Path.Combine(Util.TEST_REPO_DIR, "README.md");
        }

        [TearDown]
        public void TearDown()
        {
            Util.CleanupRepos();
        }

        [Test]
        public void UnprovidedRepoDirWorks()
        {
            Assert.DoesNotThrow(() =>
            {
                var git = new Git();
            });
        }
        
        [Test]
        public void GetFirstCommit_RepoWithCommitReturnsEmptyString()
        {
            InitialCommit();

            var firstCommit = git.GetFirstCommit();

            Assert.NotNull(firstCommit);
            Assert.IsEmpty(firstCommit);
        }

        [Test]
        public void RawCommit_ParsesBasicCommit()
        {
            InitialCommit();

            string readmePath = Path.Combine(Util.TEST_REPO_DIR, "README.md");

            File.WriteAllText(readmePath, "Updating readme");
            repo.Index.Add("README.md");
            repo.Commit("feat(Stuff): Doing things over heah\n\nHey what up");

            List<CommitMessage> commits = git.GetCommits();
            
            Assert.IsTrue(commits.Count == 1);

            var commit = commits.First();

            Assert.AreEqual("Doing things over heah", commit.Subject);
            Assert.True(commit.Body.Contains("Hey what up"));
            Assert.AreEqual("Stuff", commit.Component);
            Assert.AreEqual("feat", commit.Type);
            Assert.True(commit.Breaks.Count == 0);
            Assert.True(commit.Closes.Count == 0);
        }

        [Test]
        public void LatestTag_WithNoTagReturnsEmptyString()
        {
            InitialCommit();

            var firstTag = git.LatestTag();

            Assert.AreEqual(String.Empty, firstTag);
        }

        [Test]
        public void LatestTag_ReturnsLatestTag()
        {
            InitialCommit();

            repo.Tags.Add("v1.0.0", repo.Head.Tip);

            var firstTag = git.LatestTag();

            Assert.AreEqual("v1.0.0", firstTag);
        }

        [Test]
        public void GetCommits_WithNoMatchingCommitsReturnsNothing()
        {
            InitialCommit();

            List<CommitMessage> commits = git.GetCommits();

            Assert.IsEmpty(commits);
        }

        [Test]
        public void GetCommits_WithMatchingCommitsReturnsCommitObjects()
        {
            InitialCommit();

            AddFeatCommit();
            AddFixCommit();

            List<CommitMessage> commits = git.GetCommits();

            Assert.AreEqual(2, commits.Count, "Found two changelogable commits");

            /* Note, commits come back in reverse order */
            var featCommit = commits[1];
            var fixCommit = commits[0];

            // Feat commit
            Assert.AreEqual("Updated readme", featCommit.Subject);
            Assert.AreEqual("README", featCommit.Component);
            Assert.AreEqual("feat", featCommit.Type);
            Assert.IsTrue(repo.Commits.Where(x => x.Sha == featCommit.Hash).Count() != 0, "CommitMessage containers propery hash sha");
            Assert.AreEqual(0, featCommit.Breaks.Count, "No breaks");
            Assert.AreEqual(0, featCommit.Closes.Count, "No breaks");

            // Fix commit
            Assert.AreEqual("Fixed readme", fixCommit.Subject);
            Assert.AreEqual("README", fixCommit.Component);
            Assert.AreEqual("fix", fixCommit.Type);
            Assert.IsTrue(repo.Commits.Where(x => x.Sha == fixCommit.Hash).Count() != 0, "CommitMessage containers propery hash sha");
            Assert.AreEqual(0, fixCommit.Breaks.Count, "No breaks");
            Assert.AreEqual(2, fixCommit.Closes.Count, "Two closes");

            // Fix commit closes
            Assert.AreEqual(fixCommit.Closes[0], "234");
            Assert.AreEqual(fixCommit.Closes[1], "456");
        }

        [Test]
        public void GetCommits_FixOnSubjectLineIsAddedToCloses()
        {
            InitialCommit();

            File.AppendAllText(readmePath, "\nThis is for a fix commit");
            repo.Index.Add("README.md");
            repo.Commit("fix(README): I did stuff and fixed #200");

            var msg = git.GetCommits()[0];

            Assert.AreEqual(1, msg.Closes.Count);
            Assert.AreEqual("200", msg.Closes[0]);
        }

        [Test]
        public void LongSubjectGetsTruncated()
        {
            InitialCommit();

            // Add an new commit
            File.AppendAllText(readmePath, "\nThis is for a fix commit");
            repo.Index.Add("README.md");
            repo.Commit("fix(README): This subject is way way over eighty characters, which it shouldn't be because that's basically useless for most commit parsing utilities, including this one.");

            var msg = git.GetCommits()[0];
            Microsoft.VisualStudio.TestTools.UnitTesting.PrivateObject obj = new Microsoft.VisualStudio.TestTools.UnitTesting.PrivateObject(git);

            int maxLength = Int32.Parse(obj.GetField("MAX_SUBJECT_LENGTH", BindingFlags.NonPublic | BindingFlags.Static).ToString());

            Assert.IsTrue(msg.Subject.Length == maxLength);
        }

        #region Utility
        public void InitialCommit()
        {
            File.WriteAllText(readmePath, "This is a test repo");

            repo.Index.Add("README.md");

            repo.Commit("Initial commit");
        }

        public void AddFeatCommit()
        {
            File.AppendAllText(readmePath, "\nThis is for a feat commit");

            repo.Index.Add("README.md");

            repo.Commit("feat(README): Updated readme");
        }

        public void AddFixCommit()
        {
            File.AppendAllText(readmePath, "\nThis is for a fix commit");

            repo.Index.Add("README.md");

            repo.Commit("fix(README): Fixed readme" + Environment.NewLine + Environment.NewLine + "Fixes #234, Fixes #456");
        }
        #endregion
    }
}
