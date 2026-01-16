using System.Collections.ObjectModel;
using System.Windows;
using ThreeDeeRoomTags.Classes;

namespace ThreeDeeRoomTags.ThreeDeeRoomTagButton
{
    public class ThreeDeeRoomTagViewModel : ObservableRecipient
    {
        public string PluginVersion => $"3d Spatial Tags v.{Global.Version}";
        private ThreeDeeRoomTagModel Model { get; set; }
        public RelayCommand<Window> Close { get; set; }
        public RelayCommand<Window> Help { get; set; }
        public RelayCommand<Window> Run { get; set; }

        private bool _flyOutVisibility;
        public bool FlyOutVisibility
        {
            get => _flyOutVisibility;
            set { _flyOutVisibility = value; OnPropertyChanged(nameof(FlyOutVisibility)); }
        }
        private bool _unboundedFlyOutVisibility;
        public bool UnboundedFlyOutVisibility
        {
            get => _unboundedFlyOutVisibility;
            set { _unboundedFlyOutVisibility = value; OnPropertyChanged(nameof(UnboundedFlyOutVisibility)); }
        }
        private ObservableCollection<SpatialElement> _spatialElements;
        public ObservableCollection<SpatialElement> SpatialElements
        {
            get => _spatialElements;
            set { _spatialElements = value; OnPropertyChanged(nameof(SpatialElements)); }
        }
        private ObservableCollection<Phase> _phases;
        public ObservableCollection<Phase> Phases
        {
            get => _phases;
            set { _phases = value; OnPropertyChanged(nameof(Phases)); }
        }
        private ObservableCollection<FamilySymbol> _roomTagFamilySymbols;
        public ObservableCollection<FamilySymbol> RoomTagFamilySymbols
        {
            get => _roomTagFamilySymbols;
            set { _roomTagFamilySymbols = value; OnPropertyChanged(nameof(RoomTagFamilySymbols)); }
        }
        private ObservableCollection<RevitLinkInstance> _links;
        public ObservableCollection<RevitLinkInstance> Links
        {
            get => _links;
            set { _links = value; OnPropertyChanged(nameof(Links)); }
        }
        private int _familySymbolIndex;
        public int FamilySymbolIndex
        {
            get => _familySymbolIndex;
            set { _familySymbolIndex = value; OnPropertyChanged(nameof(FamilySymbolIndex)); }
        }

        private int _targetIndex;
        public int TargetIndex
        {
            get => _targetIndex;
            set { _targetIndex = value; OnPropertyChanged(nameof(TargetIndex)); }
        }
        private bool _updateExisting;
        public bool UpdateExisting
        {
            get => _updateExisting;
            set { _updateExisting = value; OnPropertyChanged(nameof(UpdateExisting)); }
        }
        private string _flyOutText;
        public string FlyOutText
        {
            get => _flyOutText;
            set { _flyOutText = value; OnPropertyChanged(nameof(FlyOutText)); }
        }
        private string _unboundedFlyOutText;
        public string UnboundedFlyOutText
        {
            get => _unboundedFlyOutText;
            set { _unboundedFlyOutText = value; OnPropertyChanged(nameof(UnboundedFlyOutText)); }
        }
        private string _titleText;
        public string TitleText
        {
            get => _titleText;
            set { _titleText = value; OnPropertyChanged(nameof(TitleText)); }
        }
        private bool _inProgress;
        public bool InProgress
        {
            get => _inProgress;
            set { _inProgress = value; OnPropertyChanged(nameof(InProgress)); }
        }
        private bool _fromLink;
        public bool FromLink
        {
            get => _fromLink;
            set { _fromLink = value; OnPropertyChanged(nameof(FromLink)); }
        }
        private int _linkIndex;
        public int LinkIndex
        {
            get => _linkIndex;
            set { _linkIndex = value; OnPropertyChanged(nameof(LinkIndex)); }
        }
        private string _textHeightString;
        public string TextHeightString
        {
            get => _textHeightString;
            set { _textHeightString = value; OnPropertyChanged(nameof(TextHeightString)); }
        }
        private double _textHeight;
        public double TextHeight
        {
            get => _textHeight;
            set { _textHeight = value; OnPropertyChanged(nameof(TextHeight)); }
        }
        public ThreeDeeRoomTagViewModel(ThreeDeeRoomTagModel model)
        {
            //set button commands
            Close = new RelayCommand<Window>(OnClose);
            Help = new RelayCommand<Window>(OnHelp);
            Run = new RelayCommand<Window>(OnRun);

            Model = model;
            FlyOutText = string.Empty;
            UnboundedFlyOutText = string.Empty;
            TextHeightString = Properties.Settings.Default.TextHeight;
          
            FamilySymbolIndex = Properties.Settings.Default.FamilySymbolIndex;
            TargetIndex = Properties.Settings.Default.TargetIndex;
            TitleText = TargetIndex == 0 ? "3d Room Tags" : "3d Space Tags";
            UpdateExisting = true;
            InProgress = false;
            FromLink = false;
            
            //pack lists
            Links = Model.GetRevitLinks();
            GetLinkIndex();

            Phases = Model.CollectPhases();
            RoomTagFamilySymbols = Model.CollectRoomTagFamilySymbols();
            SpatialElements = new ObservableCollection<SpatialElement>();
        }

