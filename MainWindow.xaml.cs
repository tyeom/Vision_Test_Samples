using System.ComponentModel;
using System.Threading;
using System.Windows;
using Microsoft.Win32;
using OpenCvSharp;
using OpenCvSharp.WpfExtensions;

namespace VideoCaptureWPF
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : System.Windows.Window
    {
        private const string _OPENCV_FRONTAL_FACE_DATA_ = "Data/haarcascade_frontalface_default.xml";
        private const string _OPENCV_PROFILE_FACE_DATA_ = "Data/haarcascade_profileface.xml";
        private const string _OPENCV_FULL_BODY_DATA_ = "Data/haarcascade_fullbody.xml";
        private const string _OPENCV_UPPER_BODY_DATA_ = "Data/haarcascade_upperbody.xml";

        private readonly VideoCapture _capture;

        /// <summary>
        /// 얼굴 정면
        /// </summary>
        private readonly CascadeClassifier _cascadeClassifierByFrontalFace;

        /// <summary>
        /// 얼굴 측면
        /// </summary>
        private readonly CascadeClassifier _cascadeClassifierByProfileFace;

        /// <summary>
        /// 전체
        /// </summary>
        private readonly CascadeClassifier _cascadeClassifierByFullBody;

        /// <summary>
        /// 상반신
        /// </summary>
        private readonly CascadeClassifier _cascadeClassifierByUpperBody;

        // HOG 디텍터
        HOGDescriptor _hog;

        private readonly BackgroundWorker _bkgWorker;

        public MainWindow()
        {
            InitializeComponent();

            string faceDataPath = $"{System.AppDomain.CurrentDomain.BaseDirectory}{_OPENCV_FRONTAL_FACE_DATA_}";
            string profileFaceDataPath = $"{System.AppDomain.CurrentDomain.BaseDirectory}{_OPENCV_PROFILE_FACE_DATA_}";
            string fullBodyDataPath = $"{System.AppDomain.CurrentDomain.BaseDirectory}{_OPENCV_FULL_BODY_DATA_}";
            string upperBody = $"{System.AppDomain.CurrentDomain.BaseDirectory}{_OPENCV_UPPER_BODY_DATA_}";

            _capture = new VideoCapture();

            //_cascadeClassifierByFrontalFace = new CascadeClassifier(faceDataPath);
            //_cascadeClassifierByProfileFace = new CascadeClassifier(profileFaceDataPath);
            //_cascadeClassifierByFullBody = new CascadeClassifier(fullBodyDataPath);
            //_cascadeClassifierByUpperBody = new CascadeClassifier(upperBody);

            _hog = new();
            _hog.SetSVMDetector(HOGDescriptor.GetDefaultPeopleDetector());

            _bkgWorker = new BackgroundWorker { WorkerSupportsCancellation = true };
            _bkgWorker.DoWork += this.Worker_DoWork;

            this.Loaded += this.MainWindow_Loaded;
            this.Closing += this.MainWindow_Closing;
        }

        private void MainWindow_Loaded(object sender, System.Windows.RoutedEventArgs e)
        {
            // 영상 해상도 설정
            _capture.Set(VideoCaptureProperties.FrameWidth, this.xFrameImage.ActualWidth);
            _capture.Set(VideoCaptureProperties.FrameHeight, this.xFrameImage.ActualHeight);
        }

        private void MainWindow_Closing(object sender, CancelEventArgs e)
        {
            _bkgWorker.CancelAsync();

            _capture.Dispose();


            //_cascadeClassifierByFrontalFace.Dispose();
            //_cascadeClassifierByProfileFace.Dispose();
            //_cascadeClassifierByFullBody.Dispose();
            //_cascadeClassifierByUpperBody.Dispose();
        }

        private void Worker_DoWork(object sender, DoWorkEventArgs e)
        {
            var worker = (BackgroundWorker)sender;
            while (!worker.CancellationPending)
            {
                using (var frameMat = _capture.RetrieveMat())
                {
                    // 이미지에서 사람 감지
                    // 기본 HOG 알고리즘 사용
                    var peopleDetected = _hog.DetectMultiScale(frameMat);

                    foreach (var rect in peopleDetected)
                    {
                        Cv2.Rectangle(frameMat, rect, Scalar.Blue, 5);
                    }

                    //var rectsByFrontalFace = _cascadeClassifierByFrontalFace.DetectMultiScale(frameMat, 1.3, 5, HaarDetectionTypes.ScaleImage, new OpenCvSharp.Size(30, 30));
                    //var rectsByProfileFace = _cascadeClassifierByProfileFace.DetectMultiScale(frameMat, 1.3, 5, HaarDetectionTypes.ScaleImage, new OpenCvSharp.Size(30, 30));
                    //var rectsByFullBody = _cascadeClassifierByFullBody.DetectMultiScale(frameMat, 1.3, 5, HaarDetectionTypes.ScaleImage, new OpenCvSharp.Size(70, 70));
                    //var rectsByUpperBody = _cascadeClassifierByUpperBody.DetectMultiScale(frameMat, 1.3, 5, HaarDetectionTypes.ScaleImage, new OpenCvSharp.Size(50, 50));

                    // 정면 얼굴
                    //foreach (var rect in rectsByFrontalFace)
                    //{
                    //    Cv2.Rectangle(frameMat, rect, Scalar.Red, 5);
                    //}

                    // 측면 얼굴
                    //foreach (var rect in rectsByProfileFace)
                    //{
                    //    Cv2.Rectangle(frameMat, rect, Scalar.Red, 5);
                    //}

                    // 전체 몸
                    //foreach (var rect in rectsByFullBody)
                    //{
                    //    Cv2.Rectangle(frameMat, rect, Scalar.Green, 5);
                    //}

                    // 상반신
                    //foreach (var rect in rectsByUpperBody)
                    //{
                    //    Cv2.Rectangle(frameMat, rect, Scalar.Blue, 5);
                    //}

                    Dispatcher.Invoke(() =>
                    {
                        this.xFrameImage.Source = frameMat.ToWriteableBitmap();
                    });
                }

                Thread.Sleep(30);
            }
        }

        private void xStartCamera_Click(object sender, RoutedEventArgs e)
        {
            _capture.Open(0, VideoCaptureAPIs.ANY);
            if (_capture.IsOpened() is false)
            {
                MessageBox.Show("카메라 Open 실패");
                return;
            }

            _bkgWorker.RunWorkerAsync();
        }

        private void xOpenImage1_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            if (openFileDialog.ShowDialog() == true)
            {
                // 이미지를 Mat 형식으로 로드
                Mat image = new Mat(openFileDialog.FileName, ImreadModes.Color);

                // 이미지에서 사람 감지
                // 기본 HOG 알고리즘 사용
                var peopleDetected = _hog.DetectMultiScale(image);

                foreach (var rect in peopleDetected)
                {
                    Cv2.Rectangle(image, rect, Scalar.Blue, 5);
                }

                this.xFrameImage.Source = image.ToWriteableBitmap();
                image.Dispose();
            }
        }

        private void xOpenImage2_Click(object sender, RoutedEventArgs e)
        {
            string fullBodyDataPath = $"{System.AppDomain.CurrentDomain.BaseDirectory}{_OPENCV_FULL_BODY_DATA_}";
            CascadeClassifier cascadeClassifierByFullBody = new CascadeClassifier(fullBodyDataPath);

            OpenFileDialog openFileDialog = new OpenFileDialog();
            if (openFileDialog.ShowDialog() == true)
            {
                // 이미지를 Mat 형식으로 로드
                Mat image = new Mat(openFileDialog.FileName, ImreadModes.Color);

                var rectsByFullBody = cascadeClassifierByFullBody.DetectMultiScale(image, 1.3, 5, HaarDetectionTypes.ScaleImage, new OpenCvSharp.Size(70, 70));

                // 전체 몸
                foreach (var rect in rectsByFullBody)
                {
                    Cv2.Rectangle(image, rect, Scalar.Green, 5);
                }

                this.xFrameImage.Source = image.ToWriteableBitmap();
                image.Dispose();
            }

            cascadeClassifierByFullBody.Dispose();
        }
    }
}
