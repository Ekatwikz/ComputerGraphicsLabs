using System.ComponentModel;
using System.Windows;

namespace Task1Filters {
    public abstract class RefreshableWindow : Window, INotifyPropertyChanged, IRefreshableContainer {
        protected abstract void RefreshHook();
        public void Refresh(bool forceRefresh = false) {
            if (ShouldAutoRefresh || forceRefresh) {
                RefreshHook();
            }
        }

        private bool _shouldAutoRefresh = true;
        public bool ShouldAutoRefresh {
            get => _shouldAutoRefresh;
            protected set {
                if (_shouldAutoRefresh != value) {
                    _shouldAutoRefresh = value;
                    OnPropertyChanged(nameof(ShouldAutoRefresh));
                    Refresh();
                }
            }
        }

        protected void ToggleAutoRefresh(object sender, RoutedEventArgs e) {
            ShouldAutoRefresh = !ShouldAutoRefresh;
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propertyName) {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
