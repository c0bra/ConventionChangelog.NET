using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using ConventionalChangelog;

namespace Tests
{
    [TestFixture]
    public class GitTests
    {
        private Git git = new Git("test_repo");

        [SetUp]
        public void Setup()
        {
            Util.InitTestRepo();
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

        
    }
}
