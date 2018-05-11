using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.AccessControl;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

using Emgu.CV;
using Emgu.CV.Structure;

using Microsoft.Win32;

using TextCollage.Annotations;
using TextCollage.Core;
using TextCollage.Core.Import;

using Image = System.Windows.Controls.Image;
using Point = System.Windows.Point;

namespace TextCollage.UserControls
{
    /// <summary>
    ///     Interaktionslogik für ImageImportControl.xaml
    /// </summary>
    public partial class ImageImportControl : ITCControl, INotifyPropertyChanged
    {
        #region  Constructors

        public ImageImportControl()
        {
            InitializeComponent();
        }

        #endregion

        #region  Methods

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null)
                handler(this, new PropertyChangedEventArgs(propertyName));
        }

        private void addButton_Click(object sender, RoutedEventArgs e)
        {
            ImportImageSettings iis = DataContext as ImportImageSettings;

            OpenFileDialog ofd = new OpenFileDialog { Filter = "All supported images formats (*.jpg, *.png, *.bmp)|*.jpg;*.png;*.bmp)", Multiselect = true };
            if (ofd.ShowDialog() == true)
            {
                CanLoseFocus = false;
                var clfc = CanLoseFocusChanged;
                if (clfc != null)
                    clfc(this, CanLoseFocus);
                IsEnabled = false;
                LoadImageProgressBar.Visibility = Visibility.Visible;
                LoadImageProgressBar.Value = 0;
                Task.Run(() =>
                {
                    try
                    {
                        var loadedIIs = new List<ImportImage>();
                        for (int i = 0; i < ofd.FileNames.Length; i++)
                        {
                            string file = ofd.FileNames[i];
                            Debug.Assert(iis != null, "iis != null");
                            int count = (from p in iis.ImportImageCollection
                                         where p.Path == file
                                         select p).Count();
                            if (count > 0)
                                continue;
                            ImportImage ii = ImportImage.Create(file);
                            loadedIIs.Add(ii);
                            int i1 = i;
                            new Task(() => LoadImageProgressBar.Value = i1 * 1.0 / ofd.FileNames.Length * 100).Start(
                                Project.Instance.UIScheduler);
                        }

                        return loadedIIs;
                    }
                    catch
                    {
                        return null;
                    }
                }).ContinueWith(b =>
                {
                    CanLoseFocus = true;
                    var clfc2 = CanLoseFocusChanged;
                    if (clfc2 != null)
                        clfc2(this, CanLoseFocus);
                    try
                    {
                        for (int i = 0; i < b.Result.Count; i++)
                        {
                            Debug.Assert(iis != null, "iis != null");
                            iis.ImportImageCollection.Add(b.Result[i]);
                        }
                        IsEnabled = true;
                        ImportImageListBox.SelectedIndex = 0;
                        LoadImageProgressBar.Visibility = Visibility.Collapsed;
                        Debug.Assert(iis != null, "iis != null");
                        DeleteButton.IsEnabled = iis.ImportImageCollection.Count > 0;
                    }
                    catch
                    {
                        // ignored
                    }
                }, TaskScheduler.FromCurrentSynchronizationContext());
            }
        }

        private void deleteButton_Click(object sender, RoutedEventArgs e)
        {
            ImportImageSettings iis = DataContext as ImportImageSettings;

            var selection = new object[ImportImageListBox.SelectedItems.Count];

            ImportImageListBox.SelectedItems.CopyTo(selection, 0);

            ImportImageListBox.SelectedItem = null;

            for (int i = 0; i < selection.Length; i++)
            {
                object item = selection[i];
                Debug.Assert(iis != null, "iis != null");
                iis.ImportImageCollection.Remove(item as ImportImage);
            }
            ImportImageListBox.SelectedIndex = 0;
            Debug.Assert(iis != null, "iis != null");
            DeleteButton.IsEnabled = iis.ImportImageCollection.Count > 0;
        }

        private void Image_MouseMove(object sender, MouseEventArgs e)
        {
            Image imageControl = (sender as Image);
            if (imageControl == null)
                return;
            ImportImage ii = imageControl.Tag as ImportImage;
            if (ii == null)
                return;


            Point pos = e.GetPosition(imageControl);
            System.Drawing.Point correctPos = new System.Drawing.Point((int)(pos.X * ii.ROIImage.ROIAlphaImage.Width * 1.0 / imageControl.ActualWidth),
                (int)(pos.Y * ii.ROIImage.ROIAlphaImage.Height * 1.0 / imageControl.ActualHeight));

            if (e.LeftButton == MouseButtonState.Pressed)
            {
                if (!imageControl.IsMouseCaptured)
                    imageControl.CaptureMouse();
                ii.ROIImage.UserROIPoints.Add(new System.Drawing.Point(correctPos.X, correctPos.Y));
                ii.ROIImage.UpdateROI();
            }
            if (e.RightButton == MouseButtonState.Pressed)
            {
                if (!imageControl.IsMouseCaptured)
                    imageControl.CaptureMouse();
                ii.ROIImage.UserROIPoints.Add(new System.Drawing.Point(correctPos.X, correctPos.Y));
                ii.ROIImage.UpdateROI();
            }
        }

        private void Image_MouseUp(object sender, MouseButtonEventArgs e)
        {
            FrameworkElement imageControl = (sender as FrameworkElement);
            if (imageControl == null)
                return;
            ImportImage ii = imageControl.DataContext as ImportImage;
            if (ii == null)
                return;

            imageControl.ReleaseMouseCapture();

            ii.ROIImage.UpdateROI();
        }

        private void resetCustomROIButton_Click(object sender, RoutedEventArgs e)
        {
            FrameworkElement imageControl = (sender as FrameworkElement);
            if (imageControl == null)
                return;
            ImportImage ii = imageControl.DataContext as ImportImage;
            if (ii == null)
                return;
            ii.ROIImage.ResetCustomROI();
        }

        #endregion

        #region INotifyPropertyChanged Members

        public event PropertyChangedEventHandler PropertyChanged;

        #endregion

        #region ITCControl Members

        public event Action<ITCControl, bool> CanLoseFocusChanged;
        public bool IsActive { get; set; }
        public bool CanLoseFocus { get; private set; }

        #endregion
    }
}