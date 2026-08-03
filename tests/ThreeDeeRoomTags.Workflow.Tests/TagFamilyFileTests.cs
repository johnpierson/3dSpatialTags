using System.IO;
using ThreeDeeRoomTags.Tagging;
using Xunit;

namespace ThreeDeeRoomTags.Workflow.Tests
{
    /// <summary>
    /// Where the bundled family is extracted to, which decides what the family ends up called.
    ///
    /// Revit names a loaded family after the file it came from, so these assertions are about
    /// user-visible behaviour rather than plumbing. They exist because getting it wrong compiles
    /// clean and passes everything else: a build that put the unique part of the path in the
    /// filename shipped families called "3dSpatialElementTag.4622231a00da48d386fdcf38185da90b",
    /// a new one per dialog-open, and only running Revit showed it.
    /// </summary>
    public class TagFamilyFileTests
    {
        private const string TempRoot = @"C:\temp";

        /// <summary>The whole point: the filename is the family name, whatever else varies.</summary>
        [Theory]
        [InlineData("4622231a00da48d386fdcf38185da90b")]
        [InlineData("00000000000000000000000000000000")]
        [InlineData("a")]
        public void TheFileIsAlwaysNamedAfterTheFamily(string token)
        {
            var path = TagFamilyFile.BuildExtractionPath(TempRoot, token);

            Assert.Equal("3dSpatialElementTag.rfa", Path.GetFileName(path));
            Assert.Equal("3dSpatialElementTag", Path.GetFileNameWithoutExtension(path));
        }

        /// <summary>
        /// A regression test named for what went wrong: the unique token belongs in the
        /// directory, and must never leak into the name Revit will read.
        /// </summary>
        [Fact]
        public void TheUniqueTokenNeverAppearsInTheFileName()
        {
            const string token = "4622231a00da48d386fdcf38185da90b";

            var path = TagFamilyFile.BuildExtractionPath(TempRoot, token);

            Assert.DoesNotContain(token, Path.GetFileName(path));
            Assert.Contains(token, Path.GetDirectoryName(path));
        }

        /// <summary>
        /// Two loads must not collide on disk, or one can be holding the file open while the
        /// other needs it — which is what the unique token is for in the first place.
        /// </summary>
        [Fact]
        public void TwoLoadsGetDifferentDirectoriesAndTheSameFileName()
        {
            var first = TagFamilyFile.BuildExtractionPath(TempRoot, "aaaa");
            var second = TagFamilyFile.BuildExtractionPath(TempRoot, "bbbb");

            Assert.NotEqual(Path.GetDirectoryName(first), Path.GetDirectoryName(second));
            Assert.Equal(Path.GetFileName(first), Path.GetFileName(second));
        }

        /// <summary>
        /// Extractions live under a folder of this tool's own, so a temp directory shared with
        /// everything else on the machine stays legible and the whole lot can be removed.
        /// </summary>
        [Fact]
        public void ExtractionsLiveUnderTheirOwnFolder()
        {
            var directory = TagFamilyFile.BuildExtractionDirectory(TempRoot, "aaaa");

            Assert.Equal("aaaa", Path.GetFileName(directory));
            Assert.Equal("3dSpatialTags", Path.GetFileName(Path.GetDirectoryName(directory)));
            Assert.StartsWith(TempRoot, directory);
        }
    }
}
