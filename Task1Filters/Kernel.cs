using System;
using System.Collections.ObjectModel;
using System.Windows.Controls;
using System.Windows.Input;

namespace Task1Filters {
    // TODO: new class with Offset
    public class Kernel : NamedMemberOfRefreshable, ICloneable, IRefreshableContainer {
        #region properties
        private ObservableCollection<ObservableCollection<WrappedValue>> _kernelValues;
        public ObservableCollection<ObservableCollection<WrappedValue>> KernelValues {
            get => _kernelValues;
            protected set {
                _kernelValues = value;
                ResetCenterPixelIfLinked();
                // notifyKernelShapeChanged(); // ?
                OnPropertyChanged(nameof(Denominator));
                OnPropertyChanged(nameof(KernelValues));
                RefreshableContainer?.Refresh(); // ?
            }
        }

        public int KernelHeight => KernelValues?.Count ?? 0;
        public int KernelWidth => KernelValues?.Count > 0 ? KernelValues[0].Count : 0;

        public int[,] KernelArray {
            get { // TODO: cache get/set?
                int[,] kernelArray = new int[KernelHeight, KernelWidth];

                for (int i = 0; i < KernelHeight; ++i) {
                    for (int j = 0; j < KernelWidth; ++j) {
                        kernelArray[i, j] = KernelValues[i][j].Value;
                    }
                }

                return kernelArray;
            }

            set {
                int kernelHeight = value.GetLength(0);
                int kernelWidth = value.GetLength(1);

                ObservableCollection<ObservableCollection<WrappedValue>> kernel = new ObservableCollection<ObservableCollection<WrappedValue>>();
                for (int i = 0; i < kernelHeight; ++i) {
                    kernel.Add(new ObservableCollection<WrappedValue>());

                    for (int j = 0; j < kernelWidth; ++j) {
                        kernel[i].Add(new WrappedValue(value[i, j]) {
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
        public int Denominator {
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
                if (value != 0) {
                    DenominatorIsLinkedToKernel = false;
                    _denominator = value;
                    OnPropertyChanged(nameof(Denominator));
                    RefreshableContainer?.Refresh();
                }
            }
        }

        private NamedBoundedValue _offset;
        public NamedBoundedValue Offset {
            get => _offset;
            set {
                _offset = value;
                OnPropertyChanged(nameof(Info));
                OnPropertyChanged(nameof(VerboseName));
                RefreshableContainer?.Refresh();
            }
        }

        // eww
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
                _centerPixelPosX = KernelWidth / 2;
                _centerPixelPosY = KernelHeight / 2;

                OnPropertyChanged(nameof(CenterPixelPosX));
                OnPropertyChanged(nameof(CenterPixelPosY));

                RefreshableContainer?.Refresh();
            }
        }

        public string Info {
            get {
                string extraInfo = ""; // TODO: stringbuilder?

                if (Offset.Value != 0) {
                    extraInfo = $": Offset {Math.Round(Offset.Value, 3)})";
                } else if (!DenominatorIsLinkedToKernel || !CenterPixelIsLinkedToKernel) {
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
            OnPropertyChanged(nameof(KernelWidth));
            OnPropertyChanged(nameof(KernelHeight));
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

        public ICommand ResetOffsetCommand { get; private set; }
        public void ResetOffset() {
            Offset.Value = 0;
            OnPropertyChanged(nameof(Info));
            OnPropertyChanged(nameof(VerboseName));
            OnPropertyChanged(nameof(Offset));
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
        public void ModifyShape(ModificationFlags kernelModificationFlags) {
            if ((kernelModificationFlags & ModificationFlags.SHOULDADD).ToBool()) {
                if ((kernelModificationFlags & (ModificationFlags.TOP | ModificationFlags.BOTTOM)).ToBool()) {
                    var newRow = new ObservableCollection<WrappedValue>();
                    for (int i = 0; i < Math.Max(KernelWidth, 1); ++i)
                        newRow.Add(new WrappedValue() {
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
                    if (KernelWidth == 0) {
                        KernelArray = new int[,] { { 0 } };
                        goto Done;
                    }

                    if ((kernelModificationFlags & ModificationFlags.LEFT).ToBool()) {
                        foreach (var row in KernelValues) {
                            row.Insert(0, new WrappedValue() {
                                RefreshableContainer = this
                            });
                        }
                    }

                    if ((kernelModificationFlags & ModificationFlags.RIGHT).ToBool()) {
                        foreach (var row in KernelValues) {
                            row.Add(new WrappedValue() {
                                RefreshableContainer = this
                            });
                        }
                    }
                }
            } else { // ShouldRemove
                if (KernelHeight == 0) {
                    goto Done;
                }

                if ((kernelModificationFlags & ModificationFlags.TOP).ToBool()) {
                    KernelValues.RemoveAt(0);
                }

                if ((kernelModificationFlags & ModificationFlags.BOTTOM).ToBool()) {
                    KernelValues.RemoveAt(KernelHeight - 1);
                }

                if ((kernelModificationFlags & (ModificationFlags.LEFT | ModificationFlags.RIGHT)).ToBool()
                    && KernelWidth == 1) {
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
                        row.RemoveAt(KernelWidth - 1);
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
        public Kernel() {
            BaseName = "Kernel";
            ToggleCenterPixelLinkCommand = new RelayCommand(ToggleCenterPixelLink);
            ToggleDenominatorLinkCommand = new RelayCommand(ToggleDenominatorLink);
            ResetOffsetCommand = new RelayCommand(ResetOffset);
            ModificationCommand = new RelayCommand((object val) => ModifyShape((string)val));
        }

        public Kernel(IRefreshableContainer refreshableContainer, int[,] kernelArray, int offset = 0)
            : this() {
            KernelArray = kernelArray;
            Offset = new NamedBoundedValue(nameof(Offset),
                offset,
                (-255, 255)) {
                RefreshableContainer = this
            };

            RefreshableContainer = refreshableContainer;
        }

        public Kernel(Kernel kernel)
            : this() {
            KernelArray = kernel.KernelArray;
            Offset = (NamedBoundedValue)kernel.Offset.Clone();
            Offset.RefreshableContainer = this;
        }

        public void Refresh(bool byForce = false) {
            OnPropertyChanged(nameof(Info));
            OnPropertyChanged(nameof(VerboseName));
            OnPropertyChanged(nameof(Denominator));
            RefreshableContainer?.Refresh();
        }

        public object Clone() {
            return new Kernel(this);
        }
        #endregion
    }

    public class WrappedValue : MemberOfRefreshable {
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

        public WrappedValue(int value = 0) {
            Value = value;
        }
    }
}
