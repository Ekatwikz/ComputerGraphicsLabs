using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Windows.Input;
using System.Windows.Media;

namespace WPFDrawing {
    [DataContract]
    [KnownType(typeof(BasePoint))]
    [KnownType(typeof(Line))]
    [KnownType(typeof(Polygon))]
    [KnownType(typeof(Circle))]
    public abstract class Shape : NamedMemberOfRefreshable, ICloneable, IBoundedRefreshableContainer {
        public byte[] DrawOnBitmapBuffer(byte[] pixelBuffer, int bitmapPixelWidth, int bitmapPixelHeight, int backBufferStride, PixelFormat format) {
            System.Drawing.Color color;
            int i = 0;

            foreach (RGBCoord colorCoord in PixelCoords) {
                color = colorCoord.CoordColor;
                if (IsSelected && i % 2 == 0) {
                    color = color.Invert();
                }

                (int, int) coord = colorCoord.Coord;

                if (!(0 <= coord.Item1 && coord.Item1 < bitmapPixelWidth)
                    || !(0 <= coord.Item2 && coord.Item2 < bitmapPixelHeight)) {
                    continue;
                }

                int index = coord.Item2 * backBufferStride + coord.Item1 * (format.BitsPerPixel / 8);
                pixelBuffer[index + 2] = color.R;
                pixelBuffer[index + 1] = color.G;
                pixelBuffer[index] = color.B;
                ++i;
            }

            return pixelBuffer;
        }

        #region stuff
        [DataMember]
        public BaseNamedBoundedValue Thickness { get; set; }

        public DataContractSerializer ShapeSerializer { get; }

        [DataMember]
        public SelectableColor Color { get; set; }

        public abstract RGBCoord[] PixelCoords { get; }
        public bool IsSelected { get; set; }

        [DataMember]
        protected (int, int) ContainerSize { get; set; }
        public (int, int) Bounds => ContainerSize; // ???

        public virtual void RefreshHook() {
        }

        public void Refresh(bool forceRefresh = false) {
            OnPropertyChanged(nameof(VerboseName));
            OnPropertyChanged(nameof(PixelCoords));
            RefreshableContainer.Refresh(); // ??
            RefreshHook();
        }

        public abstract string VerboseName { get; } // TODO: change this... !
        protected override void BaseNameChangedHook() => OnPropertyChanged(nameof(VerboseName));
        #endregion

        #region shapeSetup
        public abstract BaseBoundedCoord[] ClickableCoords { get; } // TODO: make a new class for this??
        public int ClickableCoordRadius { get; }

        protected Queue<BaseBoundedCoord> CoordSetupQueue { get; }
        public virtual BaseBoundedCoord GetNextSetupCoord(System.Windows.Point? lastMouseDown)
            => CoordSetupQueue.Count > 0 ? CoordSetupQueue.Dequeue() : null;
        #endregion

        #region commands
        public ICommand SaveShapeCommand { get; private set; }

        public virtual void SaveShape() { // TODO: abstract
            SaveFileDialog saveFileDialog = new SaveFileDialog {
                Title = "Save As",
                Filter = "Shape Preset (*.xml)|*.xml",
                FileName = $"{BaseName}_Preset"
            };

            if (saveFileDialog.ShowDialog() == true) {
                using (FileStream stream = new FileStream(saveFileDialog.FileName, FileMode.Create)) {
                    Shape toSave = (Shape)Clone();
                    toSave.BaseName = Path.GetFileNameWithoutExtension(saveFileDialog.FileName);
                    ShapeSerializer.WriteObject(stream, toSave);
                }

                Console.WriteLine("Shape Saved...");
            }
        }

        protected void Swap<T>(ref T a, ref T b) {
            (b, a) = (a, b);
        }
        #endregion

        #region creation
        private Shape() {
            CoordSetupQueue = new Queue<BaseBoundedCoord>();
            SaveShapeCommand = new RelayCommand(SaveShape);
        }

        public Shape(IRefreshableContainer refreshableContainer, DataContractSerializer shapeSerializer, string name) : this() {
            ShapeSerializer = shapeSerializer; // ?
            RefreshableContainer = refreshableContainer;
            BaseName = name;

            if (refreshableContainer != null) {
                ContainerSize = (refreshableContainer as IBoundedRefreshableContainer).Bounds; // Yuck...
            }

            int width = ContainerSize.Item1;
            int height = ContainerSize.Item2;
            double containerDiag = Math.Sqrt(width * width + height * height);

            ClickableCoordRadius = (int)(containerDiag / 50);
            Thickness = new NamedBoundedValue(this, nameof(Thickness), 1, (0, Math.Floor(containerDiag / 40)));
        }

        public abstract object Clone(); // makes Presets easier?
        #endregion
    }

    public struct RGBCoord {
        public (int, int) Coord;
        public System.Drawing.Color CoordColor;
    }
}
