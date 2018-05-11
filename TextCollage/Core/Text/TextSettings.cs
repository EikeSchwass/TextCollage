using System;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Drawing.Text;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;

using Color = System.Drawing.Color;
using FontFamily = System.Drawing.FontFamily;
using FontStyle = System.Drawing.FontStyle;
using Size = System.Drawing.Size;

namespace TextCollage.Core.Text
{
    [Serializable]
    public class TextSettings : INotifyPropertyChanged
    {
        #region Fields and Constants

        /// <summary>
        ///     Automatic preview image update, when settings have changed.
        /// </summary>
        private bool autoUpdatePreviewImage = true;

        /// <summary>
        ///     The space between two characters.
        /// </summary>
        private int[] characterSpacing = {0};

        /// <summary>
        ///     The Font that is used, when c# should be used.
        /// </summary>
        private FontFamily cSharpFont = new FontFamily("Arial Black");

        /// <summary>
        ///     Draws the character borders.
        /// </summary>
        private bool drawCharacterBorders;

        /// <summary>
        ///     The Fontstyle that is used.
        /// </summary>
        private FONT font = FONT.CV_FONT_HERSHEY_DUPLEX;

        /// <summary>
        ///     The Height of the image.
        /// </summary>
        private int height = 900;

        /// <summary>
        ///     The amount of horizontal stretching.
        /// </summary>
        private double horizontalCharacterStretch = 1.0;

        /// <summary>
        ///     Horizontal margin of each character.
        /// </summary>
        private int[] horizontalMargin = {0};

        /// <summary>
        ///     Horizontal offset.
        /// </summary>
        private int[] horizontalOffset = {0};

        /// <summary>
        ///     The rendering accuracy. Smaller value means higher is higher accuracy.
        /// </summary>
        private double renderingAccuracy = 9.0;

        /// <summary>
        ///     The users inputtext splitted by new-line.
        /// </summary>
        private string[] text = {"Text","Collage"};

        /// <summary>
        ///     The stroke thickness of each character.
        /// </summary>
        private int[] thickness = {35};

        /// <summary>
        ///     Use emguCV.
        /// </summary>
        private bool useCSharp = true;

        /// <summary>
        ///     Vertical margin of each character.
        /// </summary>
        private int[] verticalMargin = {0};

        /// <summary>
        ///     Vertical offset.
        /// </summary>
        private int[] verticalOffset = {0};

        private double virtualCharacterSpacingFactor = 2;

        /// <summary>
        ///     The width of the image.
        /// </summary>
        private int width = 1440;

        public readonly AutoResetEvent PropertyChangedResetEvent = new AutoResetEvent(false);
        private readonly TaskScheduler scheduler;

        #endregion

        #region Properties

