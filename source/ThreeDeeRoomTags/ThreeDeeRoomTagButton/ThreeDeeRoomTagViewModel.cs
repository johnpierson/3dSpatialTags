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
        public RelayCommand<Window> Run { get; set; }

        private bool _flyOutVisibility;
        public bool FlyOutVisibility
        {
            get => _flyOutVisibility;
            set { _flyOutVisibility = value; OnPropertyChanged(nameof(FlyOutVisibility)); OnPropertyChanged(nameof(HasStatus)); }
        }
        private bool _unboundedFlyOutVisibility;
        public bool UnboundedFlyOutVisibility
        {
            get => _unboundedFlyOutVisibility;
            set { _unboundedFlyOutVisibility = value; OnPropertyChanged(nameof(UnboundedFlyOutVisibility)); OnPropertyChanged(nameof(HasStatus)); }
        }
        private ObservableCollection<SpatialElement> _spatialElements;
        public ObservableCollection<SpatialElement> SpatialElements
        {
            get => _spatialElements;
            set { _spatialElements = value; OnPropertyChanged(nameof(SpatialElements)); OnPropertyChanged(nameof(CanRun)); }
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
            set
            {
                _roomTagFamilySymbols = value;
                OnPropertyChanged(nameof(RoomTagFamilySymbols));
                OnPropertyChanged(nameof(HasNoFamilySymbols));
                OnPropertyChanged(nameof(CanRun));
            }
        }
        private ObservableCollection<RevitLinkInstance> _links;
        public ObservableCollection<RevitLinkInstance> Links
        {
            get => _links;
            set
            {
                _links = value;
                OnPropertyChanged(nameof(Links));
                OnPropertyChanged(nameof(HasLinks));
                OnPropertyChanged(nameof(FromLinkCaption));
            }
        }
        private int _familySymbolIndex;
        public int FamilySymbolIndex
        {
            get => _familySymbolIndex;
            set { _familySymbolIndex = value; OnPropertyChanged(nameof(FamilySymbolIndex)); OnPropertyChanged(nameof(CanRun)); }
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
        private string _errorText;

        /// <summary>Whatever went wrong on the last attempt, in words the user can act on.</summary>
        public string ErrorText
        {
            get => _errorText;
            set
            {
                _errorText = value;
                OnPropertyChanged(nameof(ErrorText));
                OnPropertyChanged(nameof(HasError));
                OnPropertyChanged(nameof(HasStatus));
            }
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
            set { _inProgress = value; OnPropertyChanged(nameof(InProgress)); OnPropertyChanged(nameof(CanRun)); }
        }
        private bool _fromLink;
        public bool FromLink
        {
            get => _fromLink;
            set { _fromLink = value; OnPropertyChanged(nameof(FromLink)); OnPropertyChanged(nameof(CanRun)); }
        }
        private int _linkIndex;
        public int LinkIndex
        {
            get => _linkIndex;
            set { _linkIndex = value; OnPropertyChanged(nameof(LinkIndex)); OnPropertyChanged(nameof(CanRun)); }
        }
        private string _textHeightString;
        public string TextHeightString
        {
            get => _textHeightString;
            set
            {
                _textHeightString = value;
                OnPropertyChanged(nameof(TextHeightString));
                OnPropertyChanged(nameof(HasTextHeightError));
            }
        }
        private double _textHeight;
        public double TextHeight
        {
            get => _textHeight;
            set { _textHeight = value; OnPropertyChanged(nameof(TextHeight)); }
        }

        /// <summary>Whether this document has any link to read spatial elements from.</summary>
        public bool HasLinks => Links != null && Links.Any();

        public string FromLinkCaption => HasLinks
            ? "Use a linked model"
            : "No loaded links in this document";

        public bool HasNoFamilySymbols => RoomTagFamilySymbols == null || !RoomTagFamilySymbols.Any();

        public bool HasError => !string.IsNullOrWhiteSpace(ErrorText);

        /// <summary>Whether the status card has anything to show.</summary>
        public bool HasStatus => FlyOutVisibility || UnboundedFlyOutVisibility || HasError;

        /// <summary>
        /// Whether the text height box holds something this tool cannot read. Blank is not an
        /// error — it means "leave the family's own height alone".
        /// </summary>
        public bool HasTextHeightError =>
            !string.IsNullOrWhiteSpace(TextHeightString) && ParseTextHeight(TextHeightString) <= 0;

        /// <summary>
        /// Whether the current selection is complete enough to place tags.
        ///
        /// The button used to be gated on <c>Rooms.Count</c>, a property this view model has never
        /// had. A binding to a missing property fails silently and leaves IsEnabled at its default
        /// of true, so the command was in fact always live — including with nothing selected, which
        /// is how the index lookups below used to throw straight into Revit's error dialog.
        /// </summary>
        public bool CanRun =>
            !InProgress
            && SelectedFamilySymbol != null
            && SpatialElements != null && SpatialElements.Any()
            && (!FromLink || SelectedLink != null);

        private FamilySymbol SelectedFamilySymbol =>
            RoomTagFamilySymbols != null
            && FamilySymbolIndex >= 0
            && FamilySymbolIndex < RoomTagFamilySymbols.Count
                ? RoomTagFamilySymbols[FamilySymbolIndex]
                : null;

        private RevitLinkInstance SelectedLink =>
            Links != null && LinkIndex >= 0 && LinkIndex < Links.Count
                ? Links[LinkIndex]
                : null;

        public ThreeDeeRoomTagViewModel(ThreeDeeRoomTagModel model)
        {
            //set button commands
            Close = new RelayCommand<Window>(OnClose);
            Run = new RelayCommand<Window>(OnRun);

            Model = model;
            FlyOutText = string.Empty;
            UnboundedFlyOutText = string.Empty;
            ErrorText = string.Empty;
            TextHeightString = Properties.Settings.Default.TextHeight;

            TargetIndex = Properties.Settings.Default.TargetIndex;
            TitleText = TargetIndex == 0 ? "3d Room Tags" : "3d Space Tags";
            UpdateExisting = true;
            InProgress = false;
            FromLink = false;

            //pack lists
            Links = Model.GetRevitLinks();
            GetLinkIndex();

            // After GetLinkIndex, not before: a link picked in the model means the dialog opens on
            // that link, and it should open showing that link's phases rather than the host's.
            RefreshPhasesForCurrentSource();

            RoomTagFamilySymbols = Model.CollectRoomTagFamilySymbols();
            SpatialElements = new ObservableCollection<SpatialElement>();

            // The saved index belongs to whichever document was open last, and this one may have
            // fewer tag types — or none. Restoring it blind is an index out of range on the first
            // run in a new model. A document with no tag family says so beside the family field
            // rather than here, so the status card stays about what the last action did.
            FamilySymbolIndex = RoomTagFamilySymbols.Any()
                ? Math.Min(Math.Max(Properties.Settings.Default.FamilySymbolIndex, 0), RoomTagFamilySymbols.Count - 1)
                : -1;
        }

        public void GetLinkIndex()
        {
            if (Links.Any())
            {
                var linkInstance = Model.IsLinkSelected();

                if (linkInstance != null)
                {
                    // Matched on id rather than on name: two instances of the same linked file
                    // share a name, and the first one found is not necessarily the one selected.
                    LinkIndex = Links.ToList().FindIndex(l => linkInstance.Id.Equals(l.Id));

                    if (LinkIndex >= 0)
                    {
                        FromLink = true;
                        return;
                    }
                }
            }

            LinkIndex = -1;
        }

        public void RefreshRooms(Phase phase)
        {
            if (phase is null)
            {
                ClearCollectedElements();
                return;
            }

            var link = FromLink ? SelectedLink : null;

            if (FromLink && link is null)
            {
                ClearCollectedElements();
                return;
            }

            SpatialElements = link is null
                ? Model.CollectSpatialElements(phase, TargetIndex)
                : Model.CollectSpatialElements(phase, link, TargetIndex);

            string spatialElementType = TargetIndex == 0 ? "rooms" : "spaces";
            FlyOutText = $"{SpatialElements.Count} taggable {spatialElementType} found in the selected phase.";
            FlyOutVisibility = true;
            ErrorText = string.Empty;

            int unbounded = SpatialElements.Count(s => s.Area <= 0);

            // Cleared rather than left standing: this used to return early when a phase had no
            // unbounded elements, so a warning from a previous phase stayed on screen describing
            // a count that no longer existed.
            if (unbounded == 0)
            {
                UnboundedFlyOutText = string.Empty;
                UnboundedFlyOutVisibility = false;
                return;
            }

            UnboundedFlyOutText =
                $"Warning: {unbounded} {spatialElementType} are unbounded, redundant or unplaced. Those cannot be tagged, but tags will still be created for the placed ones.";
            UnboundedFlyOutVisibility = true;
        }

        public void RefreshPhases(RevitLinkInstance linkInstance)
        {
            Phases = linkInstance is null ? Model.CollectPhases() : Model.CollectPhases(linkInstance);

            ClearCollectedElements();
        }

        /// <summary>
        /// Reloads the phase list for whatever source is currently chosen. Called when the link
        /// checkbox is toggled: ticking it without a link chosen has nothing to collect from, and
        /// showing the host document's phases there invited a run against the wrong model.
        /// </summary>
        public void RefreshPhasesForCurrentSource()
        {
            if (!FromLink)
            {
                RefreshPhases(null);
                return;
            }

            var link = SelectedLink;

            if (link is null)
            {
                Phases = new ObservableCollection<Phase>();
                ClearCollectedElements();
                return;
            }

            RefreshPhases(link);
        }

        /// <summary>
        /// Drops the collected elements and the counts describing them. Anything that changes what
        /// would be tagged has to come through here, or the status card keeps describing a
        /// selection the user has already moved on from.
        /// </summary>
        public void ClearCollectedElements()
        {
            SpatialElements = new ObservableCollection<SpatialElement>();
            FlyOutText = string.Empty;
            FlyOutVisibility = false;
            UnboundedFlyOutText = string.Empty;
            UnboundedFlyOutVisibility = false;
        }

        private void OnRun(Window win)
        {
            var spatialElementTag = SelectedFamilySymbol;

            if (spatialElementTag is null || SpatialElements is null || !SpatialElements.Any())
            {
                return;
            }

            InProgress = true;

            try
            {
                var link = FromLink ? SelectedLink : null;

                ApplyTextHeight(spatialElementTag);

                var result = Model.CreateRoomTags(spatialElementTag, SpatialElements, UpdateExisting, link);

                if (result.MissingParameter != null)
                {
                    ErrorText = $"The selected tag family type cannot be used: it has no \"{result.MissingParameter}\" parameter. Nothing was placed.";
                    FlyOutVisibility = false;
                    return;
                }

                var summary = $"{result.Tags.Count} tags created or updated.";

                if (result.MigratedLegacyTags > 0)
                {
                    summary += $" {result.MigratedLegacyTags} of them were tags from an earlier version, "
                               + "now recorded against the link they came from.";
                }

                FlyOutText = summary;
                FlyOutVisibility = true;

                // Both are worth saying, and a run can produce both at once, so neither is
                // allowed to hide the other.
                var notes = new List<string>();

                if (result.SkippedNotEditable > 0)
                {
                    notes.Add($"{result.SkippedNotEditable} existing tags are owned by another user or out of date, so they were left alone.");
                }

                if (result.OrphanedTags > 0)
                {
                    notes.Add($"{result.OrphanedTags} tags are for elements that no longer exist. They still say what they said, "
                              + "so they are now wrong; delete them yourself if you no longer want them.");
                }

                ErrorText = string.Join(" ", notes);
            }
            catch (Exception ex)
            {
                // A Revit API failure here used to escape into Revit's own error dialog with a
                // stack trace. The user can act on a sentence; they cannot act on that.
                ErrorText = $"Tags could not be created: {ex.Message}";
                FlyOutVisibility = false;
            }
            finally
            {
                InProgress = false;
            }
        }

        /// <summary>
        /// Pushes the requested text height onto the tag family type, if one was asked for and the
        /// family can take it. A family without the parameter is not a failure worth stopping the
        /// run over — the tags are still correct, they are just the size the family already was.
        /// </summary>
        private void ApplyTextHeight(FamilySymbol famSymb)
        {
            TextHeight = ParseTextHeight(TextHeightString);

            if (TextHeight <= 0)
            {
                return;
            }

            Properties.Settings.Default.TextHeight = TextHeightString;
            Properties.Settings.Default.Save();

            var param = famSymb.LookupParameter(ThreeDeeRoomTagModel.TextHeightParameter);

            if (param is null || param.IsReadOnly || param.StorageType != StorageType.Double)
            {
                return;
            }

            double feet = TextHeight / 12;

            if (Math.Abs(param.AsDouble() - feet) < 1e-9)
            {
                return;
            }

            using (Transaction t = new Transaction(Model.Doc, "Setting Tag Height"))
            {
                t.Start();
                try
                {
                    param.Set(feet);
                    t.Commit();
                }
                catch (Exception)
                {
                    t.RollBack();
                }
            }
        }

        private static double ParseTextHeight(string value)
        {
            try
            {
                return Utilities.StringUtils.ParseStringFeetAndInches(value);
            }
            catch (Exception)
            {
                return 0;
            }
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
    }
}
