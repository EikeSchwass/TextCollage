using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;

using TextCollage.Annotations;

namespace TextCollage.Core.Import
{
    public class ROIImage : INotifyPropertyChanged, IDisposable
    {
        #region Fields and Constants

        private double hueEdgeThreshold = 80, rgbEdgeThreshold = 125, faceDetectionScaleFactor = 1.45;
        private int faceDetectionMinNeighbors = 5;

        #endregion

        #region Properties

        public TaskScheduler CalculationScheduler { get; private set; }
        public Image<Gray, Byte> UserROI { get; private set; }
        public Image<Gray, Byte> ROIAlphaImage { get; private set; }
        public double HueEdgeThreshold
        {
            get { return hueEdgeThreshold; }
            set
            {
                hueEdgeThreshold = value;
                InvokePropertyChanged();
                PropertyChangedAutoResetEvent.Set();
            }
        }
        public double RGBEdgeThreshold
        {
            get { return rgbEdgeThreshold; }
            set
            {
                rgbEdgeThreshold = value;
                InvokePropertyChanged();
                PropertyChangedAutoResetEvent.Set();
            }
        }
        public double FaceDetectionScaleFactor
        {
            get { return faceDetectionScaleFactor; }
            set
            {
                faceDetectionScaleFactor = value;
                InvokePropertyChanged();
                PropertyChangedAutoResetEvent.Set();
            }
        }
        public int FaceDetectionMinNeighbors
        {
            get { return faceDetectionMinNeighbors; }
            set
            {
                faceDetectionMinNeighbors = value;
                InvokePropertyChanged();
                PropertyChangedAutoResetEvent.Set();
            }
        }
        public bool IsHueEdgesEnabled
        {
            get { return isHueEdgesEnabled; }
            set
            {
                if (value.Equals(isHueEdgesEnabled))
                    return;
                isHueEdgesEnabled = value;
                InvokePropertyChanged();
                PropertyChangedAutoResetEvent.Set();
            }
        }
        public bool IsRGBEdgesEnabled
        {
            get { return isRGBEdgesEnabled; }
            set
            {
                if (value.Equals(isRGBEdgesEnabled))
                    return;
                isRGBEdgesEnabled = value;
                InvokePropertyChanged();
                PropertyChangedAutoResetEvent.Set();
            }
        }
        public bool IsFacedetectionEnabled
        {
            get { return isFacedetectionEnabled; }
            set
            {
                if (value.Equals(isFacedetectionEnabled))
                    return;
                isFacedetectionEnabled = value;
                InvokePropertyChanged();
                PropertyChangedAutoResetEvent.Set();
            }
        }
        public ComposingModes ComposingMode
        {
            get { return composingMode; }
            set
            {
                if (value == composingMode)
                    return;
                composingMode = value;
                InvokePropertyChanged();
                PropertyChangedAutoResetEvent.Set();
            }
        }
        public BitmapSource ROIAlphaImageBitmapSource
        {
            get
            {
                try
                {
                    return ROIAlphaImage.Bitmap.ConvertToBitmapSource();
                }
                catch
                {
                    return null;
                }
            }
        }
        private bool Disposed { get; set; }
        private ImportImage ImportImage { get; set; }
        private AutoResetEvent PropertyChangedAutoResetEvent { get; set; }
        private CascadeClassifier FaceDetection { get; set; }

        #endregion

        public readonly List<Point> UserROIPoints = new List<Point>();
        private bool isHueEdgesEnabled = true;
        private bool isRGBEdgesEnabled = true;
        private bool isFacedetectionEnabled = false;
        private ComposingModes composingMode = ComposingModes.Or;

        #region  Constructors

        public ROIImage(ImportImage image)
        {
            FaceDetection = new CascadeClassifier("haarcascade_frontalface_default.xml");
            PropertyChangedAutoResetEvent = new AutoResetEvent(false);
            ImportImage = image;
            UserROI = new Image<Gray, Byte>(image.Image.Width, image.Image.Height, new Gray(0));
            CalculateROI();
            Task.Run(() => RunCalculationTask());
        }

        #endregion

        #region  Methods

