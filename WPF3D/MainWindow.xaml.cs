using System;
using System.Collections.Generic;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Double;
using MathNet.Spatial.Euclidean;
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
        private const int CYLINDER_RADIUS = 2;

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

            var up = DenseMatrix.OfArray(new double[,] {
                    { 0 },
                    { 1 },
                    { 0 },
                    { 0 }
                });
            var down = DenseMatrix.OfArray(new double[,] {
                    { 0 },
                    { -1 },
                    { 0 },
                    { 0 }
                });

            PositionNormalPair[] cylinderVertices = new PositionNormalPair[4 * CYLINDER_RESO + 2];
            // top
            cylinderVertices[0] = new PositionNormalPair {
                Position = DenseMatrix.OfArray(new double[,] {
                    { 0 },
                    { CYLINDER_HEIGHT },
                    { 0 },
                    { 1 }
                }),
                Normal = up
            };
            for (int i = 0; i <= CYLINDER_RESO - 1; ++i) {
                cylinderVertices[i + 1] = new PositionNormalPair {
                    Position = DenseMatrix.OfArray(new double[,] {
                    { CYLINDER_RADIUS * Math.Cos(2 * Math.PI * i / CYLINDER_RESO) },
                    { CYLINDER_HEIGHT },
                    { CYLINDER_RADIUS * Math.Sin(2 * Math.PI * i / CYLINDER_RESO) },
                    { 1 }
                }),
                    Normal = up
                };
            }

            // bottom
            cylinderVertices[4 * CYLINDER_RESO + 1] = new PositionNormalPair {
                Position = DenseMatrix.OfArray(new double[,] {
                    { 0 },
                    { CYLINDER_HEIGHT },
                    { 0 },
                    { 1 }
                }),
                Normal = down
            };
            for (int i = 0; i <= CYLINDER_RESO - 1; ++i) {
                cylinderVertices[3 * CYLINDER_RESO + i + 1] = new PositionNormalPair {
                    Position = DenseMatrix.OfArray(new double[,] {
                    { CYLINDER_RADIUS * Math.Cos(2 * Math.PI * i / CYLINDER_RESO) },
                    { 0 },
                    { CYLINDER_RADIUS * Math.Sin(2 * Math.PI * i / CYLINDER_RESO) },
                    { 1 }
                }),
                    Normal = down
                };
            }

            // side
            for (int i = CYLINDER_RESO + 1; i <= 2 * CYLINDER_RESO; ++i) {
                cylinderVertices[i] = new PositionNormalPair {
                    Position = cylinderVertices[i - CYLINDER_RESO].Position,
                    Normal = DenseMatrix.OfArray(new double[,] {
                    { cylinderVertices[i - CYLINDER_RESO].Position![0, 0] / CYLINDER_RADIUS },
                    { 0 },
                    { cylinderVertices[i - CYLINDER_RESO].Position![2, 0] / CYLINDER_RADIUS },
                    { 1 }
                })
                }; 
            }
            for (int i = 2 * CYLINDER_RESO + 1; i <= 3 * CYLINDER_RESO; ++i) {
                cylinderVertices[i] = new PositionNormalPair {
                    Position = cylinderVertices[i + CYLINDER_RESO].Position,
                    Normal = DenseMatrix.OfArray(new double[,] {
                    { cylinderVertices[i + CYLINDER_RESO].Position![0, 0] / CYLINDER_RADIUS },
                    { 0 },
                    { cylinderVertices[i + CYLINDER_RESO].Position![2, 0] / CYLINDER_RADIUS },
                    { 1 }
                })
                };
            }

            foreach (PositionNormalPair thing in cylinderVertices) {
                System.Diagnostics.Debug.WriteLine($"p:[{thing.Position}]\nn:[{thing.Normal}]\n");
            }

            // triangles
            List<Tuple<int, int, int>> triangles = new List<Tuple<int, int, int>>();

            // top
            for (int i = 0; i <= CYLINDER_RESO - 2; ++i) {
                triangles.Add(new Tuple<int, int, int>(0, i + 2, i + 1));
            }
            triangles.Add(new Tuple<int, int, int>(0, 1, CYLINDER_RESO));

            // bottom
            for (int i = 3 * CYLINDER_RESO; i <= 4 * CYLINDER_RESO - 2; ++i) {
                triangles.Add(new Tuple<int, int, int>(4 * CYLINDER_RESO + 1, i + 1, i + 2));
            }
            triangles.Add(new Tuple<int, int, int>(4 * CYLINDER_RESO + 1, 4 * CYLINDER_RESO, 3 * CYLINDER_RESO + 1));

            // side
            for (int i = CYLINDER_RESO; i <= 2 * CYLINDER_RESO - 2; ++i) {
                triangles.Add(new Tuple<int, int, int>(i + 1, i + 2, i + 1 + CYLINDER_RESO));
            }
            triangles.Add(new Tuple<int, int, int>(2 * CYLINDER_RESO, CYLINDER_RESO + 1, 3 * CYLINDER_RESO));
            for (int i = 2 * CYLINDER_RESO; i <= 3 * CYLINDER_RESO - 2; ++i) {
                triangles.Add(new Tuple<int, int, int>(i + 1, i + 2 - CYLINDER_RESO, i + 2));
            }
            triangles.Add(new Tuple<int, int, int>(3 * CYLINDER_RESO, CYLINDER_RESO + 1, 2 * CYLINDER_RESO + 1));

            DataContext = this;
        }
    }
}
