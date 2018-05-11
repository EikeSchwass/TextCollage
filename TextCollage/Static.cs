using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media.Imaging;

using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;

using Size = System.Drawing.Size;

namespace TextCollage
{
    public static class Static
    {
        #region Fields and Constants

        public const int MaxImageResolution = 7500000;

        #endregion

        #region  Methods

        public static BitmapSource ConvertToBitmapSource(this Bitmap source)
        {
            IntPtr ip = source.GetHbitmap();
            BitmapSource bs;
            try
            {
                bs = Imaging.CreateBitmapSourceFromHBitmap(ip,
                    IntPtr.Zero, Int32Rect.Empty,
                    BitmapSizeOptions.FromEmptyOptions());
            }
            finally
            {
                DeleteObject(ip);
            }

            return bs;
        }

        public static List<T> Shuffle<T>(this IEnumerable<T> l)
        {
            var list = l.ToList();
            int n = list.Count();
            while (n > 1)
            {
                n--;
                int k = ThreadStaticRandom.Next(n + 1);
                T value = list[k];
                list[k] = list[n];
                list[n] = value;
            }
            return list;
        }

        /// <summary>
        ///     Draws an image on another at the given location.
        /// </summary>
        /// <param name="target">
        ///     The instance this method is called on.
        /// </param>
        /// <param name="source">
        ///     The image to be drawn.
        /// </param>
        /// <param name="location">The top-left location of image.</param>
        /// <returns>The new </returns>
        public static void Draw(this Image<Gray, Byte> target, Image<Gray, Byte> source, Rectangle location)
        {
            Rectangle roi = target.ROI;
            target.ROI = location;
            source.Copy(target, source);
            target.ROI = roi;
        }

        public static void Draw(this Image<Rgba, Byte> target, Image<Rgba, Byte> source, Rectangle location,
            Image<Gray, Byte> mask)
        {
            Rectangle roi = target.ROI;

            target.ROI = location;

            source.Copy(target, mask);

            target.ROI = roi;
        }

        public static Image<Gray, Byte> ExtractImagePart(this Image<Gray, Byte> image, Rectangle source, Size target)
        {
            Rectangle currentRoi = image.IsROISet ? image.ROI : Rectangle.Empty;

            image.ROI = source;
            var result = image.Resize(target.Width, target.Height, INTER.CV_INTER_LINEAR);
            image.ROI = currentRoi;

            return result;
        }

        public static double GetMaximumScaling(this Size size, Size targetSize, bool allowShrinking = true)
        {
            double xFactor = targetSize.Width * 1.0 / size.Width;
            double yFactor = targetSize.Height * 1.0 / size.Height;

            double factor = Math.Min(xFactor, yFactor);
            if (!allowShrinking)
                factor = Math.Max(1, factor);
            return factor;
        }

        public static void InvokeOnScheduler(this TaskScheduler scheduler, Action action)
        {
            new Task(action).Start(scheduler);
        }

        public static void InvokeOnSchedulerAndWait(this TaskScheduler scheduler, Action action)
        {
            new Task(action).RunSynchronously(scheduler);
        }

        [DllImport("gdi32")]
        private static extern int DeleteObject(IntPtr o);

        #endregion
    }
}