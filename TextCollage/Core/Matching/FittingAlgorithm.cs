using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Drawing;
using System.Runtime.CompilerServices;
using System.Windows.Media.Imaging;

using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;

namespace TextCollage.Core.Matching
{
    public abstract class FittingAlgorithm : INotifyPropertyChanged
    {
        #region Fields and Constants

        private IterationResult bestResult = new IterationResult(double.MinValue, Rectangle.Empty);
        private DateTime startCalculationTime;

        private readonly ObservableCollection<IterationResults> iterationResults =
            new ObservableCollection<IterationResults>();

        #endregion

        #region Properties

        public Match Match { get; private set; }
        public Image<Gray, Byte> CharacterAlphaImage { get; private set; }
        public ObservableCollection<IterationResults> IterationResults
        {
            get { return iterationResults; }
        }
        public IterationResult BestResult
        {
            get { return bestResult; }
            set
            {
                bestResult = value;
                //InvokePropertyChanged();
                //InvokePropertyChanged("BestResultBitmapSource");
            }
        }
        public int Iterations { get; private set; }
        public TimeSpan CalculationTime { get; private set; }
        public BitmapSource BestResultPreviewBitmapSource
        {
            get
            {
                Load(true);
                var imagePart = Match.Image.Image.Copy(BestResult.SourceRectangle);
                var destination = new Image<Rgba, byte>(imagePart.Width, imagePart.Height);
                imagePart.Copy(destination,
                    CharacterAlphaImage.Resize(imagePart.Size.Width, imagePart.Size.Height, INTER.CV_INTER_LINEAR));
                destination = destination.Resize(100, 100, INTER.CV_INTER_LINEAR, true);
                BitmapSource bs = destination.Bitmap.ConvertToBitmapSource();
                destination.Dispose();
                imagePart.Dispose();
                UnLoad();
                return bs;
            }
        }
        protected Image<Gray, Byte> ROIAlphaImage { get; private set; }

        #endregion

        #region  Constructors

        protected FittingAlgorithm(Match match)
        {
            Match = match;
        }

        #endregion

        #region  Methods

        public void InvokePropertyChanged([CallerMemberName] string name = "")
        {
            if (name == "")
                return;
            PropertyChangedEventHandler pc = PropertyChanged;
            if (pc != null)
                Project.Instance.UIScheduler.InvokeOnScheduler(() => pc(this, new PropertyChangedEventArgs(name)));
        }

        public void Load(bool excludeROI = false)
        {
            startCalculationTime = DateTime.Now;

            CharacterAlphaImage = Match.Character.GetAlphaImage();
            if (!excludeROI)
               /* Match.Image.ROIImage.CalculationScheduler.InvokeOnSchedulerAndWait(() => {*/ ROIAlphaImage = Match.Image.ROIImage.ROIAlphaImage.Clone(); //});
        }

        public abstract IterationResults NextIteration();

        public void UnLoad()
        {
            CharacterAlphaImage.Dispose();
            CharacterAlphaImage = null;
            if (ROIAlphaImage != null)
                ROIAlphaImage.Dispose();
            ROIAlphaImage = null;

            CalculationTime += DateTime.Now - startCalculationTime;
        }

        protected virtual void AddIterationResult(IterationResults results)
        {
            iterationResults.Add(results);
            if (results.BestResult.Rating > BestResult.Rating)
            {
                BestResult = results.BestResult;
            }
            Iterations++;
        }

        protected Image<Gray, Byte> GetXORImage(Rectangle source)
        {
            Image<Gray, Byte> result;
            using (var characterAlpha = CharacterAlphaImage.Resize(source.Width, source.Height, INTER.CV_INTER_LINEAR))
            {
                try
                {
                    lock (ROIAlphaImage)
                    {
                        ROIAlphaImage.ROI = source;
                        result = characterAlpha.Xor(ROIAlphaImage);
                        ROIAlphaImage.ROI = Rectangle.Empty;
                    }
                }
                catch
                {
                    return GetXORImage(source);
                }
            }

            return result;
        }

        protected double Rate(Rectangle source)
        {
            double fitness;
            using (var xorImage = GetXORImage(source))
            {
                //lock (ROIAlphaImage)
                //{
                ROIAlphaImage.ROI = Rectangle.Empty;

                using (var newRoiAlphaImage = ROIAlphaImage.Clone())
                {
                    newRoiAlphaImage.Draw(xorImage, source);
                    ROIAlphaImage.ROI = Rectangle.Empty;



                    fitness = xorImage.Width * xorImage.Height * 0.001 * (ROIAlphaImage.InRange(new Gray(255), new Gray(255)).CountNonzero()[0] * 1.0 - newRoiAlphaImage.InRange(new Gray(255), new Gray(255)).CountNonzero()[0]);

                    //double white = ROIAlphaImage.InRange(new Gray(255), new Gray(255)).CountNonzero()[0] - newRoiAlphaImage.InRange(new Gray(255), new Gray(255)).CountNonzero()[0];
                    //fitness = 1000.0 / (white + ROIAlphaImage.Width * ROIAlphaImage.Height);

                    //fitness = xorImage.Width * xorImage.Height * 100.0 / newRoiAlphaImage.InRange(new Gray(255), new Gray(255)).CountNonzero()[0];
                }
                //}
            }
            return fitness;
        }

        #endregion

        #region INotifyPropertyChanged Members

        public event PropertyChangedEventHandler PropertyChanged;

        #endregion
    }

    public struct IterationResult
    {
        #region Fields and Constants

        private readonly double rating;
        private readonly Rectangle sourceRectangle;

        #endregion

        #region Properties

        public Rectangle SourceRectangle
        {
            get { return sourceRectangle; }
        }
        public double Rating
        {
            get { return rating; }
        }

        #endregion

        #region  Constructors

        public IterationResult(double rating, Rectangle sourceRectangle)
        {
            this.rating = rating;
            this.sourceRectangle = sourceRectangle;
        }

        #endregion
    }

    public struct IterationResults
    {
        #region Fields and Constants

        private readonly IterationResult averageIterationResult;
        private readonly IterationResult bestIterationResult;
        private readonly IterationResult worstIterationResult;

        #endregion

        #region Properties

        public IterationResult BestResult
        {
            get { return bestIterationResult; }
        }
        public IterationResult WorstResult
        {
            get { return worstIterationResult; }
        }
        public IterationResult AverageResult
        {
            get { return averageIterationResult; }
        }

        #endregion

        #region  Constructors

        public IterationResults(IterationResult bestRating, IterationResult worstRating, IterationResult averageRating)
        {
            averageIterationResult = averageRating;
            bestIterationResult = bestRating;
            worstIterationResult = worstRating;
        }

        #endregion
    }

    public enum FittingAlgorithms
    {
        Linear,
        Random,
        Genetic
    }
}