using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

using Emgu.CV.CvEnum;

using TextCollage.Core;
using TextCollage.Core.Import;
using TextCollage.Core.Matching;
using TextCollage.Core.Text;

namespace TextCollage.UserControls
{
    /// <summary>
    ///     Interaktionslogik für FitnessEvaluationControl.xaml
    /// </summary>
    public partial class FitnessEvaluationControl : ITCControl, INotifyPropertyChanged
    {
        #region Fields and Constants

        private AlgorithmSettings algorithmSettings;
        private double calculationProgress;
        private FittingAlgorithms fittingAlgorithm;
        private bool isCalculating;
        private int iterationsPerMatch = 325;
        private int maxDegreeOfParallelism = Environment.ProcessorCount;
        private string progressDescription;
        private int timePerMatch = 100000;
        private readonly ObservableCollection<Match> matches = new ObservableCollection<Match>();

        #endregion

        #region Properties

        public ObservableCollection<Match> Matches
        {
            get { return matches; }
        }
        public int MaxDegreeOfParallelism
        {
            get { return maxDegreeOfParallelism; }
            set
            {
                if (value <= 0 || value > 32)
                    throw new ValidationException("The value has to be between 0 and 32");
                maxDegreeOfParallelism = value;
                InvokePropertyChanged();
            }
        }
        public string ProgressDescription
        {
            get { return progressDescription; }
            private set
            {
                progressDescription = value;
                InvokePropertyChanged();
            }
        }
        public double CalculationProgress
        {
            get { return calculationProgress; }
            set
            {
                calculationProgress = value;
                InvokePropertyChanged();
            }
        }
        public int TimePerMatch
        {
            get { return timePerMatch; }
            set
            {
                if (value < 10)
                    throw new ValidationException("The value has to be greater than 10");
                timePerMatch = value;
                InvokePropertyChanged();
            }
        }
        public int IterationsPerMatch
        {
            get { return iterationsPerMatch; }
            set
            {
                if (value < 10)
                    throw new ValidationException("The value has to be greater than 10");
                iterationsPerMatch = value;
                InvokePropertyChanged();
            }
        }
        public FittingAlgorithms FittingAlgorithm
        {
            get { return fittingAlgorithm; }
            set
            {
                switch (value)
                {
                    case FittingAlgorithms.Linear:
                        AlgorithmSettings = new LinearAlgorithmSettings();
                        break;
                    case FittingAlgorithms.Genetic:
                        AlgorithmSettings = new GeneticAlgorithmSettings();
                        break;
                    case FittingAlgorithms.Random:
                        AlgorithmSettings = new RandomAlgorithmSettings();
                        break;
                }
                fittingAlgorithm = value;
                InvokePropertyChanged();
            }
        }
        public AlgorithmSettings AlgorithmSettings
        {
            get { return algorithmSettings; }
            set
            {
                algorithmSettings = value;
                InvokePropertyChanged();
            }
        }
        public bool IsCalculating
        {
            get { return isCalculating; }
            private set
            {
                isCalculating = value;
                InvokePropertyChanged();
                if (CanLoseFocusChanged != null)
                {
                    CanLoseFocusChanged(this, !value);
                }
            }
        }
        private bool IsStopRequested { get; set; }

        #endregion

        #region  Constructors

        public FitnessEvaluationControl()
        {
            InitializeComponent();
            DataContext = this;
            FittingAlgorithm = FittingAlgorithms.Linear;
        }

        #endregion

        #region  Methods

        private void CalculateBestOverlap()
        {
            SetProgress(0, "Calculating Best Overlap...");
            double deltaProgress = 1.0 / matches.Count * 100;
            int counter = 0;

            Parallel.For(0, matches.Count, new ParallelOptions { MaxDegreeOfParallelism = MaxDegreeOfParallelism }, j =>
            {
                if (j >= matches.Count)
                    return;
                Match match = matches[j];
                double calcTime = TimePerMatch;
                Stopwatch timer = new Stopwatch();
                timer.Start();

                match.FittingAlgorithm.Load();
                int iterations = 0;
                while ((timer.ElapsedMilliseconds <= calcTime || iterations == 0) && iterations < IterationsPerMatch && !IsStopRequested)
                {
                    match.FittingAlgorithm.NextIteration();
                    iterations++;
                }
                timer.Stop();
                match.FittingAlgorithm.UnLoad();

                SetProgress(deltaProgress * counter,
                    string.Format("Calculating Best Overlap ({0}/{1})", ++counter, matches.Count));
            });

            SetProgress(100, string.Format("Generating Report..."));
        }

        private void InvokePropertyChanged([CallerMemberName] string name = "")
        {
            if (name == "")
                return;
            PropertyChangedEventHandler pc = PropertyChanged;
            if (pc != null)
                pc(this, new PropertyChangedEventArgs(name));
        }

        private void previewButton_Click(object sender, RoutedEventArgs e)
        {
            FinalizeImageDialog fid = new FinalizeImageDialog(Matches);
            fid.ShowDialog();
        }

