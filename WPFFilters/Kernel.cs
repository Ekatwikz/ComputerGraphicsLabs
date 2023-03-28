using System;
using System.Windows.Input;

namespace WPFFilters {
    public abstract class Kernel : NamedMemberOfRefreshable, ICloneable {
        public abstract int[,] KernelArray { get; set; }
        public abstract int Width { get; }
        public abstract int Height { get; }

        public abstract int Denominator { get; set; }
        public abstract bool DenominatorIsLinkedToKernel { get; set; }

        public abstract int CenterPixelPosX { get; set; }
        public abstract int CenterPixelPosY { get; set; }

        public abstract bool CenterPixelIsLinkedToKernel { get; protected set; }

        public abstract string Info { get; }
        public abstract string VerboseName { get; }

        public abstract ICommand ToggleCenterPixelLinkCommand { get; protected set; }
        public abstract ICommand ToggleDenominatorLinkCommand { get; protected set; }
        public abstract ICommand ModificationCommand { get; protected set; }

        public abstract object Clone();
    }
}
