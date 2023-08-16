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
        private const int IMAGE_WIDTH = 100;
        private const int IMAGE_HEIGHT = 100;

        private const double CAMERA_THETA = Math.PI / 2;

        private const int CYLINDER_RESO = 8;
        private const int CYLINDER_HEIGHT = 5;
        private const int CYLINDER_RADIUS = 2;

        public WriteableBitmap ImageBitmap { get; set; }

        public MainWindow() {
            InitializeComponent();

            Bitmap blankWhiteBitmap = new Bitmap(IMAGE_WIDTH, IMAGE_HEIGHT);
            using (Graphics graphics = Graphics.FromImage(blankWhiteBitmap)) {
                graphics.Clear(Color.White);
            }

            ImageBitmap = new WriteableBitmap(blankWhiteBitmap.ToBitmapSource());

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

            System.Diagnostics.Debug.WriteLine($"===\n<<<BEFORE TRANSFROM>>>\n===\n\n");
            foreach (PositionNormalPair thing in cylinderVertices) {
                System.Diagnostics.Debug.WriteLine($"p:[{thing.Position}]\nn:[{thing.Normal}]\n"); // tmp
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

            // cam stuff
            var cPos = new Vector3D(6, 9, 0);
            var cTarget = new Vector3D(0, 3, 0);
            var cUp = new Vector3D(-1, 1, 0);

            var cZ = (cPos - cTarget).Normalize().ToVector3D();
            var cX = cUp.CrossProduct(cZ).Normalize().ToVector3D();
            var cY = cZ.CrossProduct(cX).Normalize().ToVector3D();

            var cameraMatrix = DenseMatrix.OfArray(new double[,] {
                    { cX.X, cX.Y, cX.Z, cX.DotProduct(cPos) },
                    { cY.X, cY.Y, cY.Z, cY.DotProduct(cPos) },
                    { cZ.X, cZ.Y, cZ.Z, cZ.DotProduct(cPos) },
                    { 0, 0, 0, 1 }
                });

            System.Diagnostics.Debug.WriteLine($"Cam: {cameraMatrix}"); // tmp

            var projectionMatrix = DenseMatrix.OfArray(new double[,] {
                    { -IMAGE_WIDTH / Math.Tan(CAMERA_THETA / 2) / 2, 0, IMAGE_WIDTH / 2, 0 },
                    { 0, IMAGE_WIDTH / Math.Tan(CAMERA_THETA / 2) / 2, IMAGE_HEIGHT / 2, 0 },
                    { 0, 0, 0, 1 },
                    { 0, 0, 1, 0 }
                });

            System.Diagnostics.Debug.WriteLine($"Projection: {cameraMatrix}"); // tmp

            for (int i = 0; i < cylinderVertices.Length; ++i) {
                cylinderVertices[i].Position = cameraMatrix * cylinderVertices[i].Position;
                cylinderVertices[i].Position /= cylinderVertices[i].Position![3, 0];
            }

            // tmp
            System.Diagnostics.Debug.WriteLine($"===\n<<<AFTER TRANSFROM>>>\n===\n\n");
            foreach (PositionNormalPair thing in cylinderVertices) {
                System.Diagnostics.Debug.WriteLine($"p:[{thing.Position}]\nn:[{thing.Normal}]\n");
            }

            int stride = ImageBitmap.BackBufferStride;
            byte[] pixelBuffer = new byte[ImageBitmap.BackBufferStride * ImageBitmap.PixelHeight];

            foreach (var vertex in cylinderVertices) {
                int x = (int)vertex.Position![0, 0];
                int y = (int)vertex.Position![1, 0];

                int pixelIndex = (y * stride) + (x * 4); // Each pixel is 4 bytes (BGR32 format)
                pixelBuffer[pixelIndex] = 0;
                pixelBuffer[pixelIndex + 1] = 0;
                pixelBuffer[pixelIndex + 2] = 255;
                pixelBuffer[pixelIndex + 3] = 255; // Alpha value
            }

            ImageBitmap.WritePixels(new Int32Rect(0, 0, ImageBitmap.PixelWidth, ImageBitmap.PixelHeight), pixelBuffer, ImageBitmap.BackBufferStride, 0);
            DataContext = this;
        }
    }
}
