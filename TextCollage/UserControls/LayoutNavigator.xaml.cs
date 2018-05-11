using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;

using TextCollage.Core;

namespace TextCollage.UserControls
{
    /// <summary>
    ///     Interaktionslogik für LayoutNavigator.xaml
    /// </summary>
    public partial class LayoutNavigator : INotifyPropertyChanged
    {
        #region Properties

        public bool CanNavigateBack { get; private set; }
        public bool CanNavigateNext { get; private set; }
        private int CurrentIndex { get; set; }
        private ITCControl[] PossibleContent { get; set; }

        #endregion

        #region  Constructors

        public LayoutNavigator()
        {
            InitializeComponent();
            DataContext = this;
            if (!DesignerProperties.GetIsInDesignMode(this))
                LoadPossibleContent();
        }

        #endregion

        #region  Methods

        private void backButton_Click(object sender, RoutedEventArgs e)
        {
            CurrentIndex--;
            UpdateNavigation();
        }

        [SuppressMessage("ReSharper", "ConditionIsAlwaysTrueOrFalse")]
        private void c_CanLoseFocusChanged(ITCControl arg1, bool arg2)
        {
            UpdateNavigation();
            if (PossibleContent[CurrentIndex] == arg1)
            {
                if (!arg2)
                    CanNavigateBack = arg2;
                if (!arg2)
                    CanNavigateNext = arg2;
            }
        }

        private void InvokePropertyChanged([CallerMemberName] string name = "")
        {
            if (name == "")
                return;
            new Task(() =>
            {
                PropertyChangedEventHandler pc = PropertyChanged;
                if (pc != null)
                    pc(this, new PropertyChangedEventArgs(name));
            }).Start(Project.Instance.UIScheduler);
        }

        private void LoadPossibleContent()
        {
            PossibleContent = new ITCControl[]
            {
                new TextSetupControl {DataContext = Project.Instance.TextSettings},
                new ImageImportControl {DataContext = Project.Instance.ImportImageSettings},
                new FitnessEvaluationControl()
            };
            foreach (ITCControl c in PossibleContent)
            {
                c.CanLoseFocusChanged += c_CanLoseFocusChanged;
            }
            UpdateNavigation();
        }

        private void nextButton_Click(object sender, RoutedEventArgs e)
        {
            CurrentIndex++;
            UpdateNavigation();
        }

        private void UpdateNavigation()
        {
            ITCControl currentContent = (ContentControl.Content as ITCControl);
            if (currentContent != null)
                currentContent.IsActive = false;
            ContentControl.Content = PossibleContent[CurrentIndex];
            PossibleContent[CurrentIndex].IsActive = true;

            CanNavigateNext = CurrentIndex < PossibleContent.Length - 1;
            CanNavigateBack = CurrentIndex >= 1;

            InvokePropertyChanged("CanNavigateBack");
            InvokePropertyChanged("CanNavigateNext");
        }

        #endregion

        #region INotifyPropertyChanged Members

        public event PropertyChangedEventHandler PropertyChanged;

        #endregion
    }
}