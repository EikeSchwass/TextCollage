using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Media.Imaging;

using Emgu.CV.Structure;

using Microsoft.Win32;

using TextCollage.Core.Matching;
using TextCollage.UserControls;

namespace TextCollage
{
    /// <summary>
    ///     Interaktionslogik für FinalizeImageDialog.xaml
    /// </summary>
    public partial class FinalizeImageDialog : INotifyPropertyChanged
    {
        #region Properties

        public IEnumerable<Match> Matches { get; private set; }
        public BitmapSource Output { get; private set; }

        #endregion

        #region  Constructors

        public FinalizeImageDialog(IEnumerable<Match> matches)
        {
            InitializeComponent();
            Matches = matches;
            DataContext = this;
            FinalizeImageControlHostGrid.Children.Add(new FinalizeImageControl(Matches));
        }

        #endregion

        #region  Methods

        private void InvokePropertyChanged([CallerMemberName] string name = "")
        {
            if (name == "")
                return;
            PropertyChangedEventHandler pc = PropertyChanged;
            if (pc != null)
                pc(this, new PropertyChangedEventArgs(name));
        }


        private void SaveMenuItem_OnClick(object sender, RoutedEventArgs e)
        {
            FinalizeImageControl fic = (FinalizeImageControlHostGrid.Children[0] as FinalizeImageControl);
            if (fic != null)
            {
                var b = fic.ResultImage.Convert<Bgra, Byte>();
                SaveFileDialog sfd = new SaveFileDialog();
                sfd.Filter = "PNG (*.png)|*.png|JPG (*.jpg)|*.jpg|BMP (*.bmp)|*.bmp|ICO (*.ico)|*.ico";
                if (sfd.ShowDialog() == true)
                {
                    //string extension = Path.GetExtension(sfd.FileName);
                    //ImageFormat format;

                    //switch (extension)
                    //{
                    //    case ".png":
                    //        format = ImageFormat.Png;
                    //        break;
                    //    case ".jpg":
                    //        format = ImageFormat.Jpeg;
                    //        break;
                    //    case ".bmp":
                    //        format = ImageFormat.Bmp;
                    //        break;
                    //    case ".ico":
                    //        format = ImageFormat.Icon;
                    //        break;
                    //    default:
                    //        format = ImageFormat.Png;
                    //        break;
                    //}

                    b.Save(sfd.FileName);
                }
            }
        }

        private void InfoMenuItem_OnClick(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Programmiert von der Dienstagsgruppe (Eike Stein, Tim Silhan[, Christian Stolle])", "About", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        #endregion

        #region INotifyPropertyChanged Members

        public event PropertyChangedEventHandler PropertyChanged;

        #endregion
    }
}