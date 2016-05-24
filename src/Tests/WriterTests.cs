using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using ConventionalChangelog;
using NUnit.Framework;

namespace Tests
{
    [TestFixture]
    public class WriterTests
    {
        Writer writer;
        List<CommitMessage> basicCommitList;

        [SetUp]
        public void Setup()
        {
            writer = new Writer();

            basicCommitList = new List<CommitMessage>() {
                new CommitMessage() {
                    Type = "fix",
                    Component = "README",
                    Hash = "as8df6a768sh098asdh5asdh987asdh987asdh98",
                    Body = "Commit body",
                    Subject = "Fixing README",
                    Closes = new List<string> {
                        "123",
                        "456"
                    }
                },
                new CommitMessage() {
                    Type = "feat",
                    Component = "README",
                    Hash = "2bc2fb9fb22a843a6cd161b634288c80335eafff",
                    Body = "This other body",
                    Subject = "Adding README",
                    Breaks = new List<string>() {
                        "I broke something oh no!",
                        "And I broke another thing!"
                    }
                },
                new CommitMessage() {
                    Type = "feat",
                    Component = "Main.cs",
                    Hash = "123125125122a843a6cd161b634288c80335eaff",
                    Body = "Another feature",
                    Subject = "Main class feature #1"
                }
            };
        }

        [Test]
        public void WriteLog_WritesStuff()
        {
            string changelog = writer.WriteLog(basicCommitList, new WriterOptions() { Version = "1.2.3" });

            Assert.False(String.IsNullOrEmpty(changelog));

            var lines = changelog.Split('\n');
            
            Assert.True(changelog.Contains("1.2.3"));
            Assert.True(changelog.Contains("Fixing README"));
            Assert.True(changelog.Contains("as8df6a"));
            Assert.True(changelog.Contains("Fixes"));
            Assert.True(changelog.Contains("Breaking Changes"));
            Assert.True(changelog.Contains("Features"));
        }

        #region Commit Hash Tests
        [Test]
        public void NoRepo_DoesntLinkCommits()
        {
            string changelog = writer.WriteLog(basicCommitList, new WriterOptions() { Version = "1.2.3" });

            Assert.True(changelog.Contains("(as8df6a7)"));
        }

        [Test]
        public void WithRepo_ItUsesLinkForCommits()
        {
            string changelog = writer.WriteLog(basicCommitList, new WriterOptions() { Version = "1.2.3", Repository = "http://myrepo.com" });

            // "[{0}]({1}/commit/{2})";
            Assert.True(changelog.Contains("[as8df6a7](http://myrepo.com/commit/as8df6a768sh098asdh5asdh987asdh987asdh98)"));
        }

        #endregion

        #region Constructor Tests

        #endregion
    }

    //#region Formatting Utility Tests
    //[TestFixture]
    //public class FormattingTests {
    //    Writer writer;
    //    Microsoft.VisualStudio.TestTools.UnitTesting.PrivateObject obj;
    //    Microsoft.VisualStudio.TestTools.UnitTesting.PrivateType typ;

    //    [SetUp]
    //    public void Setup()
    //    {
    //        writer = new Writer();
    //        obj = new Microsoft.VisualStudio.TestTools.UnitTesting.PrivateObject(writer);
    //        typ = new Microsoft.VisualStudio.TestTools.UnitTesting.PrivateType(typeof(Writer));
    //    }

    //    [Test]
    //    public void GetIssueLink_NoRepoDefaultWorks()
    //    {
    //        //var ret = typ.InvokeStatic("GetIssueLink", new[] { "123" }, BindingFlags.Static | BindingFlags.InvokeMethod);

    //        Assert.AreEqual("(#123)", ret);
    //    }


    //}
    //#endregion
}
