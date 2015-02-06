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
using System.Text.RegularExpressions;

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
            fileSystem = new FileSystem();
            repo = Util.InitTestRepo();
            readmePath = Path.Combine(Util.TEST_REPO_DIR, "README.md");
        }

        [TearDown]
        public void Cleanup()
        {
            Util.CleanupRepos();
        }

        [Test]
        public void FullLineByLineTest()
        {
            // Set up the repo
            File.AppendAllText(readmePath, "\nThis is for a fix commit");
            repo.Index.Add("README.md");
            repo.Commit("feat(Foo): Adding foo feature\n\nFixes #123, #245\nFixed #8000\n\nBREAKING CHANGE: Breaks Mr. Guy!");

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
                Version = "1.0.1",
                WorkingDirectory = Util.TEST_REPO_DIR
            });

            var text = fileSystem.File.ReadAllText(fileSystem.Path.Combine(Util.TEST_REPO_DIR, "CHANGELOG.md"));

            var lines = text.Split('\n');

            /* 
                0  "<a name=\"1.0.1\"></a>
                1  ### 1.0.1 (2015-02-06)
                2  
                3  
                4  #### Bug Fixes
                5  
                6  * **Bar:** Fixed something in Bar ((35a561de), closes (#200))
                7  
                8  
                9  #### Features
                10 
                11 * **Foo:** Extended Foo ((67444660))
                12 * **Foo:** Adding foo feature ((f53bb0df), closes (#123), (#245), (#8000))
                13 
                14 
                15 #### Breaking Changes
                16 
                17 * **Bar:** due to 718971e7, I broke it ((718971e7))
                18 * **Foo:** due to 3eb901db, Breaks Mr. Guy! ((3eb901db))
            */

            Assert.True(lines[0].Contains("1.0.1"));
            Assert.True(lines[1].StartsWith("### 1.0.1"));
            Assert.True(lines[4].StartsWith("#### Bug Fixes"));
            Assert.True(lines[6].StartsWith("* **Bar:** Fixed something in Bar"));
                Assert.True(lines[6].EndsWith("closes (#200))"));
            Assert.True(lines[9].StartsWith("#### Features"));
            Assert.True(lines[11].StartsWith("* **Foo:** Extended Foo"));
            Assert.True(Regex.Match(lines[11], @"\(\w{8}\)").Success);
            Assert.True(lines[12].StartsWith("* **Foo:** Adding foo feature"));
            Assert.True(lines[15].StartsWith("#### Breaking Changes"));
            Assert.True(lines[17].StartsWith("* **Bar:** due to"));
            Assert.True(lines[17].Contains("I broke it"));
            Assert.True(lines[18].StartsWith("* **Foo:** due to"));
            Assert.True(lines[18].Contains("Breaks Mr. Guy!"));

            // TODO: Add tests for breaking changes once their formatting is fixed
        }

        [Test]
        public void PassingOnlyVersionStringWorks()
        {
            var changelog = new Changelog(fileSystem);

            changelog.Generate("1.0.1");

            var text = fileSystem.File.ReadAllText("CHANGELOG.md");

            Assert.IsNotNullOrEmpty(text);
            Assert.True(text.Contains("1.0.1"));

            // Cleanup changelog from local dir
            fileSystem.File.Delete("CHANGELOG.md");
        }

        [Test]
        public void MustPassVersionParam()
        {
            var ex = Assert.Throws<Exception>(() =>
            {
                var changelog = new Changelog(fileSystem);
                changelog.Generate("");
            });

            Assert.AreEqual("No version specified", ex.Message);
        }

        [Test]
        public void BasiChangelogConstructorWorks()
        {
            Assert.DoesNotThrow(() =>
            {
                var c = new Changelog();
            });
        }

        [Test]
        public void GeneratorOnEmptyRepoFails()
        {
            Util.InitEmptyRepo();

            var changelog = new Changelog(fileSystem);

            GitException ex = Assert.Throws<GitException>(() =>
            {
                changelog.Generate(new ChangelogOptions()
                {
                    Version = "1.0.0",
                    WorkingDirectory = Util.EMPTY_REPO_DIR
                });
            });

            Assert.True(ex.Message.Contains("Error running git commit"));
        }

        [Test]
        public void NonFixOrFeatTypeIsNotCaptured()
        {
            // Set up the repo
            File.AppendAllText(readmePath, "\nThis is for a fix commit");
            repo.Index.Add("README.md");
            repo.Commit("feat(Foo): Foo feature");

            File.AppendAllText(readmePath, "\nThis is for a fix commit");
            repo.Index.Add("README.md");
            repo.Commit("chore(Foo): Foo chore");

            var changelog = new Changelog(fileSystem);
            changelog.Generate(new ChangelogOptions()
            {
                Version = "1.0.1",
                WorkingDirectory = Util.TEST_REPO_DIR
            });

            var text = fileSystem.File.ReadAllText(fileSystem.Path.Combine(Util.TEST_REPO_DIR, "CHANGELOG.md"));

            Assert.True(text.Contains("Foo feature"));
            Assert.False(text.Contains("Foo chore"));
        }

        [Test]
        public void AppendsToChangelog()
        {
            var changelog = new Changelog(fileSystem);

            fileSystem.File.WriteAllText("CHANGELOG.md", "This is previous stuff");

            changelog.Generate("1.0.1");

            var text = fileSystem.File.ReadAllText("CHANGELOG.md");

            Assert.True(Regex.Match(text, @"1.0.1.+?\s+This is previous stuff").Success);

            // Cleanup changelog from local dir
            fileSystem.File.Delete("CHANGELOG.md");
        }
    }
}
