using System.Text;
using Nuke.Common.Git;
using Nuke.Common.Tools.Git;
using Nuke.Common.Tools.GitHub;
using Octokit;

sealed partial class Build
{
    Target PublishGitHub => _ => _
        .DependsOn(CreateInstaller, CreateBundle)
        .Requires(() => GitHubToken)
        .Requires(() => GitRepository)
        .OnlyWhenStatic(() => IsServerBuild && GitRepository.IsOnMainOrMasterBranch())

        // Skipped rather than failed when this version has already shipped. Every push to
        // main runs this target, and most of them are docs or spec edits that do not bump
        // Version — asserting there would leave main permanently red and train everyone to
        // ignore it. Publishing happens on the push that bumps Version, and only then.
        .OnlyWhenDynamic(() => !ReleaseTagExists())
        .Executes(async () =>
        {
            GitHubTasks.GitHubClient = new GitHubClient(new ProductHeaderValue(Solution.Name))
            {
                Credentials = new Credentials(GitHubToken)
            };

            var gitHubName = GitRepository.GetGitHubName();
            var gitHubOwner = GitRepository.GetGitHubOwner();

            ValidateRelease();

            var artifacts = Directory.GetFiles(ArtifactsDirectory, "*");
            var changelog = CreateGithubChangelog();
            Assert.NotEmpty(artifacts, "No artifacts were found to create the Release");

            var newRelease = new NewRelease(Version)
            {
                Name = Version,
                Body = changelog,
                TargetCommitish = GitRepository.Commit
            };

            var release = await GitHubTasks.GitHubClient.Repository.Release.Create(gitHubOwner, gitHubName, newRelease);
            await UploadArtifactsAsync(release, artifacts);
        });

    /// <summary>
    /// Whether a tag for the current <see cref="Version"/> already exists.
    ///
    /// Asked with `git tag -l` rather than `git describe`: describe with --always answers
    /// with an abbreviated commit hash when the clone carries no tags, and comparing that
    /// hash to a version string never matches — which is how the duplicate-release guard
    /// silently did nothing on a shallow CI checkout.
    /// </summary>
    bool ReleaseTagExists()
    {
        var tags = GitTasks.Git($"tag -l {Version}", logInvocation: false, logOutput: false);
        var exists = tags.Any(tag => tag.Text.Trim() == Version);

        if (exists) Log.Information("Release {Version} already exists; skipping publish", Version);

        return exists;
    }

    void ValidateRelease()
    {
        Assert.False(ReleaseTagExists(), $"A Release with the specified tag already exists in the repository: {Version}");
        Log.Information("Version: {Version}", Version);
    }

    /// <summary>
    /// The most recent tag reachable from HEAD, or null when the clone carries no tags at
    /// all. Distinguished explicitly rather than relying on `--always` falling back to a
    /// commit hash, which reads as a tag to every caller downstream.
    /// </summary>
    static string LatestTagOrNull()
    {
        var tags = GitTasks.Git("tag -l", logInvocation: false, logOutput: false);
        if (!tags.Any(tag => !string.IsNullOrWhiteSpace(tag.Text))) return null;

        var described = GitTasks.Git("describe --tags --abbrev=0", logInvocation: false, logOutput: false);
        return described.FirstOrDefault().Text?.Trim();
    }

    static async Task UploadArtifactsAsync(Release release, IEnumerable<string> artifacts)
    {
        foreach (var file in artifacts)
        {
            var releaseAssetUpload = new ReleaseAssetUpload
            {
                ContentType = "application/x-binary",
                FileName = Path.GetFileName(file),
                RawData = File.OpenRead(file)
            };

            await GitHubTasks.GitHubClient.Repository.Release.UploadAsset(release, releaseAssetUpload);
            Log.Information("Artifact: {Path}", file);
        }
    }

    string CreateGithubChangelog()
    {
        if (!File.Exists(ChangeLogPath))
        {
            Log.Warning("Unable to locate the changelog file: {Log}", ChangeLogPath);
            return string.Empty;
        }

        Log.Information("Changelog: {Path}", ChangeLogPath);

        var changelog = BuildChangelog();
        if (changelog.Length == 0)
        {
            Log.Warning("No version entry exists in the changelog: {Version}", Version);
            return string.Empty;
        }

        WriteCompareUrl(changelog);
        return changelog.ToString();
    }

    void WriteCompareUrl(StringBuilder changelog)
    {
        var latestTag = LatestTagOrNull();

        // Nothing to compare against on the first release, or on a clone without tags.
        if (latestTag is null || latestTag == Version) return;

        if (changelog[^1] != '\r' && changelog[^1] != '\n') changelog.AppendLine(Environment.NewLine);
        changelog.Append("Full changelog: ");
        changelog.Append(GitRepository.GetGitHubCompareTagsUrl(Version, latestTag));
    }

    StringBuilder BuildChangelog()
    {
        const string separator = "# ";

        var hasEntry = false;
        var changelog = new StringBuilder();
        foreach (var line in File.ReadLines(ChangeLogPath))
        {
            if (hasEntry)
            {
                if (line.StartsWith(separator)) break;

                changelog.AppendLine(line);
                continue;
            }

            if (line.StartsWith(separator) && line.Contains(Version))
            {
                hasEntry = true;
            }
        }

        TrimEmptyLines(changelog);
        return changelog;
    }

    static void TrimEmptyLines(StringBuilder builder)
    {
        if (builder.Length == 0) return;

        while (builder[^1] == '\r' || builder[^1] == '\n')
        {
            builder.Remove(builder.Length - 1, 1);
        }

        while (builder[0] == '\r' || builder[0] == '\n')
        {
            builder.Remove(0, 1);
        }
    }
}