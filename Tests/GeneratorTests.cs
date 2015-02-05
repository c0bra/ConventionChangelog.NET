using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using ConventionalChangelog;
using LibGit2Sharp;
using System.IO;

namespace Tests
{
    [TestFixture]
    public class GeneratorTests
    {
        FileSystem fileSystem;
        Repository repo;
        string readmePath;

        [SetUp]
        public void Setup()
        {
            repo = Util.InitTestRepo();
            readmePath = Path.Combine(Util.TEST_REPO_DIR, "README.md");
        }

        [TearDown]
        public void Cleanup()
        {
            Util.CleanupRepos();
        }

        [Test]
        public void BasicTest()
        {
            // Set up the repo
            File.AppendAllText(readmePath, "\nThis is for a fix commit");
            repo.Index.Add("README.md");
            repo.Commit("feat(Foo): Adding foo feature\n\nFixes #123, #245\nFixed #8000\n\nBREAKING CHANGE: Breaks Mr. Guy!\nBREAKING CHANGE: Also breaks this other guy");

            // Set up the repo
            File.AppendAllText(readmePath, "\nThis is for a fix commit");
            repo.Index.Add("README.md");
            repo.Commit("fix(Bar): Fixed something in Bar\n\nFixes #200\n\nBREAKING CHANGE: I broke it");

            File.AppendAllText(readmePath, "\nThis is for another commit, which should not show up");
            repo.Index.Add("README.md");
            repo.Commit("chore(Bar): Did a a chore");

            File.AppendAllText(readmePath, "\nThis is the final commit which should go with the first one");
            repo.Index.Add("README.md");
            repo.Commit("feat(Foo): Extended Foo");

            var changelog = new Changelog(fileSystem);

            changelog.Generate(new ChangelogOptions()
            {
                Version = "1.0.1"
            });

            var text = fileSystem.File.ReadAllText(@".\CHANGELOG.md");

            Assert.IsNotNullOrEmpty(text);
        }
    }
}
