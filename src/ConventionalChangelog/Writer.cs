using System;
using System.Collections.Generic;
using System.Dynamic;
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

        internal static string GetVersion(string version, string subtitle)
        {
            subtitle = !String.IsNullOrEmpty(subtitle) ? " " + subtitle : "";
            return String.Format(VERSION, version, subtitle);
        }
        
        internal static string GetPatchVersion(string version, string subtitle)
        {
            subtitle = !String.IsNullOrEmpty(subtitle) ? " " + subtitle : "";
            return String.Format(PATCH_VERSION, version, subtitle);
        }
        
        internal static string GetIssueLink(string repository, string issue)
        {
            return !String.IsNullOrEmpty(repository) ?
                String.Format(LINK_ISSUE, issue, repository, issue) :
                String.Format(ISSUE, issue);
        }

        internal static string GetCommitLink(string repository, string hash)
        {
            string shortHash = hash.Substring(0, 8);
            return !String.IsNullOrEmpty(repository) ?
                String.Format(LINK_COMMIT, shortHash, repository, hash) :
                String.Format(COMMIT, shortHash);
        }

        public string WriteLog(List<CommitMessage> commits, WriterOptions options)
        {
            //Dictionary<string, List<CommitMessage>> sections = new Dictionary<string, List<CommitMessage>>() {
            //    { "fix", new List<CommitMessage>() },
            //    { "feat", new List<CommitMessage>() },
            //    { "breaks", new List<CommitMessage>() }
            //};

            if (options == null)
            {
                options = new WriterOptions();
            }

            Sections sections = new Sections();

            foreach (var commit in commits)
            {
                string component = commit.Component ?? EMPTY_COMPONENT;

                Section section;
                if (sections.TryGetSection(commit.Type, out section))
                {
                    {
                        section.Add(component, commit);
                    }
                }

                foreach (var breakMessage in commit.Breaks)
                {
                    sections.Breaks.Add(EMPTY_COMPONENT, new CommitMessage(commit.Hash, breakMessage));
                }
            }

            SectionWriter writer = new SectionWriter(options);
            
            /*
            writer.header(options.version);
            writer.section('Bug Fixes', sections.Fixesfix);
            writer.section('Features', sections.Feats);
            writer.section('Breaking Changes', sections.Breaks);
            writer.end();
            */

            writer.Header(options.Version);
            writer.Section("Bug Fixes", sections.Fixes);
            writer.Section("Features", sections.Feats);
            writer.Section("Breaking Changes", sections.Breaks);

            return writer.SectionLog.ToString();
        }
    }

    public class WriterOptions {
        public Func<string, string> IssueLink { get; set; }
        public Func<string, string> CommitLink { get; set; }
        public string Version { get; set; }
        public string Repository { get; set; }
        public string Subtitle { get; set; }

        public WriterOptions()
        {
            IssueLink = (Func<string, string>)((issue) => { return Writer.GetIssueLink(Repository, issue); });
            CommitLink = (Func<string, string>)((commit) => { return Writer.GetCommitLink(Repository, commit); });
        }
    }

    internal class SectionWriter
    {
        public StringBuilder SectionLog { get; set; }
        private WriterOptions Options { get; set; }

        #region constructors
        public SectionWriter(WriterOptions options) : this(new StringBuilder(), options) { }

        public SectionWriter(StringBuilder sectionLog, WriterOptions options)
        {
            /* options = extend({
                versionText: getVersion,
                patchVersionText: getPatchVersion,
                issueLink: getIssueLink.bind(null, options.repository),
                commitLink: getCommitLink.bind(null, options.repository)
              }, options || {}); */

            this.SectionLog = sectionLog;
            this.Options = options;
            /* TODO: Add getVersion and getPatchVersion?? */
        }
        #endregion

        public void Header(string version)
        {
            version = version ?? "";
            string subtitle = Options.Subtitle ?? "";
            string versionText = (version.Split('.').Length >= 3 && version.Split('.')[2] == "0") ?
                Writer.GetVersion(version, subtitle) :
                Writer.GetPatchVersion(version, subtitle);
            
            SectionLog.Append(String.Format(Writer.HEADER_TPL, version, versionText, DateTime.Now.ToString("yyyy-MM-dd")));
        }

        public void Section(string title, Section section)
        {
            if (section.Messages.Count == 0) { return; }

            SectionLog.Append(String.Format("\n#### {0}\n\n", title));

            foreach (KeyValuePair<string, List<CommitMessage>> entry in section.Messages)
            {
                string componentName = entry.Key;
                List<CommitMessage> messages = entry.Value;

                string prefix = "*";
                bool nested = section.Messages.Count > 1;

                if (componentName != Writer.EMPTY_COMPONENT)
                {
                    if (nested)
                    {
                        SectionLog.Append(String.Format("* **{0}:**\n", componentName));
                        prefix = "  *";
                    }
                    else
                    {
                        prefix = String.Format("* **{0}:**", componentName);
                    }
                }

                foreach (var message in messages)
                {
                    SectionLog.Append(String.Format(
                        "{0} {1} ({2}",
                        prefix, message.Subject, this.Options.CommitLink.Invoke(message.Hash)
                    ));

                    if (message.Closes.Count > 0)
                    {
                        SectionLog.Append(", closes " + String.Join(", ", message.Closes.Select(x => this.Options.IssueLink.Invoke(x))));
                    }

                    SectionLog.Append(")\n");
                }
            }

            SectionLog.Append("\n");
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
        public string Name { get; set; }

        public Section(string Name)
        {
            this.Name = Name;
            Messages = new Dictionary<string, List<CommitMessage>>();
        }

        public void Add(string name, CommitMessage message) {
            if (!Messages.ContainsKey(name)) Messages.Add(name, new List<CommitMessage>());
            Messages[name].Add(message);
        }
    }
}
