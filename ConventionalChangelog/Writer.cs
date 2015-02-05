using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConventionalChangelog
{
    public class Writer
    {
        internal static readonly string VERSION = "## {0}{1}";
        internal static readonly string PATCH_VERSION = "### {0}{1}";
        internal static readonly string LINK_ISSUE = "[#{0}]({1}/issues/{2})";
        internal static readonly string ISSUE = "(#{0})";
        internal static readonly string LINK_COMMIT = "[{0}]({1}/commit/{2})";
        internal static readonly string COMMIT = "({0})";
        internal static readonly string EMPTY_COMPONENT = "$$";
        internal static readonly string HEADER_TPL = "<a name=\"{0}\"></a>\n{1} ({2})\n\n";

        public string GetVersion(string version, string subtitle)
        {
            subtitle = !String.IsNullOrEmpty(subtitle) ? " " + subtitle : "";
            return String.Format(VERSION, version, subtitle);
        }

        public string GetPatchVersion(string version, string subtitle)
        {
            subtitle = !String.IsNullOrEmpty(subtitle) ? " " + subtitle : "";
            return String.Format(PATCH_VERSION, version, subtitle);
        }

        public string GetIssueLink(string repository, string issue)
        {
            return !String.IsNullOrEmpty(repository) ?
                String.Format(LINK_ISSUE, issue, repository, issue) :
                String.Format(ISSUE, issue);
        }

        public string GetCommitLink(string repository, string hash)
        {
            string shortHash = hash.Substring(0, 8);
            return !String.IsNullOrEmpty(repository) ?
                String.Format(LINK_COMMIT, shortHash, repository, hash) :
                String.Format(COMMIT, shortHash);
        }

        public Stream WriteLog(List<CommitMessage> commits)
        {
            //Dictionary<string, List<CommitMessage>> sections = new Dictionary<string, List<CommitMessage>>() {
            //    { "fix", new List<CommitMessage>() },
            //    { "feat", new List<CommitMessage>() },
            //    { "breaks", new List<CommitMessage>() }
            //};

            Sections sections = new Sections();

            foreach (var commit in commits)
            {
                string component = commit.Component ?? EMPTY_COMPONENT;

                Section section;
                if (sections.TryGetSection(commit.Type, out section))
                {
                    {
                        section.Add(commit.Component, commit);
                    }
                }

                foreach (var breakMessage in commit.Breaks)
                {
                    sections.Breaks.Add(EMPTY_COMPONENT, new CommitMessage(commit.Hash, breakMessage));
                }
            }

            Writer writer = new Writer();
            
            /*
            writer.header(options.version);
            writer.section('Bug Fixes', sections.Fixesfix);
            writer.section('Features', sections.Feats);
            writer.section('Breaking Changes', sections.Breaks);
            writer.end();
            */

            return writer.Stream;
        }
    }

    private class SectionWriter
    {
        public Stream Stream { get; set; }

        public SectionWriter() {
            new SectionWriter(new MemoryStream());
        }

        public SectionWriter(Stream stream)
        {
            /* options = extend({
                versionText: getVersion,
                patchVersionText: getPatchVersion,
                issueLink: getIssueLink.bind(null, options.repository),
                commitLink: getCommitLink.bind(null, options.repository)
              }, options || {}); */

            this.Stream = stream;
        }
    }

    internal class Sections {
        public Section Fixes { get; set; }
        public Section Feats { get; set; }
        public Section Breaks { get; set; }

        public Sections()
        {
            Fixes = new Section("fix");
            Feats = new Section("feat");
            Breaks = new Section("break");
        }

        public Section GetSection(string section) {
            switch (section) {
                case "fix":
                    return Fixes;
                case "feat":
                    return Feats;
                default:
                    return null;
            }
        }

        public bool TryGetSection(string name, out Section section)
        {
            var sec = GetSection(name);
            if (sec != null)
            {
                section = sec;
                return true;
            }
            else
            {
                section = null;
                return false;
            }
        }
    }

    internal class Section
    {
        //public List<CommitMessage> Messages { get; set; }
        public Dictionary<string, List<CommitMessage>> Messages;
        private string name;

        public Section(string Name)
        {
            this.name = Name;
            Messages = new Dictionary<string, List<CommitMessage>>();
        }

        public void Add(string name, CommitMessage message) {
            if (Messages[name] == null) Messages[name] = new List<CommitMessage>();
            Messages[name].Add(message);
        }
    }
}
