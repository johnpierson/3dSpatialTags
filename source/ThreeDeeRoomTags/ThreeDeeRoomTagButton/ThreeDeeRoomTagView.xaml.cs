using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;


// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace ThreeDeeRoomTags.ThreeDeeRoomTagButton
{
    public sealed partial class ThreeDeeRoomTagView : Window
    {
        public ThreeDeeRoomTagView()
        {
            this.InitializeComponent();
        }

        /// <summary>
        /// The view model, or null while the window is being built.
        ///
        /// Every handler below goes through this. The data context is assigned after construction,
        /// and the combo boxes raise SelectionChanged as their bindings first resolve — so a
        /// straight cast here used to be a null reference waiting for the right ordering.
        /// </summary>
        private ThreeDeeRoomTagViewModel ViewModel => this.DataContext as ThreeDeeRoomTagViewModel;

        private void Selector_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!IsLoaded) return;

            var vm = ViewModel;
            if (vm is null) return;

            // A cleared phase clears what would be tagged with it. Returning early instead left
            // the previous phase's element count on screen and armed the run button with it.
            vm.RefreshRooms(this.PhaseComboBox.SelectedItem as Phase);
        }

        private void Link_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!IsLoaded) return;

            var vm = ViewModel;
            if (vm is null) return;

            vm.RefreshPhasesForCurrentSource();
        }

        private void ToggleButton_OnChecked(object sender, RoutedEventArgs e)
        {
            if (!IsLoaded) return;

            var vm = ViewModel;
            if (vm is null) return;

            // Was RefreshPhases(null) either way, so ticking "use a linked model" listed the host
            // document's phases and a run against them tagged the wrong model.
            vm.RefreshPhasesForCurrentSource();
        }

        private void LicenceLink_OnClick(object sender, RoutedEventArgs e)
        {
            string url = "https://github.com/johnpierson/3dSpatialTags/blob/main/LICENSE";

            try
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = url,
                    UseShellExecute = true
                });
            }
            catch (Exception)
            {
                // No browser, or a policy that blocks launching one. Not worth taking the dialog
                // down over a credit link.
            }
        }

        private void TargetChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!IsLoaded) return;

            var vm = ViewModel;
            if (vm is null) return;

            // Clearing the phase is a view concern — it is this control's own selection. What
            // that means for the saved setting, the window title and the collected elements is
            // the view model's, and lives there.
            this.PhaseComboBox.SelectedIndex = -1;

            vm.ChangeTarget(this.TargetComboBox.SelectedIndex);
        }

        private void FamilySymbolSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!IsLoaded) return;

            ViewModel?.ChangeFamilySymbol(this.FamilySymbolComboBox.SelectedIndex);
        }
    }
}