        public void CalculateROI()
        {
            ROIAlphaImage = new Image<Gray, byte>(ImportImage.Image.Cols, ImportImage.Image.Rows, new Gray(0));

            Image<Gray, Byte> hueCannyImage = null;
            Image<Gray, Byte> rgbCannyImage = null;

            if (IsHueEdgesEnabled)
            {
                hueCannyImage = ImportImage.Image.Convert<Hls, Byte>()[0].Canny(HueEdgeThreshold, HueEdgeThreshold);
                var hueValueCannyImage = ImportImage.Image.Convert<Hls, Byte>()[2].Canny(50, 50);
                //hueCannyImage = hueValueCannyImage.And(hueCannyImage);
            }
            if (IsRGBEdgesEnabled)
                rgbCannyImage = ImportImage.Image.Canny(RGBEdgeThreshold, RGBEdgeThreshold);

            if (IsHueEdgesEnabled && IsRGBEdgesEnabled && rgbCannyImage != null && hueCannyImage != null)
            {
                if (ComposingMode == ComposingModes.And)
                    ROIAlphaImage = rgbCannyImage.And(hueCannyImage);
                else if (ComposingMode == ComposingModes.Or)
                    ROIAlphaImage = rgbCannyImage.Or(hueCannyImage);
            }
            else if (IsHueEdgesEnabled && !IsRGBEdgesEnabled && hueCannyImage != null)
            {
                ROIAlphaImage = hueCannyImage;
            }
            else if (!IsHueEdgesEnabled && IsRGBEdgesEnabled && rgbCannyImage != null)
            {
                ROIAlphaImage = rgbCannyImage;
            }

            if (IsFacedetectionEnabled)
            {
                var faces = FaceDetection.DetectMultiScale(ImportImage.GrayImage, FaceDetectionScaleFactor,
                    FaceDetectionMinNeighbors, new Size(25, 25), ImportImage.GrayImage.Size);

                foreach (Rectangle face in faces)
                {
                    Rectangle faceRectangle = new Rectangle(face.X + face.Width / 10, face.Y + face.Height / 10,
                        (int)(face.Width / 5.0 * 4), (int)(face.Height / 10.0 * 9));
                    using (
                        var mask = new Image<Gray, Byte>("facemask.jpg").Resize(faceRectangle.Width, faceRectangle.Height,
                            INTER.CV_INTER_LINEAR))
                    {
                        ROIAlphaImage.Draw(mask, faceRectangle);
                    }
                }

            }

            for (int i = 0; i < UserROIPoints.Count; i++)
            {
                ROIAlphaImage.Draw(new CircleF(new PointF(UserROIPoints[i].X - 2, UserROIPoints[i].Y - 2), 4), new Gray(255), 0);
            }

            InvokePropertyChanged("ROIAlphaImageBitmapSource");
        }

        [NotifyPropertyChangedInvocator]
        public void InvokePropertyChanged([CallerMemberName] string name = "null")
        {
            Task t = new Task(() =>
            {
                if (name == "null")
                    return;
                PropertyChangedEventHandler pc = PropertyChanged;
                if (pc != null)
                {
                    pc(this, new PropertyChangedEventArgs(name));
                }
            });
            t.Start(Project.Instance.UIScheduler);
        }

        public void ResetCustomROI()
        {
            UserROI = new Image<Gray, Byte>(UserROI.Width, UserROI.Height, new Gray(0));
            UserROIPoints.Clear();
            PropertyChangedAutoResetEvent.Set();
        }

        public void UpdateROI()
        {
            PropertyChangedAutoResetEvent.Set();
        }

        private void RunCalculationTask()
        {
            SynchronizationContext.SetSynchronizationContext(new SynchronizationContext());
            CalculationScheduler = TaskScheduler.FromCurrentSynchronizationContext();
            while (!Disposed)
            {
                PropertyChangedAutoResetEvent.WaitOne();
                CalculateROI();
                Task.Delay(150).Wait();
            }
        }

        #endregion

        #region IDisposable Members

        public void Dispose()
        {
            if (ROIAlphaImage != null)
                ROIAlphaImage.Dispose();
            Disposed = true;
        }

        #endregion

        #region INotifyPropertyChanged Members

        public event PropertyChangedEventHandler PropertyChanged;

        #endregion
    }

    public enum ComposingModes
    {
        And,
        Or
    }
}