        public void GetLinkIndex()
        {
            if (Links.Any())
            {
                var linkInstance = Model.IsLinkSelected();

                if (linkInstance != null)
                {
                   
                    LinkIndex = Links.ToList().FindIndex(l => l.Name.Equals(linkInstance.Name));
                    FromLink = true;
                    return;
                }
            }

            LinkIndex = -1;
        }
        public void RefreshRooms(Phase phase)
        {
            SpatialElements = FromLink ? Model.CollectSpatialElements(phase, Links[LinkIndex], TargetIndex) : Model.CollectSpatialElements(phase, TargetIndex);

            string spatialElementType = TargetIndex == 0 ? "rooms" : "spaces";
            FlyOutText = $"|  {SpatialElements.Count} taggable {spatialElementType} found in selected phase.";
            FlyOutVisibility = true;

            if (!SpatialElements.Any(s => s.Area <= 0)) return;
            {
                UnboundedFlyOutText =
                    $"|  Warning, there are {SpatialElements.Count(s => s.Area <= 0)} {spatialElementType} that are unbounded/redundant/unplaced. This can cause issues creating tags, but we will try to create tags for the rooms that are placed.";
                UnboundedFlyOutVisibility = true;
            }
        }

        public void RefreshPhases(RevitLinkInstance linkInstance)
        {
            Phases = linkInstance is null ? Model.CollectPhases() : Model.CollectPhases(linkInstance);

            SpatialElements = new ObservableCollection<SpatialElement>();
        }
        private void OnRun(Window win)
        {
            InProgress = true;
            List<FamilyInstance> spatialElementTags;
            spatialElementTags = FromLink ? Model.CreateRoomTags(RoomTagFamilySymbols[FamilySymbolIndex], SpatialElements, UpdateExisting, Links[LinkIndex]) : Model.CreateRoomTags(RoomTagFamilySymbols[FamilySymbolIndex], SpatialElements, UpdateExisting);

            //try to update text height
            try
            {
                TextHeight = Utilities.StringUtils.ParseStringFeetAndInches(TextHeightString);
            }
            catch (Exception)
            {
                TextHeight = 0;
            }
          
            if (TextHeight != 0)
            {
                Properties.Settings.Default.TextHeight = TextHeightString;
                Properties.Settings.Default.Save();

                FamilySymbol famSymb = RoomTagFamilySymbols[FamilySymbolIndex];
                var param = famSymb.LookupParameter("Text Height");

                if (param.AsDouble() != TextHeight/12)
                {
                    using (Transaction t = new Transaction(Model.Doc, "Setting Tag Height"))
                    {
                        t.Start();
                        try
                        {
                            param.Set(TextHeight / 12);
                            t.Commit();
                        }
                        catch (Exception)
                        {
                            t.RollBack();
                        }
                    }
                }
            }

            FlyOutText = $"|  {spatialElementTags.Count} tags created/updated.";
            FlyOutVisibility = true;

            InProgress = false;
        }
        private void OnClose(Window win)
        {
            try
            {
                win.Close();
            }
            catch (Exception)
            {
                // ignored
            }
        }

        private void OnHelp(Window win)
        {
            string url = "https://www.parallaxteam.com/MADE-RevitPlugins";
            System.Diagnostics.Process.Start(url);
        }
    }
}