        /// <summary>
        ///     The rendering accuracy.
        /// </summary>
        public double RenderingAccuracy
        {
            get { return renderingAccuracy; }
            set
            {
                renderingAccuracy = value;
                InvokePropertyChanged();
            }
        }
        /// <summary>
        ///     The progress of the current rendering.
        /// </summary>
        public double RenderingProgress { get; private set; }
        /// <summary>
        ///     The visibility of a potential progressBar.
        /// </summary>
        public Visibility ProgressBarVisibility { get; private set; }
        /// <summary>
        ///     The brush for the border to signal progress.
        /// </summary>
        public SolidColorBrush ProgressBorderBrush { get; private set; }
        /// <summary>
        ///     Description to be shown while calculating.
        /// </summary>
        public string ProgressDescription { get; private set; }
        /// <summary>
        ///     Horizontal offset.
        /// </summary>
        public string HorizontalOffset
        {
            get
            {
                string value = "";
                for (int i = 0; i < horizontalOffset.Length; i++)
                {
                    value += horizontalOffset[i];
                    if (i < horizontalOffset.Length - 1)
                        value += ";";
                }
                return value;
            }
            set
            {
                var values = value.Split(';');

                var margins = new int[values.Length];
                for (int i = 0; i < margins.Length; i++)
                {
                    margins[i] = int.Parse(values[i]);
                }

                horizontalOffset = margins.ToArray();
                InvokePropertyChanged();
            }
        }
        /// <summary>
        ///     Horizontal offset.
        /// </summary>
        public string VerticalOffset
        {
            get
            {
                string value = "";
                for (int i = 0; i < verticalOffset.Length; i++)
                {
                    value += verticalOffset[i];
                    if (i < verticalOffset.Length - 1)
                        value += ";";
                }
                return value;
            }
            set
            {
                var values = value.Split(';');

                var margins = new int[values.Length];
                for (int i = 0; i < margins.Length; i++)
                {
                    margins[i] = int.Parse(values[i]);
                }

                verticalOffset = margins.ToArray();
                InvokePropertyChanged();
            }
        }
        /// <summary>
        ///     Horizontal margin of each character.
        /// </summary>
        public string HorizontalMargin
        {
            get
            {
                string value = "";
                for (int i = 0; i < horizontalMargin.Length; i++)
                {
                    value += horizontalMargin[i];
                    if (i < horizontalMargin.Length - 1)
                        value += ";";
                }
                return value;
            }
            set
            {
                try
                {
                    var values = value.Split(';');

                    var margins = new int[values.Length];
                    for (int i = 0; i < margins.Length; i++)
                    {
                        margins[i] = int.Parse(values[i]);
                    }

                    horizontalMargin = margins.ToArray();
                    InvokePropertyChanged();
                }
                catch (Exception e)
                {
                    throw new ValidationException("Margin! " + e.Message);
                }
            }
        }
        /// <summary>
        ///     Vertical margin of each character.
        /// </summary>
        public string VerticalMargin
        {
            get
            {
                string value = "";
                for (int i = 0; i < verticalMargin.Length; i++)
                {
                    value += verticalMargin[i];
                    if (i < verticalMargin.Length - 1)
                        value += ";";
                }
                return value;
            }
            set
            {
                try
                {
                    var values = value.Split(';');

                    var margins = new int[values.Length];
                    for (int i = 0; i < margins.Length; i++)
                    {
                        margins[i] = int.Parse(values[i]);
                    }

                    verticalMargin = margins.ToArray();
                    InvokePropertyChanged();
                }
                catch (Exception e)
                {
                    throw new ValidationException("Margin! " + e.Message);
                }
            }
        }
        /// <summary>
        ///     The Fontstyle that is used.
        /// </summary>
        public bool DrawCharacterBorders
        {
            get { return drawCharacterBorders; }
            set
            {
                drawCharacterBorders = value;
                InvokePropertyChanged();
            }
        }
        /// <summary>
        ///     Horizontal character stretch.
        /// </summary>
        public double HorizontalCharacterStretch
        {
            get { return horizontalCharacterStretch; }
            set
            {
                if (value < 0.1 || value > 10)
                    throw new ValidationException("The Value for \"HorizontalCharacterStretch\" is invalid");
                horizontalCharacterStretch = value;
                InvokePropertyChanged();
            }
        }
        /// <summary>
        ///     The emguCV fontstyle.
        /// </summary>
        public FONT Font
        {
            get { return font; }
            set
            {
                font = value;
                InvokePropertyChanged();
            }
        }
        /// <summary>
        ///     The Font that is used, when c# drawing is used.
        /// </summary>
        public string CSharpFont
        {
            get { return cSharpFont.Name; }
            set
            {
                try
                {
                    cSharpFont = new FontFamily(value);
                }
                catch
                {
                    throw new ValidationException("Font not found");
                }
                InvokePropertyChanged();
            }
        }
        /// <summary>
        ///     All FontFamilies.
        /// </summary>
        public string[] FontFamilies
        {
            get
            {
                return (from ff in FontFamily.Families
                        select ff.Name).ToArray();
            }
        }
        /// <summary>
        ///     The stroke thickness of each character.
        /// </summary>
        public string Thickness
        {
            get
            {
                string value = "";
                for (int i = 0; i < thickness.Length; i++)
                {
                    value += thickness[i];
                    if (i < thickness.Length - 1)
                        value += ";";
                }
                return value;
            }
            set
            {
                var values = value.Split(';');

                var margins = new int[values.Length];
                for (int i = 0; i < margins.Length; i++)
                {
                    int localThickness = int.Parse(values[i]);
                    if (localThickness <= 0)
                        throw new ValidationException("Thickness has to be at least 1");
                    margins[i] = localThickness;
                }

                thickness = margins.ToArray();
                InvokePropertyChanged();
            }
        }
        /// <summary>
        ///     The space between two characters.
        /// </summary>
        public string CharacterSpacing
        {
            get
            {
                string value = "";
                for (int i = 0; i < characterSpacing.Length; i++)
                {
                    value += characterSpacing[i];
                    if (i < characterSpacing.Length - 1)
                        value += ";";
                }
                return value;
            }
            set
            {
                var values = value.Split(';');

                var margins = new int[values.Length];
                for (int i = 0; i < margins.Length; i++)
                {
                    margins[i] = int.Parse(values[i]);
                }

                characterSpacing = margins.ToArray();
                InvokePropertyChanged();
            }
        }
        /// <summary>
        ///     Width of the output image.
        /// </summary>
        public int Width
        {
            get { return width; }
            set
            {
                if (value < 10 || value > 10000)
                    throw new ValidationException("Invalid width input.");
                width = value;
                InvokePropertyChanged();
            }
        }
        /// <summary>
        ///     Height of the output image.
        /// </summary>
        public int Height
        {
            get { return height; }
            set
            {
                if (value < 10 || value > 10000)
                    throw new ValidationException("Invalid height input.");
                height = value;
                InvokePropertyChanged();
            }
        }
        /// <summary>
        ///     Automatic preview image update, when settings have changed.
        /// </summary>
        public bool AutoUpdatePreviewImage
        {
            get { return autoUpdatePreviewImage; }
            set
            {
                autoUpdatePreviewImage = value;
                InvokePropertyChanged();
            }
        }
        /// <summary>
        ///     Automatic preview image update, when settings have changed.
        /// </summary>
        public bool UseCSharp
        {
            get { return useCSharp; }
            set
            {
                useCSharp = value;
                InvokePropertyChanged();
            }
        }
        /// <summary>
        ///     The users inputtext.
        /// </summary>
        public string InputText
        {
            get
            {
                string value = "";
                for (int i = 0; i < text.Length; i++)
                {
                    value += text[i];
                    if (i < text.Length - 1)
                        value += Environment.NewLine;
                }
                return value;
            }
            set
            {
                text = value.Split(new[] {Environment.NewLine}, StringSplitOptions.None);
                InvokePropertyChanged();
            }
        }
        /// <summary>
        ///     The users inputtext splitted by new-line.
        /// </summary>
        public string[] Text
        {
            get { return text.Clone() as string[]; }
        }
        public double VirtualCharacterSpacingFactor
        {
            get { return virtualCharacterSpacingFactor; }
            set
            {
                if (value < 1 || value >= 10)
                    throw new ValidationException("The VirtualCharacterSpacingFacter has to be between 1 and 10");
                virtualCharacterSpacingFactor = value;
                InvokePropertyChanged();
            }
        }
        /// <summary>
        ///     A Previewimage based on the current settings.
        /// </summary>
        public BitmapSource PreviewImageSource { get; private set; }
        public CharacterCollection CharacterCollection { get; private set; }

