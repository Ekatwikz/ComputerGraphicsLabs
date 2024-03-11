using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Double;
using MathNet.Spatial.Euclidean;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows;
using System.Windows.Input;
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
        private const int IMAGE_WIDTH = 500;
        private const int IMAGE_HEIGHT = 500;

        private const double CAMERA_THETA = Math.PI / 2;

        private const int CYLINDER_RESO = 16;
        private const int CYLINDER_HEIGHT = 5;
        private const int CYLINDER_RADIUS = 2;
        private const double CAM_SPEED = 0.3;

        public WriteableBitmap ImageBitmap { get; set; }

        private byte[] PixelBuffer { get; set; }

        private PositionNormalPair[] CylinderVertices { get; set; }

        List<Tuple<int, int, int>> CylinderTriangles { get; set; }

        private Vector3D CameraPosition { get; set; } = new(6, 9, 0);
        private Vector3D CameraInitialTarget { get; set; } = new(0, 3, 0);
        private Vector3D CameraUp { get; set; } = new(0, 1, 0);

        public MainWindow() {
            InitializeComponent();

            Bitmap blankWhiteBitmap = new(IMAGE_WIDTH, IMAGE_HEIGHT);
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

            CylinderVertices = new PositionNormalPair[4 * CYLINDER_RESO + 2];
            // top
            CylinderVertices[0] = new PositionNormalPair {
                Position = DenseMatrix.OfArray(new double[,] {
                    { 0 },
                    { CYLINDER_HEIGHT },
                    { 0 },
                    { 1 }
                }),
                Normal = up
            };
            for (int i = 0; i <= CYLINDER_RESO - 1; ++i) {
                CylinderVertices[i + 1] = new PositionNormalPair {
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
            CylinderVertices[4 * CYLINDER_RESO + 1] = new PositionNormalPair {
                Position = DenseMatrix.OfArray(new double[,] {
                    { 0 },
                    { 0 },
                    { 0 },
                    { 1 }
                }),
                Normal = down
            };
            for (int i = 0; i <= CYLINDER_RESO - 1; ++i) {
                CylinderVertices[3 * CYLINDER_RESO + i + 1] = new PositionNormalPair {
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
                CylinderVertices[i] = new PositionNormalPair {
                    Position = CylinderVertices[i - CYLINDER_RESO].Position,
                    Normal = DenseMatrix.OfArray(new double[,] {
                    { CylinderVertices[i - CYLINDER_RESO].Position![0, 0] / CYLINDER_RADIUS },
                    { 0 },
                    { CylinderVertices[i - CYLINDER_RESO].Position![2, 0] / CYLINDER_RADIUS },
                    { 1 }
                })
                };
            }
            for (int i = 2 * CYLINDER_RESO + 1; i <= 3 * CYLINDER_RESO; ++i) {
                CylinderVertices[i] = new PositionNormalPair {
                    Position = CylinderVertices[i + CYLINDER_RESO].Position,
                    Normal = DenseMatrix.OfArray(new double[,] {
                    { CylinderVertices[i + CYLINDER_RESO].Position![0, 0] / CYLINDER_RADIUS },
                    { 0 },
                    { CylinderVertices[i + CYLINDER_RESO].Position![2, 0] / CYLINDER_RADIUS },
                    { 1 }
                })
                };
            }

            System.Diagnostics.Debug.WriteLine($"===\n<<<BEFORE TRANSFROM>>>\n===\n\n");
            foreach (PositionNormalPair thing in CylinderVertices) {
                System.Diagnostics.Debug.WriteLine($"p:[{thing.Position}]\nn:[{thing.Normal}]\n"); // tmp
            }

            // triangles
            CylinderTriangles = new();

            // top
            for (int i = 0; i <= CYLINDER_RESO - 2; ++i) {
                CylinderTriangles.Add(new Tuple<int, int, int>(0, i + 2, i + 1));
            }
            CylinderTriangles.Add(new Tuple<int, int, int>(0, 1, CYLINDER_RESO));

            // bottom
            for (int i = 3 * CYLINDER_RESO; i <= 4 * CYLINDER_RESO - 2; ++i) {
                CylinderTriangles.Add(new Tuple<int, int, int>(4 * CYLINDER_RESO + 1, i + 1, i + 2));
            }
            CylinderTriangles.Add(new Tuple<int, int, int>(4 * CYLINDER_RESO + 1, 4 * CYLINDER_RESO, 3 * CYLINDER_RESO + 1));

            // side
            for (int i = CYLINDER_RESO; i <= 2 * CYLINDER_RESO - 2; ++i) {
                CylinderTriangles.Add(new Tuple<int, int, int>(i + 1, i + 2, i + 1 + CYLINDER_RESO));
            }
            CylinderTriangles.Add(new Tuple<int, int, int>(2 * CYLINDER_RESO, CYLINDER_RESO + 1, 3 * CYLINDER_RESO));
            for (int i = 2 * CYLINDER_RESO; i <= 3 * CYLINDER_RESO - 2; ++i) {
                CylinderTriangles.Add(new Tuple<int, int, int>(i + 1, i + 2 - CYLINDER_RESO, i + 2));
            }
            CylinderTriangles.Add(new Tuple<int, int, int>(3 * CYLINDER_RESO, CYLINDER_RESO + 1, 2 * CYLINDER_RESO + 1));

            PixelBuffer = new byte[ImageBitmap.BackBufferStride * ImageBitmap.PixelHeight];
            DrawCylinder();
            DataContext = this;

            KeyDown += MainWindow_KeyDown;
        }

        private void DrawCylinder() {
            // cam stuff
            var cZ = (CameraPosition - CameraInitialTarget).Normalize().ToVector3D();
            var cX = CameraUp.CrossProduct(cZ).Normalize().ToVector3D();
            var cY = cZ.CrossProduct(cX).Normalize().ToVector3D();

            var cameraMatrix = DenseMatrix.OfArray(new double[,] {
                    { cX.X, cX.Y, cX.Z, cX.DotProduct(CameraPosition) },
                    { cY.X, cY.Y, cY.Z, cY.DotProduct(CameraPosition) },
                    { cZ.X, cZ.Y, cZ.Z, cZ.DotProduct(CameraPosition) },
                    { 0, 0, 0, 1 }
                });

            //System.Diagnostics.Debug.WriteLine($"Cam: {cameraMatrix}"); // tmp

            var projectionMatrix = DenseMatrix.OfArray(new double[,] {
                    { -IMAGE_WIDTH / Math.Tan(CAMERA_THETA / 2) / 2, 0, IMAGE_WIDTH / 2, 0 },
                    { 0, IMAGE_WIDTH / Math.Tan(CAMERA_THETA / 2) / 2, IMAGE_HEIGHT / 2, 0 },
                    { 0, 0, 0, 1 },
                    { 0, 0, 1, 0 }
                });

            //System.Diagnostics.Debug.WriteLine($"Projection: {cameraMatrix}"); // tmp

            PositionNormalPair[] TransformedCylinderVertices = new PositionNormalPair[4 * CYLINDER_RESO + 2];
            for (int i = 0; i < 4 * CYLINDER_RESO + 2; ++i) {
                TransformedCylinderVertices[i] = new PositionNormalPair {
                    Position = CylinderVertices[i].Position!.Clone(),
                    Normal = CylinderVertices[i].Position!.Clone()
                };
            }

            for (int i = 0; i < TransformedCylinderVertices.Length; ++i) {
                TransformedCylinderVertices[i].Position = cameraMatrix * TransformedCylinderVertices[i].Position;
                TransformedCylinderVertices[i].Position = projectionMatrix * TransformedCylinderVertices[i].Position;
                TransformedCylinderVertices[i].Position /= TransformedCylinderVertices[i].Position![3, 0];
            }

            // tmp
            //System.Diagnostics.Debug.WriteLine($"===\n<<<AFTER TRANSFROM>>>\n===\n\n");
            //foreach (PositionNormalPair thing in TransformedCylinderVertices) {
            //    System.Diagnostics.Debug.WriteLine($"p:[{thing.Position}]\nn:[{thing.Normal}]\n");
            //}

            //int stride = ImageBitmap.BackBufferStride;
            Array.Clear(PixelBuffer, 0, PixelBuffer.Length);

            // draw triangles
            foreach (var triangle in CylinderTriangles) {
                var vertex1 = TransformedCylinderVertices[triangle.Item1];
                var vertex2 = TransformedCylinderVertices[triangle.Item2];
                var vertex3 = TransformedCylinderVertices[triangle.Item3];

                if (new Vector3D(vertex2.Position![0, 0] - vertex1.Position![0, 0], vertex2.Position![1, 0] - vertex1.Position![1, 0], 0)
                    .CrossProduct(new Vector3D(vertex3.Position![0, 0] - vertex1.Position![0, 0], vertex3.Position![1, 0] - vertex1.Position![1, 0], 0))
                    .Z > 0) {
                    DrawLine(
                        (int)vertex1.Position![0, 0], (int)vertex1.Position![1, 0],
                        (int)vertex2.Position![0, 0], (int)vertex2.Position![1, 0]
                    );
                    DrawLine(
                        (int)vertex2.Position![0, 0], (int)vertex2.Position![1, 0],
                        (int)vertex3.Position![0, 0], (int)vertex3.Position![1, 0]
                    );
                    DrawLine(
                        (int)vertex3.Position![0, 0], (int)vertex3.Position![1, 0],
                        (int)vertex1.Position![0, 0], (int)vertex1.Position![1, 0]
                    );
                }
            }

            ImageBitmap.WritePixels(new Int32Rect(0, 0, ImageBitmap.PixelWidth, ImageBitmap.PixelHeight), PixelBuffer, ImageBitmap.BackBufferStride, 0);
        }

        protected static void Swap<T>(ref T a, ref T b) {
            (b, a) = (a, b);
        }

        private void DrawLine(int x1, int y1, int x2, int y2) {
            bool steep = Math.Abs(y2 - y1) > Math.Abs(x2 - x1);
            if (steep) {
                Swap(ref x1, ref y1);
                Swap(ref x2, ref y2);
            }
            if (x1 > x2) {
                Swap(ref x1, ref x2);
                Swap(ref y1, ref y2);
            }
            int dx = x2 - x1;
            int dy = Math.Abs(y2 - y1);
            int d = dy * 2 - dx;
            int y = y1;
            int ystep = (y1 < y2) ? 1 : -1;
            for (int x = x1; x <= x2; x++) {
                if (0 > x || x >= IMAGE_WIDTH
                    || 0 > y || y >= IMAGE_HEIGHT) {
                    continue;
                }

                int pixelIndex;
                if (steep) {
                    pixelIndex = x * ImageBitmap.BackBufferStride + y * 4;
                } else {
                    pixelIndex = y * ImageBitmap.BackBufferStride + x * 4;
                }

                //PixelBuffer[pixelIndex] = 0;
                //PixelBuffer[pixelIndex + 1] = 0;
                PixelBuffer[pixelIndex + 2] = 255;
                PixelBuffer[pixelIndex + 3] = 255;

                if (d > 0) {
                    y += ystep;
                    d -= dx * 2;
                }
                d += dy * 2;
            }
        }

        private void MainWindow_KeyDown(object sender, KeyEventArgs e) {
            switch (e.Key) {
                // Cam Pos controls:
                case Key.Up:
                    CameraPosition += new Vector3D(-CAM_SPEED, 0, 0);
                    break;
                case Key.Down:
                    CameraPosition += new Vector3D(CAM_SPEED, 0, 0);
                    break;
                case Key.Left:
                    CameraPosition += new Vector3D(0, 0, -CAM_SPEED);
                    break;
                case Key.Right:
                    CameraPosition += new Vector3D(0, 0, CAM_SPEED);
                    break;
                case Key.O:
                    CameraPosition += new Vector3D(0, CAM_SPEED, 0);
                    break;
                case Key.L:
                    CameraPosition += new Vector3D(0, -CAM_SPEED, 0);
                    break;

                case Key.W:
                    CameraInitialTarget += new Vector3D(-CAM_SPEED, 0, 0);
                    break;
                case Key.S:
                    CameraInitialTarget += new Vector3D(CAM_SPEED, 0, 0);
                    break;
                case Key.A:
                    CameraInitialTarget += new Vector3D(0, 0, -CAM_SPEED);
                    break;
                case Key.D:
                    CameraInitialTarget += new Vector3D(0, 0, CAM_SPEED);
                    break;
                case Key.R:
                    CameraInitialTarget += new Vector3D(0, CAM_SPEED, 0);
                    break;
                case Key.F:
                    CameraInitialTarget += new Vector3D(0, -CAM_SPEED, 0);
                    break;

                default:
                    return;
            }

            DrawCylinder();
        }
    }
}
