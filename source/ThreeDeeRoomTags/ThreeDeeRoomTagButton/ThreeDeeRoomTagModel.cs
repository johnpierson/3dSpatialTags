using System.Collections.ObjectModel;
using System.IO;
using Autodesk.Revit.DB.Architecture;
using Autodesk.Revit.DB.Mechanical;
using Autodesk.Revit.DB.Structure;
using Autodesk.Revit.UI;
using ThreeDeeRoomTags.Classes;
using ThreeDeeRoomTags.Utilities;

namespace ThreeDeeRoomTags.ThreeDeeRoomTagButton
{
    public class ThreeDeeRoomTagModel
    {
        public UIApplication UiApp { get; }
        public Document Doc { get; }
        public UIDocument UiDoc { get; }
        internal ThreeDeeRoomTagModel(UIApplication uiApp)
        {
            UiApp = uiApp;
            UiDoc = uiApp.ActiveUIDocument;
            Doc = uiApp.ActiveUIDocument.Document;
        }

        public ObservableCollection<Phase> CollectPhases()
        {
            List<Phase>  phases = new FilteredElementCollector(Doc).OfClass(typeof(Phase)).WhereElementIsNotElementType().Cast<Phase>().ToList();
            
            return new ObservableCollection<Phase>(phases);
        }
        public ObservableCollection<Phase> CollectPhases(RevitLinkInstance linkInstance)
        {
            var doc = linkInstance.GetLinkDocument();
            List<Phase> phases = new FilteredElementCollector(doc).OfClass(typeof(Phase)).WhereElementIsNotElementType().Cast<Phase>().ToList();

            return new ObservableCollection<Phase>(phases);
        }
        public ObservableCollection<SpatialElement> CollectSpatialElements(Phase phase, int targetIndex)
        {
            if (targetIndex == 0)
            {
                List<Room>
                    rooms = new FilteredElementCollector(Doc).OfCategory(BuiltInCategory.OST_Rooms).Cast<Room>().Where(r => r.get_Parameter(BuiltInParameter.ROOM_PHASE).AsElementId().Equals(phase.Id)).ToList();
                return new ObservableCollection<SpatialElement>(rooms);
            }
            else
            {
                List<Space>
                    spaces = new FilteredElementCollector(Doc).OfCategory(BuiltInCategory.OST_MEPSpaces).Cast<Space>().Where(r => r.get_Parameter(BuiltInParameter.ROOM_PHASE).AsElementId().Equals(phase.Id)).ToList();
                return new ObservableCollection<SpatialElement>(spaces);
            }
        }
        public ObservableCollection<SpatialElement> CollectSpatialElements(Phase phase, RevitLinkInstance linkInstance, int targetIndex)
        {
            var doc = linkInstance.GetLinkDocument();
           
            if (targetIndex == 0)
            {
                List<Room>
                    rooms = new FilteredElementCollector(doc).OfCategory(BuiltInCategory.OST_Rooms).Cast<Room>().Where(r => r.get_Parameter(BuiltInParameter.ROOM_PHASE).AsElementId().Equals(phase.Id)).ToList();
                return new ObservableCollection<SpatialElement>(rooms);
            }
            if(targetIndex == 1)
            {
                List<Space>
                    spaces = new FilteredElementCollector(doc).OfCategory(BuiltInCategory.OST_MEPSpaces).Cast<Space>().Where(r => r.get_Parameter(BuiltInParameter.ROOM_PHASE).AsElementId().Equals(phase.Id)).ToList();
                return new ObservableCollection<SpatialElement>(spaces);
            }

            return new ObservableCollection<SpatialElement>();
        }

        public ObservableCollection<FamilySymbol> CollectRoomTagFamilySymbols()
        {
            List<FamilySymbol> tags = new FilteredElementCollector(Doc).OfClass(typeof(FamilySymbol)).Cast<FamilySymbol>()
                .Where(f => f.Family.Name.Contains(Global.FamilyName)).ToList();

            if (!tags.Any())
            {
                if (LoadFamily())
                {
                    tags = new FilteredElementCollector(Doc).OfClass(typeof(FamilySymbol)).Cast<FamilySymbol>()
                        .Where(f => f.Family.Name.Contains(Global.FamilyName)).ToList();
                }
            }

            return new ObservableCollection<FamilySymbol>(tags.OrderBy(f => f.Name));
        }