        private void ReportResults()
        {
            //foreach (var matchesInGroup in (from m in matches
            //                                group m by m.Character.Index))
            //{
            //    double ratingSum = matchesInGroup.Sum(m => m.FittingAlgorithm.BestResult.Rating);
            //    foreach (Match match in matchesInGroup)
            //    {
            //        match.NormalisiedFitness = match.FittingAlgorithm.BestResult.Rating / ratingSum;
            //    }
            //}
            foreach (var matchesInGroup in (from m in matches
                                            group m by m.Image))
            {
                double ratingSum = matchesInGroup.Sum(m => m.FittingAlgorithm.BestResult.Rating);
                foreach (Match match in matchesInGroup)
                {
                    match.NormalisiedFitness = match.FittingAlgorithm.BestResult.Rating / ratingSum;
                    Project.Instance.UIScheduler.InvokeOnScheduler(() =>
                    {
                        match.PreviewBestResult = match.GetBestResultImage().Resize(125, 180, INTER.CV_INTER_LINEAR, true).Bitmap.ConvertToBitmapSource();
                    });
                }
            }
            for (int i = 0; i < matches.Count; i++)
            {
                Match match = matches[i];
                //match.FittingAlgorithm.InvokePropertyChanged("BestResultBitmapSource");
                match.FittingAlgorithm.InvokePropertyChanged("BestResult");
                match.FittingAlgorithm.InvokePropertyChanged("Iterations");
                match.FittingAlgorithm.InvokePropertyChanged("CalculationTime");
                match.FittingAlgorithm.InvokePropertyChanged("BestResultPreviewBitmapSource");
            }
        }

        private void SetProgress(double value, string description)
        {
            Task t = new Task(() =>
            {
                CalculationProgress = value;
                ProgressDescription = description;
            });
            t.Start(Project.Instance.UIScheduler);
        }

        private void startCalculationsButton_Click(object sender, RoutedEventArgs e)
        {
            if (Project.Instance.TextSettings.CharacterCollection.Count == 0 ||
                Project.Instance.ImportImageSettings.ImportImageCollection.Count == 0)
            {
                MessageBox.Show(
                    "You need to have at least one character set up and one image imported, otherwise there is nothing to calculate",
                    "Invalid Settings", MessageBoxButton.OK, MessageBoxImage.Exclamation);
            }
            IsStopRequested = false;
            IsCalculating = true;
            SetProgress(0, "Creating Matches...");
            Task createMatchesTask = new Task(() =>
            {
                if (matches.Count > 0)
                {
                    Project.Instance.UIScheduler.InvokeOnSchedulerAndWait(() =>
                    {
                        if (
                            MessageBox.Show(
                                "Do you want to restart calculation?\nYes for restarting\nNo to continue the previous calculations",
                                "Restart Calculation", MessageBoxButton.YesNo, MessageBoxImage.Question) ==
                            MessageBoxResult.Yes)
                        {
                            matches.Clear();
                        }
                    });
                }
                if (matches.Count > 0)
                    return;
                ImportImageCollection images = Project.Instance.ImportImageSettings.ImportImageCollection;
                CharacterCollection characters = Project.Instance.TextSettings.CharacterCollection;

                double totalMatchesCount = images.Count * characters.Count;

                foreach (ImportImage image in images)
                {
                    foreach (Character character in characters)
                    {
                        Match m = new Match(character, image);
                        FittingAlgorithm fa = null;
                        switch (FittingAlgorithm)
                        {
                            case FittingAlgorithms.Linear:
                                fa = new LinearAlgorithm(m, AlgorithmSettings as LinearAlgorithmSettings);
                                break;
                            case FittingAlgorithms.Genetic:
                                fa = new GeneticAlgorithm(m, AlgorithmSettings as GeneticAlgorithmSettings);
                                break;
                            case FittingAlgorithms.Random:
                                fa = new RandomAlgorithm(m);
                                break;
                        }
                        m.FittingAlgorithm = fa;
                        Project.Instance.UIScheduler.InvokeOnSchedulerAndWait(() => matches.Add(m));
                        double progress = matches.Count / totalMatchesCount * 100.0;
                        SetProgress(progress, "Creating Matches...");
                    }
                }
            });
            createMatchesTask.ContinueWith(result =>
            {
                CalculateBestOverlap();
                ReportResults();
                Project.Instance.UIScheduler.InvokeOnScheduler(() => IsCalculating = false);
            });
            createMatchesTask.Start();
        }

        private void stopCalculationsButton_Click(object sender, RoutedEventArgs e)
        {
            IsStopRequested = true;
        }

        #endregion

        #region INotifyPropertyChanged Members

        public event PropertyChangedEventHandler PropertyChanged;

        #endregion

        #region ITCControl Members

        public bool CanLoseFocus
        {
            get { return true; }
        }
        public bool IsActive { get; set; }
        public event Action<ITCControl, bool> CanLoseFocusChanged;

        #endregion

        private void EditMatchButton_OnClick(object sender, RoutedEventArgs e)
        {
            var button = (sender as Button);
            if (button != null)
            {
                var match = (button.DataContext as Match);
                if (match != null)
                {
                    var emd = new EditMatchDialog(match);
                    emd.ShowDialog();
                    match.PreviewBestResult = match.GetBestResultImage().Bitmap.ConvertToBitmapSource();
                }
            }
        }
    }
}