        #endregion

        #region  Constructors

        /// <summary>
        ///     Constructor to create new instance. Renders one previewimage with default settings before returning.
        /// </summary>
        public TextSettings()
        {
            PropertyChanged += TextSettings_PropertyChanged;
            scheduler = TaskScheduler.FromCurrentSynchronizationContext();

            Bitmap b = GeneratePreviewImage().Bitmap;
            b.MakeTransparent(Color.Black);
            PreviewImageSource = b.ConvertToBitmapSource();
            InvokePropertyChanged("PreviewImageSource");

            StartBackgroundCalculation();
        }

        #endregion

        #region  Methods

        public Size GetMaximumCharacterSize()
        {
            int maxWidth = int.MinValue;
            int maxHeight = int.MinValue;

            foreach (Character c in CharacterCollection)
            {
                if (c.Location.Width > maxWidth)
                    maxWidth = c.Location.Width;

                if (c.Location.Height > maxHeight)
                    maxHeight = c.Location.Height;
            }

            return new Size((int)(maxWidth * VirtualCharacterSpacingFactor),
                (int)(maxHeight * VirtualCharacterSpacingFactor)); // VirtualCharacterSpacingFactor));
        }

        /// <summary>
        ///     Calculates the c# charactermasks for each character with the desired settings.
        /// </summary>
        /// <returns>A CharacterCollection instance containing all the masks</returns>
        private CharacterCollection GetCharacterMaskCSharp()
        {
            double totalTextLength = 0;

            for (int i = 0; i < Text.Length; i++)
                for (int j = 0; j < Text[i].Length; j++)
                    totalTextLength++;

            CharacterCollection characterCollection = new CharacterCollection();

            double currentTotalIndex = 0;

            for (int i = 0; i < Text.Length; i++)
            {
                int characterSpacing = this.characterSpacing[Math.Min(i, this.characterSpacing.Length - 1)];
                int thickness = this.thickness[Math.Min(i, this.thickness.Length - 1)];
                int horizontalMargin = this.horizontalMargin[Math.Min(i, this.horizontalMargin.Length - 1)];
                int verticalMargin = this.verticalMargin[Math.Min(i, this.verticalMargin.Length - 1)];
                int horizontalOffset = this.horizontalOffset[Math.Min(i, this.horizontalOffset.Length - 1)];
                int verticalOffset = this.verticalOffset[Math.Min(i, this.verticalOffset.Length - 1)];
                double hScale = 1000;
                Size neededSize;

                SetRenderingProgress(currentTotalIndex / totalTextLength * 80, "Calculating Size in Line #" + (i + 1));
                do
                {
                    int width = 0;
                    int height = 0;

                    foreach (char c in Text[i])
                    {
                        Size tempSize = GetGraphicsTextSize(c + "", hScale);
                        width += (int)(tempSize.Width * HorizontalCharacterStretch);
                        height = Math.Max(tempSize.Height, height);
                    }


                    width += (Text[i].Length - 1) * characterSpacing;

                    int extraWidth = horizontalMargin;
                    int extraHeight = verticalMargin;

                    Size wordSize = new Size(width, height);
                    neededSize = new Size(wordSize.Width + extraWidth, wordSize.Height + extraHeight);
                    hScale -= Math.Max((neededSize.Height + extraHeight - Height / Text.Length) / RenderingAccuracy, Math.Max(1, (neededSize.Width + extraWidth - Width) / RenderingAccuracy));
                } while (neededSize.Width > Width || neededSize.Height >= Height / Text.Length);
                CharacterCollection currentLineCharacterCollection = new CharacterCollection();

                Size trueSize = GetGraphicsTextSize(Text[i], hScale);
                trueSize = new Size(trueSize.Width + (Text[i].Length - 1) * characterSpacing, trueSize.Height);
                trueSize = new Size((int)(trueSize.Width * HorizontalCharacterStretch), trueSize.Height);

                int x = Width / 2 - trueSize.Width / 2 + horizontalOffset;

                int localHeight = Height / Text.Length;
                int y = localHeight / 2 - trueSize.Height / 2 + i * localHeight + verticalOffset;

                for (int j = 0; j < Text[i].Length; j++)
                {
                    currentTotalIndex++;
                    char currentChar = Text[i][j];
                    Size charSize = GetGraphicsTextSize(currentChar + "", hScale);
                    charSize = new Size((int)(charSize.Width * HorizontalCharacterStretch), charSize.Height);
                    int currentX = x + currentLineCharacterCollection.TotalWidth(0, j) + characterSpacing * j;
                    Rectangle location = new Rectangle(currentX, y, charSize.Width, charSize.Height);
                    Character character = new Character(currentChar, this, (int)currentTotalIndex - 1, location, hScale,
                        thickness);
                    currentLineCharacterCollection.Add(character);
                }
                characterCollection.Add(currentLineCharacterCollection);
            }
            SetRenderingProgress(currentTotalIndex / totalTextLength * 80, "Size calculation completed");
            return characterCollection;
        }