        public bool LoadFamily()
        {
            bool result;

            string familyPath = string.Empty;
            string installPath = Path.Combine(Global.ExecutingPath, $"{Global.FamilyName}.rfa");
            if (File.Exists(installPath))
            {
                familyPath = installPath;
            }
            else
            {
                string tempPath = Path.Combine(Global.TempPath, $"{Global.FamilyName}.rfa");
                File.WriteAllBytes(tempPath, Properties.FamilySymbols._3dSpatialElementTag);

                if (File.Exists(tempPath))
                {
                    familyPath = tempPath;
                }
            }
            
            if (string.IsNullOrWhiteSpace(familyPath)) return false;

            using (Transaction t = new Transaction(Doc,"Loading tag"))
            {
                t.Start();
                result = Doc.LoadFamily(familyPath);
                t.Commit();
            }

            return result;
        }

        public List<FamilyInstance> CreateRoomTags(FamilySymbol spatialElementTag, ObservableCollection<SpatialElement> spatialElements, bool updateExisting = true, RevitLinkInstance linkInstance = null)
        {

            List<FamilyInstance> currentTags = new List<FamilyInstance>();
            List<FamilyInstance> existingTags = null;

            if (updateExisting)
            {
                existingTags = new FilteredElementCollector(Doc).OfClass(typeof(FamilyInstance))
                    .WhereElementIsNotElementType().Cast<FamilyInstance>()
                    .Where(f => f.Symbol.Family.Name.Contains(Global.FamilyName)).ToList();
            }


            using (Transaction t = new Transaction(Doc, "Placing 3d spatial element tags"))
            {
                t.Start();
                
                if (!spatialElementTag.IsActive)
                {
                    spatialElementTag.Activate();
                }

                foreach (var spatialElement in spatialElements)
                {
                    //skip unplaced rooms
                    //if(spatialElement.Area <= 0) continue;
                    
                    var spatialElementLocation = spatialElement.Location as LocationPoint;

                    if (spatialElementLocation is null) continue;

                    var spatialElementPoint = spatialElementLocation.Point;

                    if (linkInstance != null)
                    {
                        var transform = linkInstance.GetTransform();
                        spatialElementPoint = transform.OfPoint(spatialElementPoint);
                    }

                    //if the room name and number are blank, then skip.
                    var roomName = spatialElement.get_Parameter(BuiltInParameter.ROOM_NAME).AsString();
                    var roomNumber = spatialElement.get_Parameter(BuiltInParameter.ROOM_NUMBER).AsString();

                    if (string.IsNullOrWhiteSpace(roomName) || string.IsNullOrWhiteSpace(roomNumber))
                    {
                        continue;
                    }


                    FamilyInstance roomTagInstance = null;

                    if (updateExisting && existingTags.Any())
                    {
                        var foundTag = existingTags.FirstOrDefault(f => f.LookupParameter("SpatialElementId").AsString().Equals(spatialElement.UniqueId));
                        if (foundTag != null)
                        {
                            if (foundTag.IsElementEditable())
                            {
                                roomTagInstance = foundTag;
                                foundTag.Symbol = spatialElementTag;
                                var tagLocation = foundTag.Location as LocationPoint;
                                tagLocation.Point = spatialElementPoint;
                            }
                        }
                    }
                    if(roomTagInstance == null) 
                    {
                        roomTagInstance = Doc.Create.NewFamilyInstance(spatialElementPoint, spatialElementTag, StructuralType.NonStructural);
                    }

                    roomTagInstance.LookupParameter("Name").Set(roomName);
                    roomTagInstance.LookupParameter("Number").Set(roomNumber);
                    roomTagInstance.LookupParameter("SpatialElementId").Set(spatialElement.UniqueId);

                    currentTags.Add(roomTagInstance);
                }

                // Set failure handler to hide identical instances warnings which may be posted.
                FailureHandlingOptions failureOptions = t.GetFailureHandlingOptions();
                failureOptions.SetFailuresPreprocessor(new HideOverlappingElementWarning());
                t.Commit(failureOptions);
            }

            return currentTags;
        }
        public ObservableCollection<RevitLinkInstance> GetRevitLinks()
        {
            var links = new FilteredElementCollector(Doc).OfClass(typeof(RevitLinkInstance)).Cast<RevitLinkInstance>().OrderBy(l => l.Name).ToList();

            return new ObservableCollection<RevitLinkInstance>(links);
        }

        public RevitLinkInstance IsLinkSelected()
        {
            var id = UiDoc.Selection.GetElementIds().FirstOrDefault();
            if (id != null)
            {
                var element = Doc.GetElement(id);
                if (element is RevitLinkInstance linkInstance)
                {
                    return linkInstance;
                }
            }

            return null;
        }
    }
}
