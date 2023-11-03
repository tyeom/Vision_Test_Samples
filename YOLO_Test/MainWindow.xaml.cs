using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Alturos.Yolo;
using System.Drawing;
using static System.Net.Mime.MediaTypeNames;
using System.Drawing.Drawing2D;
using System.IO;
using System.Windows.Media.Media3D;
using System.Windows.Media.Effects;
using OpenCvSharp;
using System.Threading;
using OpenCvSharp.WpfExtensions;
using System.Drawing.Imaging;

namespace YOLO_Test
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : System.Windows.Window
    {
        private readonly VideoCapture _capture;

        private const string YOLO_CONFIG_FOLDER = "Data";
        private const string YOLO_CONFIG_FILE = "Data\\yolov3.cfg";
        private const string YOLO_WEIGHT_FILE = "Data\\yolov3.weights";
        private const string YOLO_NAMES_FILE = "Data\\coco.names";

        private YoloWrapper? _yoloWrapper;
        private CancellationTokenSource? _videoCaptureCancelToken = null;

        public MainWindow()
        {
            InitializeComponent();

            _capture = new VideoCapture();
            this.Loaded += this.MainWindow_Loaded;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            string confihFolderPath = $"{System.AppDomain.CurrentDomain.BaseDirectory}{YOLO_CONFIG_FOLDER}";
            string confihPath = $"{System.AppDomain.CurrentDomain.BaseDirectory}{YOLO_CONFIG_FILE}";
            string weightPath = $"{System.AppDomain.CurrentDomain.BaseDirectory}{YOLO_WEIGHT_FILE}";
            string namesPath = $"{System.AppDomain.CurrentDomain.BaseDirectory}{YOLO_NAMES_FILE}";

            try
            {
                this.InitCamera();

                var configurationDetector = new YoloConfigurationDetector();
                var config = configurationDetector.Detect(confihFolderPath);

                // YOLO 모델 초기화
                _yoloWrapper = new YoloWrapper(config.ConfigFile, config.WeightsFile, config.NamesFile);


            }  catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void xStartCamera_Click(object sender, RoutedEventArgs e)
        {
            this.StartCamera();
        }

        private async void xOpenImage1_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            if (openFileDialog.ShowDialog() == true)
            {
                var drawingImage = await this.YOLODetectAsync(openFileDialog.FileName);
                this.xImage.Source = drawingImage;
            }
        }

        private Task<BitmapImage> YOLODetectAsync(string imageFilePath)
        {
            return Task.Run(() =>
            {
                // 이미지 로드
                BitmapImage image = new BitmapImage(new Uri(imageFilePath, UriKind.Absolute));

                // 01. YOLO로 객체 검출
                var detectItems = _yoloWrapper!.Detect(imageFilePath);

                // 이미지 생성
                var bitmap = new BitmapImage(new Uri(imageFilePath, uriKind: UriKind.Absolute));
                var visual = new DrawingVisual();

                using (DrawingContext drawingContext = visual.RenderOpen())
                {
                    // 02. 이미지 그리기
                    drawingContext.DrawImage(bitmap, new System.Windows.Rect(0, 0, image.Width, image.Height));

                    // 검출 결과
                    foreach (var obj in detectItems)
                    {
                        if (obj.Type != "person")
                            continue;

                        // 03. 사각형 그리기
                        drawingContext.DrawRectangle(new SolidColorBrush(Colors.Transparent),  // 배경색
                            new System.Windows.Media.Pen(new SolidColorBrush(Colors.Red), 2),  // 테두리
                            new System.Windows.Rect(obj.X, obj.Y, obj.Width, obj.Height));
                    }
                }

                // 04. 비트맵 이미지로 랜더링합니다.
                RenderTargetBitmap renderBitmap = new RenderTargetBitmap((int)image.Width, (int)image.Height, 96, 96, PixelFormats.Pbgra32);
                renderBitmap.Render(visual);

                // 05. BitmapImage로 변환하여 이미지를 표시합니다.
                BitmapImage bitmapImg = new BitmapImage();
                using (var stream = new MemoryStream())
                {
                    BitmapEncoder encoder = new PngBitmapEncoder();
                    encoder.Frames.Add(BitmapFrame.Create(renderBitmap));
                    encoder.Save(stream);

                    stream.Seek(0L, SeekOrigin.Begin);

                    bitmapImg.BeginInit();
                    bitmapImg.CacheOption = BitmapCacheOption.OnLoad;
                    bitmapImg.DecodePixelWidth = renderBitmap.PixelWidth;
                    bitmapImg.StreamSource = stream;
                    bitmapImg.EndInit();
                    bitmapImg.Freeze();
                }

                return bitmapImg;
            });
        }

        private WriteableBitmap? YOLODetect(byte[] imageByte, Mat? frameMat)
        {
            if (frameMat is null)
                return null;

            // 01. YOLO로 객체 검출
            var detectItems = _yoloWrapper!.Detect(imageByte);

            // 검출 결과
            foreach (var obj in detectItems)
            {
                if (obj.Type != "person")
                    continue;

                OpenCvSharp.Rect rect = new(obj.X, obj.Y, obj.Width, obj.Height);
                // 02. 사각형 그리기
                Cv2.Rectangle(frameMat, rect, Scalar.Red, 5);
            }

            // 03. 이미지 표시
            var bitmap = frameMat.ToWriteableBitmap();
            bitmap.Freeze();
            return bitmap;
        }

        private DrawingVisual CreateVisualForBitmap(string bitmapPath, System.Windows.Rect bounds, out DrawingContext dc)
        {
            var bitmap = new BitmapImage(new Uri(bitmapPath, uriKind: UriKind.Absolute));
            var visual = new DrawingVisual();

            DrawingContext drawingContext = visual.RenderOpen();
            drawingContext.DrawImage(bitmap, bounds);

            dc = drawingContext;

            return visual;
        }

        private byte[] ConvertBitmapToByteArray(BitmapImage bitmapImg)
        {
            byte[] bytes;
            JpegBitmapEncoder encoder = new JpegBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(bitmapImg));
            using (MemoryStream ms = new MemoryStream())
            {
                encoder.Save(ms);
                bytes = ms.ToArray();
            }
            return bytes;
        }

        private void InitCamera()
        {
            // 영상 해상도 설정
            _capture.Set(VideoCaptureProperties.FrameWidth, this.xImage.ActualWidth);
            _capture.Set(VideoCaptureProperties.FrameHeight, this.xImage.ActualHeight);
        }

        private void StartCamera()
        {
            _capture.Open(0, VideoCaptureAPIs.ANY);
            if (_capture.IsOpened() is false)
            {
                MessageBox.Show("카메라 Open 실패");
                return;
            }

            _videoCaptureCancelToken = new CancellationTokenSource();
            Task.Factory.StartNew(() =>
            {
                while (true)
                {
                    if (_videoCaptureCancelToken.Token.IsCancellationRequested is false)
                    {
                        using (var frameMat = _capture.RetrieveMat())
                        {
                            var bitmap = this.YOLODetect(frameMat.ToBytes(), frameMat);
                            if (bitmap is not null)
                            {
                                Dispatcher.Invoke(() =>
                                {
                                    this.xImage.Source = bitmap;
                                });
                            }
                        }
                    }
                    Thread.Sleep(30);
                }
            }, _videoCaptureCancelToken.Token, TaskCreationOptions.LongRunning, TaskScheduler.Default);
        }

        private void StopCamera()
        {
            _videoCaptureCancelToken.Cancel();
        }
    }
}
