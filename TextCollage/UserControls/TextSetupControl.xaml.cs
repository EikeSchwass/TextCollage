using System;
using System.Windows;

using TextCollage.Core.Text;

namespace TextCollage.UserControls
{
    /// <summary>
    ///     Interaktionslogik für TextSetupControl.xaml
    /// </summary>
    public partial class TextSetupControl : ITCControl
    {
        #region  Constructors

        public TextSetupControl()
        {
            InitializeComponent();
            DataContextChanged += TextSetupControl_DataContextChanged;
        }

        #endregion

        #region  Methods

        private void TextSetupControl_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            TextSettings textSettings = e.NewValue as TextSettings;
            if (textSettings != null)
                textSettings.PropertyChanged += (o, ea) =>
                {
                    if (ea.PropertyName == "ProgressBarVisibility")
                    {
                        var clfc = CanLoseFocusChanged;
                        if (clfc != null)
                            clfc(this, CanLoseFocus);
                    }
                };
        }

        private void UpdateImagePreviewButton_Click(object sender, RoutedEventArgs e)
        {
            TextSettings ts = (DataContext as TextSettings);
            if (ts != null)
            {
                ts.PropertyChangedResetEvent.Set();
            }
        }

        #endregion

        #region ITCControl Members

        public event Action<ITCControl, bool> CanLoseFocusChanged;
        public bool IsActive { get; set; }
        public bool CanLoseFocus
        {
            get
            {
                TextSettings ts = (DataContext as TextSettings);
                return ts != null && ts.ProgressBarVisibility != Visibility.Visible;
            }
        }

        #endregion
    }
}