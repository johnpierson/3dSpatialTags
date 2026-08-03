using System.IO;

namespace ThreeDeeRoomTags.Tagging
{
    /// <summary>
    /// Where the bundled tag family gets extracted to before it is loaded.
    ///
    /// This exists as its own thing, away from the Revit API, for one reason: Revit names a
    /// loaded family after the file it came from. That makes the filename a piece of
    /// user-visible behaviour rather than an implementation detail, and it is behaviour that
    /// nothing but running Revit will tell you about — a build where this is wrong compiles
    /// clean, passes every other test, and shows up as families called
    /// "3dSpatialElementTag.4622231a00da48d386fdcf38185da90b" in somebody's drop-down.
    /// </summary>
    internal static class TagFamilyFile
    {
        /// <summary>
        /// The family's name, which is also the file's name, because Revit takes one from the
        /// other. Changing this renames the family in every model that loads it.
        /// </summary>
        public const string FamilyName = "3dSpatialElementTag";

        public const string Extension = ".rfa";

        /// <summary>The folder extractions live under, so they can be told apart from anything else in temp.</summary>
        public const string FolderName = "3dSpatialTags";

        /// <summary>
        /// The path to extract to for one load.
        ///
        /// The uniqueness goes in a directory and never in the filename. A fixed path in temp is
        /// a file another process can be holding open when this one needs it, so each load gets
        /// its own folder — but the file inside keeps the family's own name, or Revit ends up
        /// calling the family whatever made the path unique.
        /// </summary>
        public static string BuildExtractionPath(string tempRoot, string uniqueToken) =>
            Path.Combine(BuildExtractionDirectory(tempRoot, uniqueToken), FamilyName + Extension);

        public static string BuildExtractionDirectory(string tempRoot, string uniqueToken) =>
            Path.Combine(tempRoot, FolderName, uniqueToken);
    }
}
