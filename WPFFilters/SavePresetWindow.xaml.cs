using System.Windows;

namespace WPFFilters {
    /// <summary>
    /// Interaction logic for SavePresetWindow.xaml
    /// </summary>
    public partial class SavePresetWindow : Window {
        public SavePresetWindow() {
            InitializeComponent();
        }

        private void OkButton_Click(object sender, RoutedEventArgs e) {
            // Set Result to true and close window
            DialogResult = true;
        }
    }
}
