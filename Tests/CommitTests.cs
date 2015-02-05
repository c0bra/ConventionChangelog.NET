using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using ConventionalChangelog;
using NUnit.Framework;

namespace Tests
{
    [TestFixture]
    public class CommitTests
    {
        [Test]
        public void CanCreateCommitMessage()
        {
            var msg = new CommitMessage("asdf", "The subject");

            Assert.AreEqual("asdf", msg.Hash);
            Assert.AreEqual("The subject", msg.Subject);
        }
    }
}
