#r "./bin/Debug/ConventionalChangelog.dll"

var target = Argument<string>("target", "Default");
var config = Argument<string>("config", "Release");

Task("Default")
	.Does(() =>
{
	var changelog = new ConventionalChangelog.Changelog();

	changelog.Generate(new ConventionalChangelog.ChangelogOptions() {
		Version = "1.0.0",
		WorkingDirectory = @".."
	});
})
.OnError(ex =>
{
    Error("There was an error: {0}", new object[] { ex.Message });
});

RunTarget(target);