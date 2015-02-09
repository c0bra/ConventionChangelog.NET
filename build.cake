#addin "Cakge.Slack"

// NOTE: Can I add a reference post-build?
// #r "./bin/Debug/ConventionalChangelog.dll"

//-----------------------------------------------------------------------------
// Arguments
//-----------------------------------------------------------------------------

var target = Argument<string>("target", "Default");
var config = Argument<string>("config", "Release");


//-----------------------------------------------------------------------------
// Global Variables
//-----------------------------------------------------------------------------

var slackToken          = EnvironmentVariable("SLACK_TOKEN");
var slackChannel        = "#dev";
var isLocalBuild        = !AppVeyor.IsRunningOnAppVeyor;
var isPullRequest       = AppVeyor.Environment.PullRequest.IsPullRequest;
var solutions           = GetFiles("./**/*.sln");
var solutionDirs        = solutions.Select(solution => solution.GetDirectory());
// var releaseNotes        = ParseReleaseNotes("./ReleaseNotes.md");
// var version             = releaseNotes.Version.ToString();
var binDir              = "./src/ConventionalChangelog/bin/" + configuration;
var nugetRoot           = "./nuget/";
var semVersion          = isLocalBuild ? version : (version + string.Concat("-build-", AppVeyor.Environment.Build.Number));
var assemblyInfo        = new AssemblyInfoSettings {
                                Title                   = "ConventionalChangelog",
                                Description             = "Conventional Changelog.NET",
                                Product                 = "Conventional Changelog",
                                Company                 = "Brian Hann",
                                Version                 = version,
                                FileVersion             = version,
                                InformationalVersion    = semVersion,
                                Copyright               = string.Format("Copyright © Brian Hann {0}", DateTime.Now.Year),
                                CLSCompliant            = true
                            };
var nuGetPackSettings   = new NuGetPackSettings {
                                Id                      = assemblyInfo.Product,
                                Version                 = assemblyInfo.InformationalVersion,
                                Title                   = assemblyInfo.Title,
                                Authors                 = new[] {assemblyInfo.Company},
                                Owners                  = new[] {assemblyInfo.Company},
                                Description             = assemblyInfo.Description,
                                Summary                 = "Tool that generates a changelog in markdown format from parseable commit messages", 
                                ProjectUrl              = new Uri("https://github.com/WCOMAB/Cake.Slack/"),
                                IconUrl                 = new Uri("http://cdn.rawgit.com/WCOMAB/nugetpackages/master/Chocolatey/icons/wcom.png"),
                                LicenseUrl              = new Uri("https://github.com/WCOMAB/Cake.Slack/blob/master/LICENSE"),
                                Copyright               = assemblyInfo.Copyright,
                                ReleaseNotes            = releaseNotes.Notes.ToArray(),
                                Tags                    = new [] {"Changelog", "Script", "Build", "Release"},
                                RequireLicenseAcceptance= false,        
                                Symbols                 = false,
                                NoPackageAnalysis       = true,
                                Files                   = new [] { new NuSpecContent { Source = "ConventionalChangelog.dll" }, new NuSpecContent { Source = System.IO.Abstractions.dll } },
        BasePath    = binDir, 
        OutputDirectory   = nugetRoot
                            };


//-----------------------------------------------------------------------------
// Output some information about the current build.
//-----------------------------------------------------------------------------
var buildStartMessage = string.Format("Building version {0} of {1} ({2}).", version, assemblyInfo.Product, semVersion);
Information(buildStartMessage);
SlackChatPostMessage(
    token:slackToken,
    channel:slackChannel,
    text:buildStartMessage
);


//-----------------------------------------------------------------------------
// SETUP / TEARDOWN
//-----------------------------------------------------------------------------

Setup(() =>
{
    // Executed BEFORE the first task.
    Information("Running tasks...");
});

Teardown(() =>
{
    // Executed AFTER the last task.
    Information("Finished running tasks.");
});


//-----------------------------------------------------------------------------
// Tasks
//-----------------------------------------------------------------------------

Task("Clean")
    .Does(() =>
{
    // Clean solution directories.
    foreach(var solutionDir in solutionDirs)
    {
        Information("Cleaning {0}", solutionDir);
        CleanDirectories(solutionDir + "/**/bin/" + configuration);
        CleanDirectories(solutionDir + "/**/obj/" + configuration);
    }
});

Task("Restore")
    .Does(() =>
{
    // Restore all NuGet packages.
    foreach(var solution in solutions)
    {
        Information("Restoring {0}", solution);
        NuGetRestore(solution);
    }
});

Task("SolutionInfo")
    .IsDependentOn("Clean")
    .IsDependentOn("Restore")
    .Does(() =>
{
    var file = "./src/SolutionInfo.cs";
    CreateAssemblyInfo(file, assemblyInfo);
});

Task("Build")
    .IsDependentOn("Clean")
    .IsDependentOn("Restore")
    .IsDependentOn("SolutionInfo")
    .Does(() =>
{
    // Build all solutions.
    foreach(var solution in solutions)
    {
        Information("Building {0}", solution);
        MSBuild(solution, settings => 
            settings.SetPlatformTarget(PlatformTarget.MSIL)
                .WithProperty("TreatWarningsAsErrors","true")
                .WithTarget("Build")
                .SetConfiguration(configuration));
    }
});

Task("Create-NuGet-Package")
    .IsDependentOn("Build")
    .Does(() =>
{
    if (!Directory.Exists(nugetRoot))
    {
    CreateDirectory(nugetRoot);
    }
    NuGetPack("./nuspec/ConventionalChangelog.nuspec", nuGetPackSettings);
}); 

Task("Changelog")
	.Does(() =>
{
  	var changelog = new ConventionalChangelog.Changelog();

  	changelog.Generate(new ConventionalChangelog.ChangelogOptions() {
  		Version = version
  	});
});

Task("Default")
    .IsDependentOn("Create-NuGet-Package");

// Task("AppVeyor")
//    .IsDependentOn("Publish-MyGet");

RunTarget(target);