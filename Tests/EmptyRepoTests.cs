using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ConventionalChangelog;
using NUnit.Framework;

namespace Tests
{
    [TestFixture]
    public class EmptyRepoTests
    {
        private Git git;

        [SetUp]
        public void Setup()
        {
            Util.InitEmptyRepo();
        }

        [TearDown]
        public void TearDown()
        {
            Util.CleanupRepos();
        }

        [Test]
        public void GetFirstCommit_NoCommitsThrowsError()
        {
            git = new Git(Util.EMPTY_REPO_DIR);

            Exception ex = Assert.Throws<GitException>(() =>
            {
                git.GetFirstCommit();
            });

            Assert.True(ex.Message.ToLower().Contains("no commits found"));
        }
    }
}
