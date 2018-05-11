using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;

using Emgu.CV;
using Emgu.CV.Structure;

using TextCollage.Annotations;
using TextCollage.Core.Matching;

using Image = System.Windows.Controls.Image;
using Point = System.Windows.Point;

namespace TextCollage
{
    /// <summary>
    ///     Interaktionslogik für EditMatchDialog.xaml
    /// </summary>
    public partial class EditMatchDialog : Window, INotifyPropertyChanged
    {
        #region Fields and Constants

        private BitmapSource totalImage;

        #endregion

        #region Properties

        public Match Match { get; private set; }
        public BitmapSource TotalImage
        {
            get { return totalImage; }
            private set
            {
                if (Equals(value, totalImage))
                    return;
                totalImage = value;
                OnPropertyChanged();
            }
        }

        private bool Dragging { get; set; }
        private Point LastPosition { get; set; }

        #endregion

        #region  Constructors

        public EditMatchDialog(Match match)
        {
            InitializeComponent();
            Match = match;
            DataContext = this;
            CalculateTotalImage();
        }

        #endregion

        #region  Methods

        private void CalculateTotalImage()
        {
            using (var resultImage = Match.GetBestResultImage())
            {
                var image = Match.Image.Image.Clone().Convert<Gray, Byte>().Convert<Rgba, Byte>();

                Rectangle r = Match.FittingAlgorithm.BestResult.SourceRectangle;

                image.Draw(resultImage, r, new Image<Gray, byte>(image.Width, image.Height, new Gray(255)));

                TotalImage = image.Bitmap.ConvertToBitmapSource();
                Match.PreviewBestResult = Match.GetBestResultImage().Bitmap.ConvertToBitmapSource();
            }

        }

        [NotifyPropertyChangedInvocator]
        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null)
                handler(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion

        #region INotifyPropertyChanged Members

        public event PropertyChangedEventHandler PropertyChanged;

        #endregion

        private void Image_OnMouseMove(object sender, MouseEventArgs e)
        {
            var image = sender as Image;
            Debug.Assert(image != null);

            var pos = e.GetPosition(image);

            if (e.LeftButton == MouseButtonState.Released)
                Dragging = false;

            if (!Dragging)
                return;

            var deltaPos = pos - LastPosition;
            double deltaX = deltaPos.X * Match.Image.Image.Width / image.ActualWidth;
            double deltaY = deltaPos.Y * Match.Image.Image.Height / image.ActualHeight;
            LastPosition = pos;


            var bestResult = Match.FittingAlgorithm.BestResult;

            double newX = bestResult.SourceRectangle.X + deltaX;
            double newY = bestResult.SourceRectangle.Y + deltaY;

            if (newX < 0 || newX > Match.Image.Image.Width - bestResult.SourceRectangle.Width)
            {

            }
            if (newY < 0 || newY > Match.Image.Image.Height - bestResult.SourceRectangle.Height)
            {

            }

            newX = (int)Math.Max(0, Math.Min(Match.Image.Image.Width - bestResult.SourceRectangle.Width, newX));
            newY = (int)Math.Max(0, Math.Min(Match.Image.Image.Height - bestResult.SourceRectangle.Height, newY));


            Match.FittingAlgorithm.BestResult = new IterationResult(bestResult.Rating, new Rectangle((int)newX, (int)newY, bestResult.SourceRectangle.Width, bestResult.SourceRectangle.Height));
            CalculateTotalImage();
        }

        private void Image_OnMouseDown(object sender, MouseButtonEventArgs e)
        {
            Image image = sender as Image;
            if (image != null)
                image.CaptureMouse();
            LastPosition = e.GetPosition(image);
            Dragging = true;
        }

        private void Image_OnMouseUp(object sender, MouseButtonEventArgs e)
        {
            Dragging = false;
            Image image = sender as Image;
            if (image != null)
                image.ReleaseMouseCapture();
        }

        private void Image_OnMouseWheel(object sender, MouseWheelEventArgs e)
        {
            var bestResult = Match.FittingAlgorithm.BestResult;

            double ratio = bestResult.SourceRectangle.Width * 1.0 / bestResult.SourceRectangle.Height;

            int newWidth = (int)(bestResult.SourceRectangle.Width - e.Delta * ratio / 10);
            int newHeight = (int)(bestResult.SourceRectangle.Height - e.Delta * 1.0 / 10);

            if (newWidth < 50 || newWidth < 50 || bestResult.SourceRectangle.X + newWidth > Match.Image.Image.Width || bestResult.SourceRectangle.Y + newHeight > Match.Image.Image.Height)
                return;

            Match.FittingAlgorithm.BestResult = new IterationResult(bestResult.Rating, new Rectangle(bestResult.SourceRectangle.X, bestResult.SourceRectangle.Y, newWidth, newHeight));
            CalculateTotalImage();

        }
    }
}