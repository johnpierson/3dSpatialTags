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

        // GetTempPath, not the user-scoped TMP variable. The old code read TMP out of the user
        // registry first and only fell back here, which is the opposite of what its comment
        // claimed and meant a stale profile value pointing somewhere unwritable broke the
        // bundled-family fallback with no explanation. GetTempPath consults the whole chain --
        // TMP, TEMP, USERPROFILE, the Windows directory -- and always answers.
        internal static string TempPath = Path.GetTempPath();

        // Under the user's own local app data, not beside the assembly. A MultiUser install
        // puts the assembly in ProgramData, where writing a log is a permissions question and
        // every user on the machine would share one file.
        internal static string LogDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "design tech unraveled",
            "3d Spatial Tags",
            "logs");

        internal static string LogFile = Path.Combine(LogDirectory, "log-.txt");

        internal static string RevitVersion { get; set; }
        internal static string Version = ExecutingAssembly.GetName().Version.ToString();

        internal static PushButton ThreeDeeRoomTagPushButton { get; set; }
    }
}
