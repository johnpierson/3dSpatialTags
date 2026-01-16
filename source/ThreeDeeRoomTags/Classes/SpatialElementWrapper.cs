using System.ComponentModel;

namespace ThreeDeeRoomTags.Classes
{
    public class SpatialElementWrapper : INotifyPropertyChanged
    {
        public bool IsTaggable { get; set; }
        public XYZ LocationPoint { get; set; }
        public ElementId Id { get; set; }
        public string Name { get; set; }
        public string Number { get; set; }


        public SpatialElementWrapper(Autodesk.Revit.DB.Element roomSpaceOrTags)
        {
            if (roomSpaceOrTags is Autodesk.Revit.DB.SpatialElement spatialElement)
            {
                
            }

            if (roomSpaceOrTags is Autodesk.Revit.DB.Architecture.RoomTag roomTag)
            {

                var roomTagLocation = roomTag.TagHeadPosition;

                LocationPoint = roomTag.TagHeadPosition;
            }
        }



        public event PropertyChangedEventHandler PropertyChanged;
        private void RaisePropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
