using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;

using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;

using Microsoft.Win32;

using TextCollage.Annotations;
using TextCollage.Core;
using TextCollage.Core.Finalizing;
using TextCollage.Core.Matching;

using Point = System.Drawing.Point;

namespace TextCollage.UserControls
{
    /// <summary>
    ///     Interaktionslogik für FinalizeImageControl.xaml
    /// </summary>
    public sealed partial class FinalizeImageControl : INotifyPropertyChanged
    {
        #region Fields and Constants

        private double backgroundColorRed, backgroundColorGreen, backgroundColorBlue;
        private TimeSpanOptions timelineMode = TimeSpanOptions.Month;
        private bool useTimeline;
        private BitmapSource resultBitmapSource;
        private Image<Rgba, byte> resultImage;
        private readonly ManualResetEvent manualResetEvent = new ManualResetEvent(false);
        private bool isBackgroundTransparent;
        private string backgroundImagePath;
        private bool useBackgroundImage;

        #endregion

        #region Properties

        public Match[] Matches { get; private set; }
        public bool IsBackgroundTransparent
        {
            get { return isBackgroundTransparent; }
            set
            {
                if (value.Equals(isBackgroundTransparent))
                    return;
                isBackgroundTransparent = value;
                OnPropertyChanged();
                BackgroundChanged = true;
            }
        }
        public bool UseBackgroundImage
        {
            get { return useBackgroundImage; }
            set
            {
                if (value.Equals(useBackgroundImage))
                    return;
                useBackgroundImage = value;
                OnPropertyChanged();
                BackgroundChanged = true;
            }
        }
        public double BackgroundColorRed
        {
            get { return backgroundColorRed; }
            set
            {
                if (value == backgroundColorRed)
                    return;
                backgroundColorRed = value;
                OnPropertyChanged();
                BackgroundChanged = true;
            }
        }
        public double BackgroundColorGreen
        {
            get { return backgroundColorGreen; }
            set
            {
                if (value == backgroundColorGreen)
                    return;
                backgroundColorGreen = value;
                OnPropertyChanged();
                BackgroundChanged = true;
            }
        }
        public double BackgroundColorBlue
        {
            get { return backgroundColorBlue; }
            set
            {
                if (value == backgroundColorBlue)
                    return;
                backgroundColorBlue = value;
                OnPropertyChanged();
                BackgroundChanged = true;
            }
        }
        public string BackgroundImagePath
        {
            get { return backgroundImagePath; }
            set
            {
                if (value == backgroundImagePath)
                    return;
                backgroundImagePath = value;
                OnPropertyChanged();
                BackgroundChanged = true;
            }
        }
        public TimeSpanOptions TimelineMode
        {
            get { return timelineMode; }
            set
            {
                if (value == timelineMode)
                    return;
                timelineMode = value;
                OnPropertyChanged();
                InitSolutionTree();
            }
        }
        public bool UseTimeline
        {
            get { return useTimeline; }
            set
            {
                if (value.Equals(useTimeline))
                    return;
                useTimeline = value;
                OnPropertyChanged();
                InitSolutionTree();
            }
        }
        public BitmapSource ResultBitmapSource
        {
            get { return resultBitmapSource; }
            private set
            {
                if (Equals(value, resultBitmapSource))
                    return;
                resultBitmapSource = value;
                OnPropertyChanged();
            }
        }
        public Image<Rgba, Byte> ResultImage
        {
            get { return resultImage; }
            private set
            {
                if (Equals(value, resultImage))
                    return;
                resultImage = value;
                OnPropertyChanged();
            }
        }
        private bool BackgroundChanged { get; set; }
        private Bruteforce Bruteforce { get; set; }
        private bool IsOpened { get; set; }

        #endregion

        #region  Methods

        private void InitSolutionTree()
        {
            if (Bruteforce != null)
                Bruteforce.Stop();
            Bruteforce = new Bruteforce(Matches, new FinalizeSettings(UseTimeline, TimelineMode));
            Task.Run(() => StartComputingResults());
        }

        #endregion

        #region  Constructors

        public FinalizeImageControl(IEnumerable<Match> matches)
        {
            InitializeComponent();
            IsOpened = true;
            DataContext = this;
            Matches = matches.ToArray();
            Bruteforce = new Bruteforce(Matches.ToArray(), new FinalizeSettings(UseTimeline, TimelineMode));
            Task.Run(() => StartComputingResults());
            Task.Run(() => UpdatePreview());
        }

