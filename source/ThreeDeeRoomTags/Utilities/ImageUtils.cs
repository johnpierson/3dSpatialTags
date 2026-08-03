using System.Reflection;
using System.Windows.Media.Imaging;
using Serilog;

namespace ThreeDeeRoomTags.Utilities
{
    internal static class ImageUtils
    {
        /// <summary>
        /// An embedded image, or null if it could not be read.
        ///
        /// Null rather than the half-built BitmapImage this used to hand back: a bitmap whose
        /// initialisation threw is not a picture, and giving one to a ribbon button draws a
        /// blank where the icon should be. Revit's own default is better than that, and the
        /// reason now reaches the log instead of nowhere.
        /// </summary>
        public static BitmapImage LoadImage(Assembly a, string name)
        {
            try
            {
                var resourceName = a.GetManifestResourceNames().FirstOrDefault(x => x.Contains(name));

                if (resourceName is null)
                {
                    Log.Warning("No embedded image resource matches {Name}", name);
                    return null;
                }

                var img = new BitmapImage();

                using (var stream = a.GetManifestResourceStream(resourceName))
                {
                    if (stream is null)
                    {
                        Log.Warning("Embedded image resource {Resource} could not be opened", resourceName);
                        return null;
                    }

                    img.BeginInit();
                    img.StreamSource = stream;

                    // The stream is closed at the end of this block, so the bitmap has to have
                    // finished with it by then rather than reading lazily on first draw.
                    img.CacheOption = BitmapCacheOption.OnLoad;
                    img.EndInit();
                }

                return img;
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Could not load the embedded image {Name}", name);
                return null;
            }
        }
    }
}