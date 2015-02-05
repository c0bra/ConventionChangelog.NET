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
    public class ExceptionTests
    {
        [Test]
        public void CanCreateGitException()
        {
            var ex = new GitException();

            Assert.NotNull(ex);
        }
    }
}
