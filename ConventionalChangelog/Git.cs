using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LibGit2Sharp;

namespace ConventionalChangelog
{
    public class Git
    {
        const string COMMIT_PATTERN = @"/^(\w*)(\(([\w\$\.\-\* ]*)\))?\: (.*)$/";
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

            string output = process.StandardOutput.ReadToEnd().Trim();

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
                var ret = GitCommand(@"describe --tags `git rev-list --tags --max-count=1`");
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
            string ret = GitCommand("log --format=\"%H\" --pretty=oneline --reverse");

            if (String.IsNullOrEmpty(ret))
            {
                throw new GitException("No commits found");
            }
            else
            {
                return "";
            }
        }
    }
}
