sealed partial class Build
{
    const string Version = "2.0.0";
    readonly AbsolutePath ArtifactsDirectory = RootDirectory / "output";
    readonly AbsolutePath ChangeLogPath = RootDirectory / "Changelog.md";

    protected override void OnBuildInitialized()
    {
        // Anchored so the wildcards match only what ships. "Release*" also matched the
        // solution's plain "Release" configuration, which rebuilds Release R26 a second time,
        // and "Installer*" is spelled out because there is exactly one such configuration —
        // it is what builds the Installer project, which carries no Build entry under the
        // per-version configurations.
        Configurations =
        [
            "Release R*",
            "Installer"
        ];

        Bundles =
        [
            Solution.ThreeDeeRoomTags
        ];

        InstallersMap = new()
        {
            {Solution.Installer, Solution.ThreeDeeRoomTags}
        };
    }
}