        /// <summary>
        ///     Calculates the emguCV charactermasks for each character with the desired settings.
        /// </summary>
        /// <returns>A CharacterCollection instance containing all the masks</returns>
        private CharacterCollection GetCharacterMasks()
        {
            CharacterCollection characterCollection = new CharacterCollection();

            MCvFont font = new MCvFont();
            int baseline = 0;
            int currentCharacterIndex = 0;

            for (int i = 0; i < Text.Length; i++)
            {
                string s = Text[i];
                Size size = new Size(30, 15000);

                double hScale = 500;

                int characterSpacing = this.characterSpacing[Math.Min(i, this.characterSpacing.Length - 1)];
                int thickness = this.thickness[Math.Min(i, this.thickness.Length - 1)];
                int horizontalMargin = this.horizontalMargin[Math.Min(i, this.horizontalMargin.Length - 1)];
                int verticalMargin = this.verticalMargin[Math.Min(i, this.verticalMargin.Length - 1)];
                int horizontalOffset = this.horizontalOffset[Math.Min(i, this.horizontalOffset.Length - 1)];
                int verticalOffset = this.verticalOffset[Math.Min(i, this.verticalOffset.Length - 1)];

                CvInvoke.cvInitFont(ref font, Font, hScale, 1, 0, thickness, LINE_TYPE.CV_AA);
                CvInvoke.cvGetTextSize(s, ref font, ref size, ref baseline);

                while (size.Height > Height / Text.Length || size.Width > Width)
                {
                    hScale -= 1;
                    CvInvoke.cvInitFont(ref font, Font, Math.Round(hScale, 2), 1, 0, thickness, LINE_TYPE.CV_AA);
                    if (!UseCSharp)
                        CvInvoke.cvGetTextSize(s, ref font, ref size, ref baseline);

                    int newWidth = size.Width + horizontalMargin * 2 + characterSpacing * (Text[i].Length - 1);
                    int newHeight = size.Height + verticalMargin * 2 + thickness;

                    size = new Size(newWidth, newHeight);
                }

                CharacterCollection charactersInCurrentLine = new CharacterCollection();

                int thicknessPenalty = thickness;

                int x = Width / 2 - size.Width / 2 + thicknessPenalty / 2 + horizontalMargin;
                x += horizontalOffset;

                int localHeight = Height / Text.Length;
                int y = (int)(localHeight / 2 + size.Height / 2 + i * localHeight - 2 * thicknessPenalty / 3.0 - verticalMargin);
                y += verticalOffset;

                for (int j = 0; j < Text[i].Length; j++)
                {
                    CvInvoke.cvGetTextSize(Text[i][j] + "", ref font, ref size, ref baseline);

                    int currentX = x + charactersInCurrentLine.TotalWidth(0, j);
                    currentX += characterSpacing * j;
                    int currentY = y - size.Height;

                    Character character = new Character(Text[i][j], this, currentCharacterIndex,
                        new Rectangle(currentX, currentY, size.Width - thicknessPenalty, size.Height - thicknessPenalty),
                        hScale, thickness);

                    charactersInCurrentLine.Add(character);
                    currentCharacterIndex++;
                }
                characterCollection.Add(charactersInCurrentLine);
            }

            return characterCollection;
        }

