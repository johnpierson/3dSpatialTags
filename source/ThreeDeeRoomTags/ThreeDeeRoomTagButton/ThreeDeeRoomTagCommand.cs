using System.Reflection;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.UI;
using Serilog;
using ThreeDeeRoomTags.Classes;
using ThreeDeeRoomTags.Utilities;

namespace ThreeDeeRoomTags.ThreeDeeRoomTagButton
{
    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    [Journaling(JournalingMode.NoCommandData)]
    internal class ThreeDeeRoomTagCommand : IExternalCommand
    {
        private const string DialogTitle = "3d Spatial Tags";

        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            var uiApp = commandData.Application;
            var uiDoc = uiApp.ActiveUIDocument;

            // Guarded before anything is constructed. Opening the dialog is not a passive
            // act: the view model's constructor collects family symbols, and when none are
            // loaded that path opens a transaction and calls Doc.LoadFamily. In a family
            // document that either throws or quietly nests the tag family into the user's
            // own family; on a read-only document the transaction throws outright. Neither
            // was reachable through the dialog's own error reporting, which only covers a
            // run, so both escaped into Revit's raw unhandled-exception dialog.
            var unsupported = DescribeUnsupportedDocument(uiDoc);

            if (unsupported != null)
            {
                TaskDialog.Show(DialogTitle, unsupported);
                return Result.Cancelled;
            }

            try
            {
                var m = new ThreeDeeRoomTagModel(uiApp);
                var vm = new ThreeDeeRoomTagViewModel(m);
                var v = new ThreeDeeRoomTagView
                {
                    DataContext = vm
                };

                // Owned by the Revit window so the dialog is properly modal to it, opens on
                // the same screen, and cannot be lost behind it on a multi-monitor setup.
                new System.Windows.Interop.WindowInteropHelper(v).Owner = uiApp.MainWindowHandle;

                v.ShowDialog();

                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                // Everything from here reaches the user as a sentence rather than as a stack
                // trace in Revit's own error dialog. A corrupt user.config, a document that
                // turns read-only between the guard and the transaction, a family that will
                // not load — none of them are worth a crash report. The detail goes to the log.
                Log.Error(ex, "The 3d Spatial Tags dialog could not be opened");

                message = $"3d Spatial Tags could not open: {ex.Message}";

                return Result.Failed;
            }
        }

        /// <summary>
        /// Why this document cannot be tagged, or null if it can be.
        /// </summary>
        private static string DescribeUnsupportedDocument(UIDocument uiDoc)
        {
            if (uiDoc?.Document is null)
            {
                return "Open a Revit project before creating 3d spatial tags.";
            }

            if (uiDoc.Document.IsFamilyDocument)
            {
                return "3d Spatial Tags works on projects, not families. Open the project that "
                       + "contains the rooms or spaces you want to tag.";
            }

            if (uiDoc.Document.IsReadOnly)
            {
                return "This document is read-only, so no tags can be placed in it.";
            }

            return null;
        }

        public static void CreateButton(RibbonPanel panel)
        {
            var assembly = Assembly.GetExecutingAssembly();

            var pushButtonData = new PushButtonData(
                MethodBase.GetCurrentMethod().DeclaringType?.Name,
                "3d Spatial" + Environment.NewLine + "Tags",
                assembly.Location,
                MethodBase.GetCurrentMethod().DeclaringType?.FullName)
            {
                ToolTip = "Create / Update 3d Spatial Tags",
                LargeImage = ImageUtils.LoadImage(assembly, "rise.3dRoomTags_32.png")
            };

            Global.ThreeDeeRoomTagPushButton = panel.AddItem(pushButtonData) as PushButton;
        }
    }
}
