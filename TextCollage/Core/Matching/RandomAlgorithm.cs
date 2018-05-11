using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace TextCollage.Core.Matching
{
    public class RandomAlgorithm : FittingAlgorithm
    {
        public RandomAlgorithm(Match match)
            : base(match)
        {
        }

        public override IterationResults NextIteration()
        {
            double sizeFactor = ThreadStaticRandom.NextDouble() * Match.Character.GetAlphaImage().Size.GetMaximumScaling(Match.Image.ROIImage.ROIAlphaImage.Size);
            double x = ThreadStaticRandom.Next(0, (int)(ROIAlphaImage.Width - CharacterAlphaImage.Width * sizeFactor - 1)), y = ThreadStaticRandom.Next(0, (int)(ROIAlphaImage.Height - CharacterAlphaImage.Height * sizeFactor - 1));
            while (x + CharacterAlphaImage.Width * sizeFactor >= ROIAlphaImage.Width - 1 || y + CharacterAlphaImage.Height * sizeFactor >= ROIAlphaImage.Height - 1 || sizeFactor < 0.1)
            {
                sizeFactor = ThreadStaticRandom.NextDouble() * Match.Character.GetAlphaImage().Size.GetMaximumScaling(Match.Image.ROIImage.ROIAlphaImage.Size);
                x = ThreadStaticRandom.Next(0, (int)(ROIAlphaImage.Width - CharacterAlphaImage.Width * sizeFactor - 1));
                y = ThreadStaticRandom.Next(0, (int)(ROIAlphaImage.Height - CharacterAlphaImage.Height * sizeFactor - 1));
            }
            Rectangle sourceRectangle = new Rectangle((int)x, (int)y, (int)(CharacterAlphaImage.Width * sizeFactor), (int)(CharacterAlphaImage.Height * sizeFactor));
            double fitness = Rate(sourceRectangle);
            IterationResult iterationResult = new IterationResult(fitness, sourceRectangle);
            var iterationResults = new IterationResults(iterationResult, iterationResult, iterationResult);
            AddIterationResult(iterationResults);
            return iterationResults;
        }

        protected override void AddIterationResult(IterationResults result)
        {
            if (IterationResults.Count == 0 || result.BestResult.Rating > IterationResults.Last().BestResult.Rating)
                base.AddIterationResult(result);
            else
                base.AddIterationResult(IterationResults.Last());
        }
    }
}
