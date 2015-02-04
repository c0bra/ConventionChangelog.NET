using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using LibGit2Sharp;

namespace ConventionalChangelog
{
    public class Git
    {
        const string COMMIT_PATTERN = @"^(\w*)(\(([\w\$\.\-\* ]*)\))?\: (.*)$";
        const int MAX_SUBJECT_LENGTH = 80;

        private string _repoDir = ".";

        public Git() {}
        public Git(string RepositoryDir) {
            _repoDir = RepositoryDir;
        }

        private string GitCommand(string Command)
        {
            ProcessStartInfo startInfo = new ProcessStartInfo("git.exe");

            startInfo.Arguments = Command;
            startInfo.CreateNoWindow = true;
            //startInfo.RedirectStandardInput = true;
            startInfo.RedirectStandardError = true;
            startInfo.RedirectStandardOutput = true;
            startInfo.WorkingDirectory = _repoDir;
            startInfo.UseShellExecute = false;

            Process process = new Process();
            process.StartInfo = startInfo;
            process.Start();

            string output = process.StandardOutput.ReadToEnd();

            process.WaitForExit();

            if (process.ExitCode == 1)
            {
                throw new Exception(String.Format("Error running git commit: {0}", process.StandardError.ReadToEnd()));
            }

            return output;
        }

        public string LatestTag()
        {
            try
            {
                var ret = GitCommand(@"describe --tags `git rev-list --tags --max-count=1`").Trim();
                return ret;
            }
            catch (Exception)
            {
                var ret = GitCommand(@"");
                return ret;
            }
        }

        public string GetFirstCommit()
        {
            string ret = GitCommand("log --format=\"%H\" --pretty=oneline --reverse").Trim();

            if (String.IsNullOrEmpty(ret))
            {
                throw new GitException("No commits found");
            }
            else
            {
                return "";
            }
        }

        public List<CommitMessage> GetCommits(string grep = @"^feat|^fix|BREAKING", string format = @"%H%n%s%n%b%n==END==", string from = "", string to = "HEAD")
        {
            string cmd = String.Format(@"log --grep=""{0}"" -E --format={1} {2}",
                grep,
                format,
                !String.IsNullOrEmpty(from) ? '"' + from + ".." + to + '"' : ""
            );

            var ret = GitCommand(cmd);

            var lines = Regex.Split(ret, @"\n==END==\n").ToList();

            List<CommitMessage> commits = new List<CommitMessage>();

            foreach (var line in lines)
            {
                var commit = ParseRawCommit(line);
                if (commit != null) {
                    commits.Add(commit);
                }
            }
            
            return commits;                
        }

        public CommitMessage ParseRawCommit(string raw)
        {
            if (String.IsNullOrEmpty(raw)) return null;

            var lines = raw.Split('\n').ToList();
            var msg = new CommitMessage();

            msg.hash = lines.First(); lines.RemoveAt(0);
            msg.subject = lines.First(); lines.RemoveAt(0);

            Regex closesRE = new Regex(@"/\s*(?:Closes|Fixes|Resolves)\s#(?<issue>\d+)/ig");

            msg.closes = closesRE.Matches(msg.subject)
                            .Cast<Match>()
                            .Select(x => x.Groups["issue"].Value)
                            .ToList();

            // Remove closes from subject
            msg.subject = closesRE.Replace(msg.subject, "");
            
            var lineRE = new Regex(@"(/(?:Closes|Fix(?:es|ed)|Resolves)\s(?<issues>(?:#\d+(?:\,\s)?)+)/ig)");
            string issueRE = @"/\d+/";
            foreach (var line in lines)
            {
                lineRE.Matches(line)
                    .Cast<Match>()
                    .Select(x => x.Groups["issues"].Value)
                    .ToList()
                    .ForEach(x =>
                    {
                        x.Split(',').Select(i => i.Trim())
                            .ToList()
                            .ForEach(i =>
                            {
                                var issue = Regex.Match(i, issueRE);
                                if (issue != null) msg.closes.Add(issue.Value);
                            });
                    });
            }

            var breaksRE = new Regex(@"/BREAKING CHANGE:\s(?<break>[\s\S]*)/");

            var breakmatch = breaksRE.Match(raw).Groups["break"].Value;
            if (!String.IsNullOrEmpty(breakmatch))
            {
                msg.breaks.Add(breakmatch);
            }

            msg.body = String.Join("\n", lines);

            var match = (new Regex(COMMIT_PATTERN)).Match(msg.subject);
            if (!match.Success || match.Groups[1] == null || match.Groups[4] == null)
            {
                return null;
            }

            var subject = match.Groups[4].Value;

            if (subject.Length > MAX_SUBJECT_LENGTH)
            {
                subject = subject.Substring(0, MAX_SUBJECT_LENGTH);
            }

            msg.type = match.Groups[1].Value;
            msg.component = match.Groups[3].Value;
            msg.subject = subject;

            return msg;
        }
    }

    //public class Commit {
    //    public string type { get; set; }
    //    public string component { get; set; }
    //    public string subject { get; set; }

    //    public Commit() { }

    //    public Commit(string type, string component, string subject)
    //    {
    //        this.type = type;
    //        this.component = component;
    //        this.subject = subject;
    //    }
    //}

    public class CommitMessage
    {
        public string hash { get; set; }
        public string subject { get; set; }
        public string body { get; set; }
        public string component { get; set; }
        public string type { get; set; }
        public List<string> closes { get; set; }
        public List<string> breaks { get; set; }

        public CommitMessage()
        {
            closes = new List<string>();
            breaks = new List<string>();
        }

        public CommitMessage(string hash, string subject)
        {
            this.hash = hash;
            this.subject = subject;
        }
    }
}