        private void UpdatePreview()
        {
            while (IsOpened)
            {
                Task.Delay(500).Wait();
                try
                {
                    while (Bruteforce == null || Bruteforce.BestMatches == null)
                        Task.Delay(50).Wait();
                    if (Bruteforce.NewBestMatch || BackgroundChanged)
                        lock (Bruteforce)
                        {
                            var previewImage = new Image<Rgba, byte>(Project.Instance.TextSettings.Width, Project.Instance.TextSettings.Height, new Rgba(IsBackgroundTransparent ? 0 : BackgroundColorRed, IsBackgroundTransparent ? 0 : BackgroundColorGreen, IsBackgroundTransparent ? 0 : BackgroundColorBlue, IsBackgroundTransparent ? 0 : 255));

                            if (UseBackgroundImage && BackgroundImagePath != null && File.Exists(BackgroundImagePath))
                            {
                                try
                                {
                                    var bgImage = new Image<Rgba, Byte>(BackgroundImagePath).Resize(previewImage.Width, previewImage.Height, INTER.CV_INTER_LINEAR);
                                    previewImage.Draw(bgImage, new Rectangle(new Point(0, 0), previewImage.Size), new Image<Gray, byte>(previewImage.Width, previewImage.Height, new Gray(255)));
                                }
                                catch
                                {
                                    // ignored
                                }
                            }

                            var bestDNS = Bruteforce.BestMatches;
                            for (int i = 0; i < bestDNS.Length; i++)
                            {
                                using (var resultImage = bestDNS[i].GetBestResultImage())
                                {
                                    using (var resized = resultImage.Resize(bestDNS[i].Character.Location.Width, bestDNS[i].Character.Location.Height, INTER.CV_INTER_LINEAR))
                                    {
                                        previewImage.ROI = bestDNS[i].Character.Location;
                                        resized.Copy(previewImage, bestDNS[i].Character.GetAlphaImage());
                                        previewImage.ROI = Rectangle.Empty;
                                    }
                                }
                            }
                            ResultImage = previewImage;
                            Project.Instance.UIScheduler.InvokeOnScheduler(() => { ResultBitmapSource = ResultImage.Bitmap.ConvertToBitmapSource(); });
                            Bruteforce.NewBestMatch = false;
                            BackgroundChanged = false;
                            //AlgorithmDataControl.UpdateContent(GeneticFinaliseMatching.Generations);
                        }
                }
                catch
                {
                }
            }
            // ReSharper disable once FunctionNeverReturns
        }

        private double Rate(Match[] dns)
        {
            //Overall fitting
            double rating = dns.Sum(t => t.NormalisiedFitness) + 1000;

            //Timeline
            if (UseTimeline)
            {
                TimeSpanOptions timeline = TimelineMode;
                DateTime currentTime = DateTime.MinValue;

                for (int i = 0; i < dns.Length; i++)
                {
                    bool respected = false;
                    FileInfo fi = new FileInfo(dns[i].Image.Path);
                    DateTime time = fi.CreationTime;

                    switch (timeline)
                    {
                        case TimeSpanOptions.Total:
                            respected = time >= currentTime;
                            break;
                        case TimeSpanOptions.Year:
                            respected = time.Year >= currentTime.Year || time >= currentTime;
                            break;
                        case TimeSpanOptions.Month:
                            respected = time.Month >= currentTime.Month || time.Year > currentTime.Year || time > currentTime;
                            break;
                        case TimeSpanOptions.Day:
                            respected = time.Day >= currentTime.Day || time.Month > currentTime.Month || time.Year > currentTime.Year || time >= currentTime;
                            break;
                        case TimeSpanOptions.Hour:
                            respected = time.Hour >= currentTime.Hour || time.Day > currentTime.Day || time.Month > currentTime.Month || time.Year > currentTime.Year || time >= currentTime;
                            break;
                        case TimeSpanOptions.Minute:
                            respected = time.Minute >= currentTime.Minute || time.Hour > currentTime.Hour || time.Day > currentTime.Day || time.Month > currentTime.Month || time.Year > currentTime.Year || time >= currentTime;
                            break;
                    }
                    currentTime = time;
                    if (!respected)
                        rating /= 4;
                }
            }

            //Color Temperature
            if (BackgroundColorRed > 0)
            {
                Rgba startColor = dns[0].Image.AverageColor;
                for (int i = 1; i < dns.Length; i++)
                {
                    Rgba currentColor = dns[i].Image.AverageColor;
                    double deltaR = currentColor.Red - startColor.Red;
                    double deltaG = currentColor.Green - startColor.Green;
                    double deltaB = currentColor.Blue - startColor.Blue;

                    if (deltaR > 0)
                        deltaR /= (10.0 / BackgroundColorRed);
                    else
                        deltaR = 0;
                    if (deltaG > 0)
                        deltaG /= (10.0 / BackgroundColorGreen);
                    else
                        deltaG = 0;
                    if (deltaB > 0)
                        deltaB /= (10.0 / BackgroundColorBlue);
                    else
                        deltaB = 0;

                    double distance = Math.Sqrt(deltaR * deltaR + deltaG * deltaG + deltaB * deltaB);
                    distance /= 60.0;
                    rating -= distance;
                    startColor = currentColor;
                }
            }


            return rating;
        }

        #endregion

        #region  Methods

        private void StartComputingResults()
        {
            while (Matches == null || !Matches.Any() || Bruteforce == null)
                Task.Delay(50).Wait();
            //while (IsOpened)
            //{
            try
            {
                ComputeResult();
            }
            catch
            {
            }
            //manualResetEvent.WaitOne();
            //}
            // ReSharper disable once FunctionNeverReturns
        }

        private void ComputeResult()
        {
            Bruteforce.ComputeNext();
        }

        [NotifyPropertyChangedInvocator]
        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null)
                Project.Instance.UIScheduler.InvokeOnScheduler(() => handler(this, new PropertyChangedEventArgs(propertyName)));
        }

        #endregion

        #region INotifyPropertyChanged Members

        public event PropertyChangedEventHandler PropertyChanged;

        #endregion

        private void FinalizeImageControl_OnUnloaded(object sender, RoutedEventArgs e)
        {
            IsOpened = false;
            Bruteforce.Stop();
        }

        private void SelectBackgroundImageButton_OnClick(object sender, RoutedEventArgs e)
        {
            var ofd = new OpenFileDialog { Filter = "All Supported Files (*.png, *.jpg, *.bmp)|*.png;*.jpg;*.bmp" };
            if (ofd.ShowDialog() == true)
            {
                BackgroundImagePath = ofd.FileName;
            }
        }
    }

    public enum TimeSpanOptions
    {
        Minute = 0,
        Hour = 1,
        Day = 2,
        Month = 4,
        Year = 5,
        Total = 6
    }
}