        /// <summary>
        ///     Starts the Task that will periotically update the preview image.
        /// </summary>
        [SuppressMessage("ReSharper", "FunctionNeverReturns")]
        private void StartBackgroundCalculation()
        {
            Task.Run(() =>
            {
                while (true)
                {
                    try
                    {
                        PropertyChangedResetEvent.WaitOne();
                        Bitmap b = GeneratePreviewImage().Bitmap;
                        b.MakeTransparent(Color.Black);
                        new Task(() =>
                        {
                            PreviewImageSource = b.ConvertToBitmapSource();
                            InvokePropertyChanged("PreviewImageSource");
                        }).Start(scheduler);
                    }
                    catch (Exception e)
                    {
                        SetRenderingProgress(0, "---Error during Calculation (" + e.Message + ")---", true);
                    }
                }
            });
        }

        private void InvokePropertyChanged([CallerMemberName] string name = "null")
        {
            if (name == "null")
                return;
            PropertyChangedEventHandler e = PropertyChanged;
            if (e != null)
                e(this, new PropertyChangedEventArgs(name));
        }

        /// <summary>
        ///     Generates a new preview image.
        /// </summary>
        /// <returns>The new preview image.</returns>
        private Image<Gray, Byte> GeneratePreviewImage()
        {
            SetRenderingProgress(0.1, "Inititialize...");
            var result = new Image<Gray, Byte>(Width, Height, new Gray(1));
            try
            {
                CharacterCollection characters = !UseCSharp ? GetCharacterMasks() : GetCharacterMaskCSharp();

                CharacterCollection = characters;

                for (int i = 0; i < characters.Count; i++)
                {
                    double progress = 80.0 + i * 1.0 / characters.Count * 20.0;
                    Character character = characters.ElementAt(i);
                    var image = GetCharacterImage(character);
                    result.Draw(image, character.Location);
                    SetRenderingProgress(progress, "Drawing Character #" + (i + 1) + "...");
                }
                return result;
                //}
                //catch
                //{
                //var font = new MCvFont(FONT.CV_FONT_HERSHEY_COMPLEX, 10, 10);
                //var errorImage = new Image<Rgba, Byte>(Width, Height, new Rgba(200, 200, 200, 0));
                //errorImage.Draw("Error composing text", ref font, new Point(errorImage.Width / 2, errorImage.Height / 2), new Rgba(255, 0, 0, 255));
                //return errorImage;
            }
            finally
            {
                SetRenderingProgress(100, "Finished");
            }
        }

