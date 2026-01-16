using System.Reflection;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.UI;
using ThreeDeeRoomTags.Classes;
using ThreeDeeRoomTags.Utilities;

namespace ThreeDeeRoomTags.ThreeDeeRoomTagButton
{
    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    [Journaling(JournalingMode.NoCommandData)]
    internal class ThreeDeeRoomTagCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            var uiApp = commandData.Application;
            var uiDoc = commandData.Application.ActiveUIDocument;
            var doc = uiDoc.Document;

            var m = new ThreeDeeRoomTagModel(uiApp);
            var vm = new ThreeDeeRoomTagViewModel(m);
            var v = new ThreeDeeRoomTagView
            {
               DataContext = vm 
            };

            v.ShowDialog();

            return Result.Succeeded;
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
