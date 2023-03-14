using System;
using System.Windows.Input;

namespace Task1Filters {
    public class RelayCommand : ICommand {
        private readonly Action<object> _executeWithParam;
        private readonly Action _execute;
        public void Execute(object parameter) {
            _executeWithParam?.Invoke(parameter);
            _execute?.Invoke();
        }

        private readonly Func<object, bool> _canExecute;
        public bool CanExecute(object parameter) {
            return _canExecute == null || _canExecute(parameter);
        }

        public event EventHandler CanExecuteChanged;
        public void RaiseCanExecuteChanged() {
            CanExecuteChanged?.Invoke(this, EventArgs.Empty); // ?
        }

        #region creation
        public RelayCommand(Action execute, Action<object> executeWithParam, Func<object, bool> canExecute) {
            _execute = execute;
            _executeWithParam = executeWithParam;
            _canExecute = canExecute;
        }

        public RelayCommand(Action execute, Func<object, bool> canExecute)
            : this(execute, null, canExecute) {
        }

        public RelayCommand(Action<object> executeWithParam, Func<object, bool> canExecute)
            : this(null, executeWithParam, canExecute) {
        }

        public RelayCommand(Action execute, Action<object> executeWithParam)
            : this(execute, executeWithParam, null) {
        }

        public RelayCommand(Action execute)
        : this(execute, null) {
        }

        public RelayCommand(Action<object> executeWithParam)
            : this(null, executeWithParam) {
        }
        #endregion
    }
}
