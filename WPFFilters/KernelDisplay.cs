using System;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace WPFFilters {
    // TODO: new class with Offset
    public class KernelDisplay : Kernel, IRefreshableContainer {
        public void Refresh(bool byForce = false) {
            OnPropertyChanged(nameof(Info));
            OnPropertyChanged(nameof(VerboseName));
            OnPropertyChanged(nameof(Denominator));
            RefreshableContainer?.Refresh();
        }

        #region properties
        private ObservableCollection<ObservableCollection<ContainedValue>> _kernelValues;
        public ObservableCollection<ObservableCollection<ContainedValue>> KernelValues {
            get => _kernelValues;
            protected set {
                _kernelValues = value; // !? Values should be correctly linked!
                ResetCenterPixelIfLinked();
                // notifyKernelShapeChanged(); // ?
                OnPropertyChanged(nameof(Denominator));
                OnPropertyChanged(nameof(KernelValues));
                RefreshableContainer?.Refresh(); // ?
            }
        }

        public override int Height => KernelValues?.Count ?? 0;
        public override int Width => KernelValues?.Count > 0 ? KernelValues[0].Count : 0;

        public override int[,] KernelArray {
            get { // TODO: cache get/set?
                int[,] kernelArray = new int[Height, Width];

                for (int i = 0; i < Height; ++i) {
                    for (int j = 0; j < Width; ++j) {
                        kernelArray[i, j] = KernelValues[i][j].Value;
                    }
                }

                return kernelArray;
            }

            set {
                int kernelHeight = value.GetLength(0);
                int kernelWidth = value.GetLength(1);

                ObservableCollection<ObservableCollection<ContainedValue>> kernel = new ObservableCollection<ObservableCollection<ContainedValue>>();
                for (int i = 0; i < kernelHeight; ++i) {
                    kernel.Add(new ObservableCollection<ContainedValue>());

                    for (int j = 0; j < kernelWidth; ++j) {
                        kernel[i].Add(new ContainedValue(value[i, j]) {
                            RefreshableContainer = this
                        });
                    }
                }

                KernelValues = kernel;
            }
        }

        private bool _denominatorIsLinkedToKernel = true; // recalculate denom every time... ?
        public bool DenominatorIsLinkedToKernel {
            get => _denominatorIsLinkedToKernel;
            set {
                if (_denominatorIsLinkedToKernel != value) { // might be good for UI?
                    _denominatorIsLinkedToKernel = value;
                    OnPropertyChanged(nameof(DenominatorIsLinkedToKernel));
                    OnPropertyChanged(nameof(Denominator));
                    OnPropertyChanged(nameof(Info));
                    OnPropertyChanged(nameof(VerboseName));
                    RefreshableContainer?.Refresh();
                }
            }
        }

        private int _denominator = 1;
        public override int Denominator {
            get {
                if (_denominatorIsLinkedToKernel) {
                    _denominator = 0;

                    foreach (var nums in KernelValues) {
                        foreach (var num in nums) {
                            _denominator += num.Value;
                        }
                    }
                }

                return _denominator;
            }

            set {
                if (_denominator != value && value != 0) {
                    DenominatorIsLinkedToKernel = false;
                    _denominator = value;
                    OnPropertyChanged(nameof(Denominator));
                    RefreshableContainer?.Refresh();
                }
            }
        }

        private int _centerPixelPosX;
        public int CenterPixelPosX {
            get => _centerPixelPosX;
            set {
                CenterPixelIsLinkedToKernel = false;
                _centerPixelPosX = value;
                OnPropertyChanged(nameof(CenterPixelPosX));
                RefreshableContainer?.Refresh();
            }
        }

        private int _centerPixelPosY;
        public int CenterPixelPosY {
            get => _centerPixelPosY;
            set {
                CenterPixelIsLinkedToKernel = false;
                _centerPixelPosY = value;
                OnPropertyChanged(nameof(CenterPixelPosY));
                RefreshableContainer?.Refresh();
            }
        }

        private bool _centerPixelIsLinkedToKernel = true;
        public bool CenterPixelIsLinkedToKernel {
            get => _centerPixelIsLinkedToKernel;
            protected set {
                if (_centerPixelIsLinkedToKernel != value) {
                    _centerPixelIsLinkedToKernel = value;
                    OnPropertyChanged(nameof(CenterPixelIsLinkedToKernel));
                    OnPropertyChanged(nameof(CenterPixelPosX));
                    OnPropertyChanged(nameof(CenterPixelPosY));
                    OnPropertyChanged(nameof(Info));
                    OnPropertyChanged(nameof(VerboseName));

                    ResetCenterPixelIfLinked();
                }
            }
        }

        private void ResetCenterPixelIfLinked() {
            if (CenterPixelIsLinkedToKernel) {
                _centerPixelPosX = Width / 2;
                _centerPixelPosY = Height / 2;

                OnPropertyChanged(nameof(CenterPixelPosX));
                OnPropertyChanged(nameof(CenterPixelPosY));

                RefreshableContainer?.Refresh();
            }
        }

        public string Info {
            get {
                string extraInfo = ""; // TODO: stringbuilder?

                if (!DenominatorIsLinkedToKernel || !CenterPixelIsLinkedToKernel) {
                    extraInfo = ": Tweaked";
                }

                return extraInfo;
            }
        }

        public string VerboseName {
            get => $"{BaseName}{Info}";
        }
        #endregion

        #region notificationHacks
        public void NotifyKernelValuesChanged() {
            OnPropertyChanged(nameof(Info));
            OnPropertyChanged(nameof(VerboseName));
            OnPropertyChanged(nameof(Denominator));
            OnPropertyChanged(nameof(KernelArray));
        }

        public void NotifyKernelShapeChanged() {
            NotifyKernelValuesChanged();
            OnPropertyChanged(nameof(Width));
            OnPropertyChanged(nameof(Height));
        }
        #endregion

        #region commands
        public ICommand ToggleCenterPixelLinkCommand { get; private set; }
        public void ToggleCenterPixelLink() {
            CenterPixelIsLinkedToKernel = !CenterPixelIsLinkedToKernel;
        }

        public ICommand ToggleDenominatorLinkCommand { get; private set; }
        public void ToggleDenominatorLink() {
            DenominatorIsLinkedToKernel = !DenominatorIsLinkedToKernel;
        }

        public ICommand ModificationCommand { get; private set; }
        public void ModifyShape(string modificationActionString) {
            Console.WriteLine(modificationActionString); // TMP!!

            ModificationFlags kernelModificationFlags = 0x0;

            string[] modificationParts = modificationActionString.Split('_');
            string modificationAction = modificationParts[0];
            string modificationActionLocation = modificationParts[1];

            switch (modificationAction) {
                case "add":
                    kernelModificationFlags |= ModificationFlags.SHOULDADD;
                    break;
                case "remove":
                    break;
                default:
                    throw new ArgumentException("Bad button action");
            }

            switch (modificationActionLocation) {
                case "top":
                    kernelModificationFlags |= ModificationFlags.TOP;
                    break;
                case "bottom":
                    kernelModificationFlags |= ModificationFlags.BOTTOM;
                    break;
                case "left":
                    kernelModificationFlags |= ModificationFlags.LEFT;
                    break;
                case "right":
                    kernelModificationFlags |= ModificationFlags.RIGHT;
                    break;
                default:
                    throw new ArgumentException("Bad button action location");
            }

            ModifyShape(kernelModificationFlags);
        }
        #endregion

        #region KernelModification
        public void ModifyShape(ModificationFlags kernelModificationFlags) { // TODO: remove me
            if ((kernelModificationFlags & ModificationFlags.SHOULDADD).ToBool()) {
                if ((kernelModificationFlags & (ModificationFlags.TOP | ModificationFlags.BOTTOM)).ToBool()) {
                    var newRow = new ObservableCollection<ContainedValue>();
                    for (int i = 0; i < Math.Max(Width, 1); ++i)
                        newRow.Add(new ContainedValue() {
                            RefreshableContainer = this
                        });

                    if ((kernelModificationFlags & ModificationFlags.TOP).ToBool()) {
                        KernelValues.Insert(0, newRow);
                    }

                    if ((kernelModificationFlags & ModificationFlags.BOTTOM).ToBool()) {
                        KernelValues.Add(newRow);
                    }
                }

                if ((kernelModificationFlags & (ModificationFlags.LEFT | ModificationFlags.RIGHT)).ToBool()) {
                    if (Width == 0) {
                        KernelArray = new int[,] { { 0 } };
                        goto Done;
                    }

                    if ((kernelModificationFlags & ModificationFlags.LEFT).ToBool()) {
                        foreach (var row in KernelValues) {
                            row.Insert(0, new ContainedValue() {
                                RefreshableContainer = this
                            });
                        }
                    }

                    if ((kernelModificationFlags & ModificationFlags.RIGHT).ToBool()) {
                        foreach (var row in KernelValues) {
                            row.Add(new ContainedValue() {
                                RefreshableContainer = this
                            });
                        }
                    }
                }
            } else { // ShouldRemove
                if (Height == 0) {
                    goto Done;
                }

                if ((kernelModificationFlags & ModificationFlags.TOP).ToBool()) {
                    KernelValues.RemoveAt(0);
                }

                if ((kernelModificationFlags & ModificationFlags.BOTTOM).ToBool()) {
                    KernelValues.RemoveAt(Height - 1);
                }

                if ((kernelModificationFlags & (ModificationFlags.LEFT | ModificationFlags.RIGHT)).ToBool()
                    && Width == 1) {
                    KernelValues.Clear();
                    goto Done;
                }

                if ((kernelModificationFlags & ModificationFlags.LEFT).ToBool()) {
                    foreach (var row in KernelValues) {
                        row.RemoveAt(0);
                    }
                }

                if ((kernelModificationFlags & ModificationFlags.RIGHT).ToBool()) {
                    foreach (var row in KernelValues) {
                        row.RemoveAt(Width - 1);
                    }
                }
            }

        Done:
            ResetCenterPixelIfLinked();
            NotifyKernelShapeChanged();
        }

        public enum ModificationFlags {
            TOP = 0x1,
            BOTTOM,
            LEFT = 0x4,
            RIGHT = 0x8,

            SHOULDADD = 0x10
        }
        #endregion

        #region creation
        public KernelDisplay() {
            BaseName = "Kernel";
            ToggleCenterPixelLinkCommand = new RelayCommand(ToggleCenterPixelLink);
            ToggleDenominatorLinkCommand = new RelayCommand(ToggleDenominatorLink);
            ModificationCommand = new RelayCommand((object val) => ModifyShape((string)val));
        }

        public KernelDisplay(IRefreshableContainer refreshableContainer, int[,] kernelArray)
            : this() {
            KernelArray = kernelArray;

            RefreshableContainer = refreshableContainer;
        }

        public KernelDisplay(KernelDisplay kernel)
            : this() {
            KernelArray = kernel.KernelArray;
        }

        public override object Clone()
            => new KernelDisplay(this);
        #endregion
    }

    public class ContainedValue : MemberOfRefreshable {
        private int _value;
        public int Value {
            get => _value;
            set {
                if (_value != value) {
                    _value = value;
                    RefreshableContainer?.Refresh();
                }
            }
        }

        public ContainedValue(int value = 0) {
            Value = value;
        }
    }
}
