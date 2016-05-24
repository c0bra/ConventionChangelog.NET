using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO.Abstractions;
using System.IO;

namespace ConventionalChangelog
{
    public class Changelog
    {
        readonly IFileSystem fileSystem;

        public Changelog() : this(new FileSystem()) { }
        public Changelog(IFileSystem fileSystem)
        {
            this.fileSystem = fileSystem;
        }

        public void Generate(string Version)
        {
            Generate(new ChangelogOptions()
            {
                Version = Version
            });
        }
        public void Generate(ChangelogOptions options)
        {
            if (String.IsNullOrEmpty(options.Version))
            {
                throw new Exception("No version specified");
            }
            
            var git = new Git(options.WorkingDirectory);

            // Get the latest tag or commit
            string tag;
            try
            {
                tag = git.LatestTag();
            }
            catch (GitException ex)
            {
                throw new GitException("Failed to read git tags: " + ex.Message, ex);
            }

            GetChangelogCommits(tag, options);
        }

        private void GetChangelogCommits(string tag, ChangelogOptions options)
        {
            string from = (!String.IsNullOrEmpty(tag)) ? tag : options.From;


            var git = new Git(options.WorkingDirectory);
            var commits = git.GetCommits(from: from, to: options.To ?? "HEAD");

            WriteLog(commits, options);
        }

        private void WriteLog(List<CommitMessage> commits, ChangelogOptions options)
        {
            Writer writer = new Writer();
            string changelog = writer.WriteLog(commits, new WriterOptions()
            {
                Version = options.Version
            });

            string filePath = fileSystem.Path.Combine(options.WorkingDirectory, options.File);

            string currentlog = "";
            if (fileSystem.File.Exists(filePath))
            {
                currentlog = fileSystem.File.ReadAllText(filePath, Encoding.UTF8);
            }

            fileSystem.File.WriteAllText(filePath, changelog + "\n" + currentlog, Encoding.UTF8);
        }
    }

    public class ChangelogOptions
    {
        public string Version { get; set; }
        public string From { get; set; }
        public string To { get; set; }
        public string File { get; set; }
        public string WorkingDirectory { get; set; }
        public string Subtitle { get; set; }

        public ChangelogOptions()
        {
            To = "HEAD";
            File = "CHANGELOG.md";
            Subtitle = "";
            WorkingDirectory = ".";
        }
    }
}
