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

        private void Selector_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (PhaseComboBox.SelectedIndex == -1)
            {
                return;
            }
            var vm = this.DataContext as ThreeDeeRoomTagViewModel;

            var phase = this.PhaseComboBox.SelectedItem as Phase;
            vm.RefreshRooms(phase);
        }

        private void Link_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            this.PhaseComboBox.SelectedIndex = -1;
            var vm = this.DataContext as ThreeDeeRoomTagViewModel;

            var linkInstance = this.LinkComboBox.SelectedItem as RevitLinkInstance;

            vm.RefreshPhases(linkInstance);
        }

        private void ToggleButton_OnChecked(object sender, RoutedEventArgs e)
        {
            this.PhaseComboBox.SelectedIndex = -1;
            var vm = this.DataContext as ThreeDeeRoomTagViewModel;
            vm.RefreshPhases(null);
        }

        private void UIElement_OnMouseDown(object sender, MouseButtonEventArgs e)
        {
            string url = "https://github.com/johnpierson/3dSpatialTags/blob/main/LICENSE";
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = url,
                UseShellExecute = true
            });
        }

        private void TargetChanged(object sender, SelectionChangedEventArgs e)
        {
            if(!IsLoaded) return;

            var vm = this.DataContext as ThreeDeeRoomTagViewModel;

            var index =this.TargetComboBox.SelectedIndex;
            Properties.Settings.Default.TargetIndex = index;
            Properties.Settings.Default.Save();

            vm.TitleText = index == 0 ? "3d Room Tags" : "3d Space Tags";
        }

        private void FamilySymbolSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!IsLoaded) return;
            var index = this.FamilySymbolComboBox.SelectedIndex;
            Properties.Settings.Default.FamilySymbolIndex = index;
            Properties.Settings.Default.Save();
        }
    }
}
