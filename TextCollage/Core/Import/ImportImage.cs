using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows.Media.Imaging;

using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;

namespace TextCollage.Core.Import
{
    [Serializable]
    public class ImportImage : INotifyPropertyChanged, IDisposable
    {
        #region Fields and Constants

        private double blendingMovement = 0.5;
        private Image<Gray, Byte> originalGrayImage;
        private Image<Rgba, Byte> originalImage;

        #endregion

        #region Properties

        public string Path { get; private set; }
        public ROIImage ROIImage { get; private set; }
        public Rgba AverageColor { get; private set; }
        public Image<Rgba, Byte> Image
        {
            get
            {
                if (originalImage == null)
                {
                    var image = new Image<Rgba, byte>(Path);
                    double factor = (image.Width * image.Height) * 1.0 / Static.MaxImageResolution;
                    if (factor > 1)
                        image = image.Resize(1 / factor, INTER.CV_INTER_NN);
                    originalImage = image;
                }
                return originalImage;
            }
        }
        public Image<Gray, Byte> GrayImage
        {
            get { return originalGrayImage; }
            private set { originalGrayImage = value; }
        }
        public BitmapSource BitmapSource
        {
            get
            {
                try
                {
                    var image = Image;
                    if (image == null)
                        return null;
                    double tooLargeFactor = (image.Width * image.Height) * 1.0 / Static.MaxImageResolution;
                    if (tooLargeFactor >= 1)
                        image = image.Resize(1 / tooLargeFactor, INTER.CV_INTER_LINEAR);

                    return image.Bitmap.ConvertToBitmapSource();
                }
                catch
                {
                    return null;
                }
            }
        }
        public BitmapSource BlendBitmapSource
        {
            get
            {
                try
                {
                    var result = Image.AddWeighted(ROIImage.ROIAlphaImage.Convert<Rgba, Byte>(), 1 - BlendingMovement,
                        BlendingMovement, 0.0);

                    BitmapSource bs = result.Bitmap.ConvertToBitmapSource();

                    result.Dispose();

                    return bs;
                }
                catch
                {
                    return null;
                }
            }
        }
        public double BlendingMovement
        {
            get { return blendingMovement; }
            set
            {
                blendingMovement = value;
                InvokePropertyChanged();
                InvokePropertyChanged("BlendBitmapSource");
            }
        }
        public string FileName
        {
            get { return System.IO.Path.GetFileName(Path); }
        }

        #endregion

        #region  Constructors

        private ImportImage()
        {
        }

        #endregion

        #region  Methods

        public static ImportImage Create(string path)
        {
            if (!File.Exists(path))
                throw new ArgumentException("The file doesn't exist");

            ImportImage result = new ImportImage {Path = path};
            var image = new Image<Rgba, byte>(path);

            var resizedImage = image.Resize(Math.Min(1920, image.Width), Math.Min(1080, image.Height),
                INTER.CV_INTER_LINEAR, true);

            //if (maxSize.Height < image.Height)
            //{
            //    int newWidth = (int)(maxSize.Height * aspectRatio);
            //    resizedImage = image.Resize(newWidth, maxSize.Height, INTER.CV_INTER_LINEAR);
            //}
            image.Dispose();
            result.originalImage = resizedImage;
            result.originalGrayImage = resizedImage.Convert<Gray, Byte>();
            GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced);
            
            result.ROIImage = new ROIImage(result);
            result.ROIImage.PropertyChanged += (o, e) => { result.InvokePropertyChanged("BlendBitmapSource"); };
            result.AverageColor = result.originalImage.GetAverage();
            return result;
        }

        private void InvokePropertyChanged([CallerMemberName] string name = "null")
        {
            if (name == "null")
                return;
            PropertyChangedEventHandler pc = PropertyChanged;
            if (pc != null)
            {
                pc(this, new PropertyChangedEventArgs(name));
            }
        }

        #endregion

        #region IDisposable Members

        public void Dispose()
        {
            if (originalImage != null)
            {
                originalImage.Dispose();
                originalImage = null;
            }
            if (originalGrayImage != null)
            {
                originalGrayImage.Dispose();
                originalGrayImage = null;
            }
        }

        #endregion

        #region INotifyPropertyChanged Members

        public event PropertyChangedEventHandler PropertyChanged;

        #endregion
    }
}