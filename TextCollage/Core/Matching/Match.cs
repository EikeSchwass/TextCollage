using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Media.Imaging;

using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;

using TextCollage.Core.Import;
using TextCollage.Core.Text;

namespace TextCollage.Core.Matching
{
    public class Match : INotifyPropertyChanged
    {
        #region Fields and Constants

        private FittingAlgorithm fittingAlgorithm;
        private double normalisiedFitness;
        private bool enabled;
        private BitmapSource previewBestResult;

        #endregion

        #region Properties

        public Character Character { get; private set; }
        public ImportImage Image { get; private set; }
        public FittingAlgorithm FittingAlgorithm
        {
            get { return fittingAlgorithm; }
            set
            {
                fittingAlgorithm = value;
                InvokePropertyChanged();
            }
        }
        public double NormalisiedFitness
        {
            get { return normalisiedFitness; }
            set
            {
                normalisiedFitness = value;
                InvokePropertyChanged();
            }
        }
        public bool Enabled
        {
            get { return enabled; }
            set
            {
                enabled = value;
                InvokePropertyChanged();
            }
        }
        public BitmapSource PreviewBestResult
        {
            get { return previewBestResult; }
            set { previewBestResult = value; InvokePropertyChanged(); }
        }

        #endregion

        #region  Constructors

        public Match(Character character, ImportImage image)
        {
            Character = character;
            Image = image;
            Enabled = true;
        }

        #endregion

        #region  Methods

        public Image<Rgba, Byte> GetBestResultImage()
        {
            FittingAlgorithm.Load();

            var imagePart = Image.Image.Copy(FittingAlgorithm.BestResult.SourceRectangle);
            var destination = new Image<Rgba, byte>(imagePart.Width, imagePart.Height);
            imagePart.Copy(destination,
                FittingAlgorithm.CharacterAlphaImage.Resize(imagePart.Size.Width, imagePart.Size.Height,
                    INTER.CV_INTER_LINEAR));
            imagePart.Dispose();

            FittingAlgorithm.UnLoad();
            return destination;
        }

        private void InvokePropertyChanged([CallerMemberName] string name = "")
        {
            if (name == "")
                return;
            PropertyChangedEventHandler pc = PropertyChanged;
            if (pc != null)
                pc(this, new PropertyChangedEventArgs(name));
        }

        #endregion

        #region INotifyPropertyChanged Members

        public event PropertyChangedEventHandler PropertyChanged;

        #endregion
    }
}