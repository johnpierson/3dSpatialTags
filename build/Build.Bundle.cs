using Autodesk.PackageBuilder;
using System.Xml.Linq;
using Nuke.Common.Git;
using Nuke.Common.ProjectModel;
using Nuke.Common.Utilities;

sealed partial class Build
{
    Target CreateBundle => _ => _
        .DependsOn(Compile)
        .OnlyWhenStatic(() => IsLocalBuild || GitRepository.IsOnMainOrMasterBranch())
        .Executes(() =>
        {
            foreach (var project in Bundles)
            {
                Log.Information("Project: {Name}", project.Name);

                var directories = Directory.GetDirectories(project.Directory, "* Release *", SearchOption.AllDirectories);
                Assert.NotEmpty(directories, "No files were found to create a bundle");

                var bundleRoot = ArtifactsDirectory / project.Name;
                var bundlePath = bundleRoot / $"{project.Name}.bundle";
                var manifestPath = bundlePath / "PackageContents.xml";
                var contentsDirectory = bundlePath / "Contents";
                foreach (var path in directories)
                {
                    var version = MatchYear(path);

                    Log.Information("Bundle files for version {Version}:", version);
                    CopyAssemblies(path, contentsDirectory / version);
                }

                GenerateManifest(project, directories, manifestPath);
                AssertManifestModulesExist(manifestPath, contentsDirectory, directories);
                CompressFolder(bundleRoot);
            }
        });

    /// <summary>
    /// The Revit year for a publish directory, read from the directory's own name.
    ///
    /// Matched against the leaf rather than the whole path: the pattern is four bare digits
    /// and Regex.Match returns the leftmost hit, so a clone under a path like C:\ci\2023
    /// used to win over the "Revit 2027" segment and stamp every component with the wrong
    /// year. The CI runner's path happens to contain no digits, which is the only reason
    /// this never showed up in a release build.
    /// </summary>
    string MatchYear(string path) => YearRegex.Match(Path.GetFileName(path.TrimEnd(Path.DirectorySeparatorChar))).Value;

    void GenerateManifest(Project project, string[] directories, AbsolutePath manifestDirectory)
    {
        BuilderUtils.Build<PackageContentsBuilder>(builder =>
        {
            var versions = directories.Select(MatchYear).Select(int.Parse);
            var company = GetConfigurationValue(project, config => config.Name == "VendorId");
            var email = GetConfigurationValue(project, config => config.Name == "VendorEmail");
            var moduleName = GetAddInFileName(project);

            builder.ApplicationPackage.Create()
                .ProductType(ProductTypes.Application)
                .AutodeskProduct(AutodeskProducts.Revit)
                .Name(Solution.Name)
                .AppVersion(Version);

            builder.CompanyDetails.Create(company)
                .Email(email);

            foreach (var version in versions)
            {
                builder.Components.CreateEntry($"Revit {version}")
                    .RevitPlatform(version)
                    .AppName(project.Name)
                    .ModuleName($"./Contents/{version}/{moduleName}");
            }
        }, manifestDirectory);
    }

    /// <summary>
    /// The name of the add-in manifest as it actually exists on disk.
    ///
    /// This used to be assumed to be "{project.Name}.addin". It is not: the manifest is
    /// called Rise.ThreeDeeRoomTags.addin, from a retired brand, while the project is
    /// ThreeDeeRoomTags. Every published bundle therefore pointed at a file that was not in
    /// the zip, and Autodesk's loader silently loaded nothing. The MSIs were unaffected
    /// because they drop the manifest into Addins\&lt;year&gt;\, where Revit reads any *.addin
    /// regardless of name — which is why this went unnoticed.
    /// </summary>
    static string GetAddInFileName(Project project)
    {
        var manifest = project.Directory.GetFiles("*.addin").FirstOrDefault();

        return manifest.NotNull($"No .addin manifest was found for the project: {project.Name}").Name;
    }

    /// <summary>
    /// Fails the build if the manifest names a module that is not in the staged bundle.
    /// The defect this guards against shipped silently through three releases; a broken
    /// bundle should not be discoverable only by a user whose add-in never appears.
    /// </summary>
    void AssertManifestModulesExist(AbsolutePath manifestPath, AbsolutePath contentsDirectory, string[] directories)
    {
        var moduleName = GetAddInFileName(Bundles.First());

        foreach (var version in directories.Select(MatchYear))
        {
            var modulePath = contentsDirectory / version / moduleName;

            Assert.FileExists(modulePath, $"The bundle manifest names a module that is not in the bundle: {modulePath}");
        }

        Assert.FileExists(manifestPath, "No PackageContents.xml was generated for the bundle");
    }

    string GetConfigurationValue(Project project, Func<XElement, bool> filter)
    {
        var defaultValue = string.Empty;
        var configPath = project.Directory.GetFiles("*.addin").FirstOrDefault();

        if (configPath is null) return defaultValue;

        var configDocument = configPath.ReadXml();
        if (configDocument.Root is null) return defaultValue;

        var sectionElement = configDocument.Root.Elements().FirstOrDefault();
        if (sectionElement is null) return defaultValue;

        var configElement = sectionElement.Elements().FirstOrDefault(filter);
        if (configElement is null) return defaultValue;

        return configElement.Value;
    }

    static void CompressFolder(AbsolutePath bundleRoot)
    {
        var bundleName = bundleRoot.WithExtension(".zip");
        bundleRoot.CompressTo(bundleName);
        bundleRoot.DeleteDirectory();

        Log.Information("Compressing into a Zip: {Name}", bundleName);
    }

    static void CopyAssemblies(string sourcePath, string targetPath)
    {
        foreach (var dirPath in Directory.GetDirectories(sourcePath, "*", SearchOption.AllDirectories))
            Directory.CreateDirectory(dirPath.Replace(sourcePath, targetPath));

        foreach (var filePath in Directory.GetFiles(sourcePath, "*", SearchOption.AllDirectories))
        {
            Log.Information("{Assembly}", filePath);
            File.Copy(filePath, filePath.Replace(sourcePath, targetPath), true);
        }
    }
}