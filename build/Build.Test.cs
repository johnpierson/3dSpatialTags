using Nuke.Common.Tools.DotNet;
using static Nuke.Common.Tools.DotNet.DotNetTasks;

sealed partial class Build
{
    /// <summary>
    /// Runs the framework-neutral test suite.
    ///
    /// Deliberately not part of the per-Revit-version configuration matrix: the suite covers
    /// logic that does not touch the Revit API, on both framework families the add-in ships
    /// on (net48 and net8.0). Anything that needs a running Revit belongs in the manual
    /// verification checklist, not here.
    /// </summary>
    Target Test => _ => _
        .DependsOn(Compile)
        .Executes(() =>
        {
            var testProject = RootDirectory / "tests" / "ThreeDeeRoomTags.Workflow.Tests" / "ThreeDeeRoomTags.Workflow.Tests.csproj";

            DotNetTest(settings => settings
                .SetProjectFile(testProject)
                .SetConfiguration("Release")
                .SetVerbosity(DotNetVerbosity.minimal));
        });
}
