using System.IO;
using System.Reflection;
using Autodesk.Revit.UI;

namespace ThreeDeeRoomTags.Classes
{
    internal class Global
    {
        internal static string PanelName => "design tech unraveled";
        internal static Assembly ExecutingAssembly = Assembly.GetExecutingAssembly();
        internal static string ExecutingPath = Path.GetDirectoryName(ExecutingAssembly.Location);
        // The user-scoped TMP variable is missing on machines where the administrator sets only
        // the machine-wide one, and reading it back as null turned every Path.Combine below into
        // an ArgumentNullException. GetTempPath consults the whole chain and always answers.
        internal static string TempPath = Environment.GetEnvironmentVariable("TMP", EnvironmentVariableTarget.User)
                                          ?? Path.GetTempPath();
        internal static string LogFile = Path.Combine(ExecutingPath, "3dRoomTagsLog.txt");
        internal static string RevitVersion { get; set; }
        internal static string Version = ExecutingAssembly.GetName().Version.ToString();
        public static string[] EmbeddedLibraries =
            ExecutingAssembly.GetManifestResourceNames().Where(x => x.EndsWith(".dll")).ToArray();

        internal static PushButton ThreeDeeRoomTagPushButton { get; set; }

        internal static int ProductId => 160955;
    }
}
