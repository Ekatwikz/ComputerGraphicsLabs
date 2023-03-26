using System.ComponentModel;

namespace WPFFilters {
    // idea is that this contains some member(s) which can receive direct updates
    // which we care about, so we ask to be notified?
    public interface IRefreshableContainer {
        void Refresh(bool forceRefresh = false);
    }

    public abstract class MemberOfRefreshable {
        public IRefreshableContainer RefreshableContainer { get; set; }
    }

    public abstract class NamedMemberOfRefreshable : MemberOfRefreshable, INotifyPropertyChanged {
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propertyName) {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private string _baseName;
        public string BaseName {
            get => _baseName;
            set {
                _baseName = value;
                OnPropertyChanged(nameof(BaseName));
                BaseNameChangedHook();
            }
        }

        protected virtual void BaseNameChangedHook() { }
    }
}