using System;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Double;
using System.Drawing;
using System.Windows;
using System.Windows.Media.Imaging;

namespace WPF3D {
    public struct PositionNormalPair {
        public Matrix<double>? Position { get; set; }
        public Matrix<double>? Normal { get; set; }
    }

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window {
        private const int INITIAL_IMAGE_WIDTH = 100;
        private const int INITIAL_IMAGE_HEIGHT = 100;

        private const int CYLINDER_RESO = 4;
        private const int CYLINDER_HEIGHT = 5;
        private const int CYLINDER_RADIUS = 5;

        public WriteableBitmap OriginalBitmap { get; set; }

        public MainWindow() {
            InitializeComponent();

            Bitmap blankWhiteBitmap = new Bitmap(INITIAL_IMAGE_WIDTH, INITIAL_IMAGE_HEIGHT);
            using (Graphics graphics = Graphics.FromImage(blankWhiteBitmap)) {
                graphics.Clear(Color.White);
            }

            OriginalBitmap = new WriteableBitmap(blankWhiteBitmap.ToBitmapSource());

            //Matrix<double> a = DenseMatrix.OfArray(new double[,] { { 1, 2, 3 }, { 4, 5, 6 }, { 7, 8, 9 } });
            //Matrix<double> b = DenseMatrix.OfArray(new double[,] { { 1 }, { 2 }, { 3 } });
            //Matrix<double> result = a * b;

            Matrix<double>[] cylinderTopPositions = new DenseMatrix[CYLINDER_RESO + 1];
            cylinderTopPositions[0] = DenseMatrix.OfArray(new double[,] {
                { 0 },
                { CYLINDER_HEIGHT },
                { 0 },
                { 1 }
            });
            for (int i = 0; i < CYLINDER_RESO; ++i) {
                cylinderTopPositions[i] = DenseMatrix.OfArray(new double[,] {
                    { CYLINDER_RADIUS * Math.Cos(2 * Math.PI * i / CYLINDER_RESO) },
                    { CYLINDER_HEIGHT },
                    { CYLINDER_RADIUS * Math.Sin(2 * Math.PI * i / CYLINDER_RESO) },
                    { 1 }
                });
            }

            Matrix<double>[] cylinderBottomPositions = new DenseMatrix[CYLINDER_RESO + 1];
            for (int i = 0; i < CYLINDER_RESO; ++i) {
                cylinderBottomPositions[i] = DenseMatrix.OfArray(new double[,] {
                    { CYLINDER_RADIUS * Math.Cos(2 * Math.PI * i / CYLINDER_RESO) },
                    { 0 },
                    { CYLINDER_RADIUS * Math.Sin(2 * Math.PI * i / CYLINDER_RESO) },
                    { 1 }
                });
            }

            PositionNormalPair[] cylinderVertices = new PositionNormalPair[4 * CYLINDER_RESO + 2];
            cylinderVertices[0] = new PositionNormalPair {
                Position = DenseMatrix.OfArray(new double[,] {
                    { 0 },
                    { CYLINDER_HEIGHT },
                    { CYLINDER_RADIUS * Math.Sin(2 * Math.PI * i / CYLINDER_RESO) },
                    { 1 }
                }),
                Normal =  null
            };

            DataContext = this;
        }
    }
}
