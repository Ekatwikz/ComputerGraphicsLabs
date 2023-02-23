using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Task1Filters {
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window {
        public MainWindow() {
            InitializeComponent();
        }

        private void loadImageIn(object sender, RoutedEventArgs e) {

        }

        private void saveImageOut(object sender, RoutedEventArgs e) {

        }

        private void exitApp(object sender, RoutedEventArgs e) {
            Close();
        }

        private void showAboutBox(object sender, RoutedEventArgs e) {
            System.Windows.MessageBox.Show("Task1 - Filters\n(Variant 2: With Convolution Filters' Editor)", "About", MessageBoxButton.OK);
        }
    }
}
