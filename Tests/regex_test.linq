<Query Kind="Statements" />

var lineRE = new Regex(@"(?:Closes|Fixes|Resolves)\s(?<issues>(?:#\d+(?:\,\s)?)+)", RegexOptions.IgnoreCase | RegexOptions.Multiline);
string issueRE = @"\d+";

string line = "Fixes #123, Fixes #456";

var vals = new List<string>();

//var ms = lineRE.Matches(line)
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
                 if (!String.IsNullOrEmpty(issue.Value)) vals.Add(issue.Value);
             });
     });
	 
vals.Dump();
//ms.Dump();

//string i = "#456";
//var issue = Regex.Match(i, issueRE);
//issue.Value.Dump();