using System.Drawing;
using System.Linq;

namespace TextCollage.Core.Matching
{
    public class LinearAlgorithm : FittingAlgorithm
    {
        #region Properties

        public LinearAlgorithmSettings Settings { get; private set; }
        public double SizeFactor { get; private set; }
        public int X { get; private set; }
        public int Y { get; private set; }

        #endregion

        #region  Constructors

        public LinearAlgorithm(Match match, LinearAlgorithmSettings settings)
            : base(match)
        {
            Settings = settings;
            SizeFactor = match.Character.GetAlphaImage().Size.GetMaximumScaling(match.Image.ROIImage.ROIAlphaImage.Size);
            X = 0;
            Y = 0;
        }

        #endregion

        #region  Methods

        public override IterationResults NextIteration()
        {
            while (X + CharacterAlphaImage.Width * SizeFactor >= ROIAlphaImage.Width - 1 ||
                   Y + CharacterAlphaImage.Height * SizeFactor >= ROIAlphaImage.Height - 1 && SizeFactor >= 0.1)
            {
                if (X + CharacterAlphaImage.Width * SizeFactor >= ROIAlphaImage.Width - 1)
                {
                    int deltaX =
                        (int)
                            (Settings.HorizontalStepOffset -
                             (X + CharacterAlphaImage.Width * SizeFactor - ROIAlphaImage.Width));
                    X = deltaX;
                    Y += (int)Settings.VerticalStepOffset;
                }
                if (Y + CharacterAlphaImage.Height * SizeFactor >= ROIAlphaImage.Height - 1)
                {
                    SizeFactor -= Settings.SizeStep;
                    Y = 0;
                    X = 0;
                }
            }
            IterationResults iterationResults = new IterationResults();

            if (SizeFactor < 0.1)
                return iterationResults;

            Rectangle sourceRectangle = new Rectangle(X, Y, (int)(CharacterAlphaImage.Width * SizeFactor),
                (int)(CharacterAlphaImage.Height * SizeFactor));
            double fitness = Rate(sourceRectangle);
            IterationResult iterationResult = new IterationResult(fitness, sourceRectangle);
            iterationResults = new IterationResults(iterationResult, iterationResult, iterationResult);
            AddIterationResult(iterationResults);
            X += (int)Settings.HorizontalStepOffset;
            return iterationResults;
        }

        protected override void AddIterationResult(IterationResults result)
        {
            if (IterationResults.Count == 0 || result.BestResult.Rating > IterationResults.Last().BestResult.Rating)
                base.AddIterationResult(result);
            else
                base.AddIterationResult(IterationResults.Last());
        }

        #endregion
    }
}