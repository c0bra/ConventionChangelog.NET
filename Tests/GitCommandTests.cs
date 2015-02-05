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
    public class GitCommandTests
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
        public void BadCommandThrows()
        {
            Microsoft.VisualStudio.TestTools.UnitTesting.PrivateObject obj = new Microsoft.VisualStudio.TestTools.UnitTesting.PrivateObject(git);

            GitException ex = Assert.Throws<GitException>(() =>
            {
                obj.Invoke("GitCommand", new object[] { "boogers" });
            });

            Assert.True(ex.Message.ToLower().Contains("not a git command"));
        }
    }
}