        /// <summary>
        ///     Creates the image of the given character.
        /// </summary>
        /// <param name="c">The character.</param>
        /// <returns>The calculated image.</returns>
        private Image<Gray, Byte> GetCharacterImage(Character c)
        {
            return c.GetImage(DrawCharacterBorders);
        }

        /// <summary>
        ///     Calculates the needed space for a given string using GDI+.
        /// </summary>
        /// <param name="s">The string which space should be calculated.</param>
        /// <param name="fontSize">The foontsize.</param>
        /// <returns></returns>
        private Size GetGraphicsTextSize(string s, double fontSize)
        {
            if (s.Length > 1)
            {
                Size sum = new Size(0, 0);
                return s.Select(c => GetGraphicsTextSize(c + "", fontSize)).Aggregate(sum, (current, size) => new Size(current.Width + size.Width, Math.Max(current.Height, size.Height)));
            }
            if (s.Length <= 0)
            {
                return new Size(0, 0);
            }
            if (s[0] == ' ')
            {
                return new Size((int)(fontSize / 4), (int)(fontSize * 1.1175));
            }

            Bitmap b = new Bitmap(Width, Height);
            using (Graphics graphics = Graphics.FromImage(b))
            {
                graphics.TextRenderingHint = TextRenderingHint.AntiAlias;
                SizeF size = graphics.MeasureString(s,
                    new Font(CSharpFont, (float)fontSize, FontStyle.Bold, GraphicsUnit.Pixel), Width * 4,
                    StringFormat.GenericTypographic);

                graphics.Dispose();
                b.Dispose();
                return new Size((int)(size.Width), (int)size.Height);
            }
        }

        private void SetRenderingProgress(double value, string description, bool error = false)
        {
            Task t = new Task(() =>
            {
                ProgressDescription = description.Substring(0, Math.Min(description.Length, 125));
                if (ProgressDescription.Length < description.Length)
                    ProgressDescription = ProgressDescription + "...";

                value = Math.Max(Math.Min(100, value), 0);
                ProgressBarVisibility = value >= 99 ? Visibility.Collapsed : Visibility.Visible;
                RenderingProgress = value;

                System.Windows.Media.Color color = Colors.Green;

                if (value < 100)
                    color = Colors.DarkOrange;
                if (error)
                    color = Colors.Red;

                ProgressBorderBrush = new SolidColorBrush(color);

                InvokePropertyChanged("ProgressBarVisibility");
                InvokePropertyChanged("ProgressBorderBrush");
                InvokePropertyChanged("RenderingProgress");
                InvokePropertyChanged("ProgressDescription");
            });
            t.Start(scheduler);
        }

        private void TextSettings_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "PreviewImageSource" || e.PropertyName == "ProgressBarVisibility" || e.PropertyName == "RenderingProgress" || e.PropertyName == "ProgressBorderBrush" || e.PropertyName == "ProgressDescription")
                return;
            if (AutoUpdatePreviewImage)
                PropertyChangedResetEvent.Set();
        }

        #endregion

        #region INotifyPropertyChanged Members

        public event PropertyChangedEventHandler PropertyChanged;

        #endregion
    }
}