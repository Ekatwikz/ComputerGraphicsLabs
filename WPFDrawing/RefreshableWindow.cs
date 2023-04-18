using System.ComponentModel;
using System.Windows;
using System.Windows.Media.TextFormatting;

namespace WPFDrawing {
    public abstract class RefreshableWindow : Window, INotifyPropertyChanged, IBoundedRefreshableContainer, IRenderSettingsProvider {
        protected abstract void RefreshHook();
        public void Refresh(bool forceRefresh = false) {
            if (ShouldAutoRefresh || forceRefresh) {
                RefreshHook();
            }
        }

        public abstract (int, int) Bounds { get; }

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

        protected RenderSettings _currentRenderSettings;
        public RenderSettings CurrentRenderSettings {
            get => _currentRenderSettings;
            set {
                _currentRenderSettings = value;
                OnPropertyChanged(nameof(CurrentRenderSettings));
                Refresh();
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
