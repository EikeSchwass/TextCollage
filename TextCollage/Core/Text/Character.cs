using System;
using System.Drawing;
using System.Drawing.Text;

using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;

namespace TextCollage.Core.Text
{
    public struct Character
    {
        #region Fields and Constants

        private readonly char character;
        private readonly string cSharpFont;
        private readonly FONT font;
        private readonly double horizontalCharacterStretch;
        private readonly double hScale;
        private readonly int index;
        private readonly Rectangle location;
        private readonly int thickness;
        private readonly bool useCSharp;

        #endregion

        #region Properties

        public int Index
        {
            get { return index; }
        }
        public bool UseCSharp
        {
            get { return useCSharp; }
        }
        public double HorizontalCharacterStretch
        {
            get { return horizontalCharacterStretch; }
        }
        public string CSharpFont
        {
            get { return cSharpFont; }
        }
        public FONT Font
        {
            get { return font; }
        }
        public double HScale
        {
            get { return hScale; }
        }
        public int Thickness
        {
            get { return thickness; }
        }
        public char Char
        {
            get { return character; }
        }
        public Rectangle Location
        {
            get { return location; }
        }

        #endregion

        #region  Constructors

        public Character(char character, TextSettings settings, int index, Rectangle location, double hScale,
            int thickness)
        {
            this.index = index;
            font = settings.Font;
            cSharpFont = settings.CSharpFont;
            horizontalCharacterStretch = settings.HorizontalCharacterStretch;
            useCSharp = settings.UseCSharp;
            this.character = character;
            this.thickness = thickness;
            this.hScale = hScale;
            this.location = new Rectangle(location.X, location.Y, location.Width, location.Height);
        }

        #endregion

        #region  Methods

        public Image<Gray, Byte> GetAlphaImage()
        {
            if (!UseCSharp)
            {
                var image = new Image<Gray, Byte>(Location.Width, Location.Height, new Gray(0));
                MCvFont font = new MCvFont();
                CvInvoke.cvInitFont(ref font, Font, HScale, 1, 0, Thickness, LINE_TYPE.CV_AA);
                image.Draw(Char + "", ref font, new Point(0, Location.Height - Thickness - 2), new Gray(255));
                return image;
            }
            else
            {
                Bitmap image = new Bitmap((int)(Location.Width / HorizontalCharacterStretch), Location.Height);
                Font font = new Font(CSharpFont, (float)HScale, FontStyle.Bold, GraphicsUnit.Pixel);
                using (Graphics g = Graphics.FromImage(image))
                {
                    g.TextRenderingHint = TextRenderingHint.AntiAlias;
                    g.DrawString(Char + "", font, Brushes.White, 0, 0, StringFormat.GenericTypographic);
                    g.Flush();
                }
                image.MakeTransparent(Color.Black);
                var result = new Image<Gray, Byte>(image);
                result = result.Resize(Location.Width, Location.Height, INTER.CV_INTER_AREA);
                result = result.SmoothGaussian((int)Math.Max(2, HorizontalCharacterStretch * 2) + 1, 3, 30, 30);
                //result = result.SmoothBlur((int)Math.Max(1, HorizontalCharacterStretch * 2), 3);
                return result;
            }
        }

        public Image<Gray, Byte> GetImage(bool drawBorder)
        {
            if (!UseCSharp)
            {
                var image = new Image<Gray, Byte>(Location.Width, Location.Height, new Gray(1));
                MCvFont font = new MCvFont();
                CvInvoke.cvInitFont(ref font, Font, HScale, 1, 0, Thickness, LINE_TYPE.CV_AA);
                image.Draw(Char + "", ref font, new Point(0, Location.Height - Thickness - 2), new Gray(255));
                return image;
            }
            else
            {
                Bitmap image = new Bitmap((int)(Location.Width / HorizontalCharacterStretch), Location.Height);
                Font font = new Font(CSharpFont, (float)HScale, FontStyle.Bold, GraphicsUnit.Pixel);
                using (Graphics g = Graphics.FromImage(image))
                {
                    g.TextRenderingHint = TextRenderingHint.AntiAlias;
                    g.DrawString(Char + "", font, Brushes.White, 0, 0, StringFormat.GenericTypographic);
                    if (drawBorder)
                        g.DrawRectangle(new Pen(Brushes.Red, 4), 2, 2, image.Width - 4, image.Height - 4);
                    g.Flush();
                }
                image.MakeTransparent(Color.Black);
                var result = new Image<Rgba, Byte>(image);
                result = result.Resize(Location.Width, Location.Height, INTER.CV_INTER_AREA);
                result = result.SmoothGaussian((int)Math.Max(2, HorizontalCharacterStretch * 2) + 1, 3, 30, 30);
                //result = result.SmoothBlur((int)Math.Max(1, HorizontalCharacterStretch * 2), 3);
                return result.Convert<Gray, Byte>();
            }
        }

        public override bool Equals(object obj)
        {
            if (!(obj is Character))
                return base.Equals(obj);

            Character c = (Character)obj;

            return c.Char == Char &&
                   c.CSharpFont == CSharpFont &&
                   c.Font == Font &&
                   c.HorizontalCharacterStretch == HorizontalCharacterStretch &&
                   c.HScale == HScale &&
                   c.Index == Index &&
                   c.Location == Location &&
                   c.Thickness == Thickness &&
                   c.UseCSharp == UseCSharp;
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        #endregion
    }
}