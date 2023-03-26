using System;

namespace WPFFilters {
    public abstract class Kernel : NamedMemberOfRefreshable, ICloneable {
        public abstract int[,] KernelArray { get; set; }
        public abstract int Width { get; }
        public abstract int Height { get; }
        public abstract int Denominator { get; set; }

        public abstract object Clone();
    }
}
