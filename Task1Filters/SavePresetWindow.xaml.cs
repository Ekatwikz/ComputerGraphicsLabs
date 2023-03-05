using System.Windows;

namespace Task1Filters {
    /// <summary>
    /// Interaction logic for SavePresetWindow.xaml
    /// </summary>
    public partial class SavePresetWindow : Window {
        public SavePresetWindow() {
            InitializeComponent();
        }

        private void OkButton_Click(object sender, RoutedEventArgs e) {
            // Set the DialogResult to true and close the window
            DialogResult = true;
        }
    }
}
