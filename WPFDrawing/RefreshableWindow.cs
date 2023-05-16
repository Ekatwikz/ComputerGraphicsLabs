using System.ComponentModel;
using System.Runtime.Serialization;
using System.Windows;

namespace WPFDrawing {
    [DataContract]
    [KnownType(typeof(MainWindowDto))]
    public abstract class RefreshableWindowDto {
        [DataMember]
        public bool ShouldAutoRefresh { get; set; } = true;

        [DataMember]
        public RenderSettings CurrentRenderSettings { get; set; }
    }

    public abstract class RefreshableWindow : Window, INotifyPropertyChanged, IBoundedRefreshableContainer, IRenderSettingsProvider {
        protected abstract RefreshableWindowDto Dto { get; set; }

        protected abstract void RefreshHook();
        public void Refresh(bool forceRefresh = false) {
            if (ShouldAutoRefresh || forceRefresh) {
                RefreshHook();
            }
        }

        #region stuff
        public abstract (int, int) Bounds { get; }

        private bool _shouldAutoRefresh = true;

        public bool ShouldAutoRefresh {
            get => _shouldAutoRefresh;
            protected set {
                if (_shouldAutoRefresh != value) {
                    _shouldAutoRefresh = value;
                    Dto.ShouldAutoRefresh = value;
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
                Dto.CurrentRenderSettings = value;
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
        #endregion
    }
}
