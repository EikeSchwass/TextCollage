using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

using TextCollage.Core.Matching;
using TextCollage.UserControls;

namespace TextCollage.Core.Finalizing
{
    public class Bruteforce
    {
        #region Fields and Constants

        public bool NewBestMatch;
        private double bestRating = double.MinValue;
        private int numberMatches;
        private bool stop;
        private readonly Match[] matches;

        #endregion

        #region Properties

        public Match[] BestMatches { get; private set; }
        private FinalizeSettings FinalizeSettings { get; set; }

        #endregion

        #region  Constructors

        public Bruteforce(Match[] matches, FinalizeSettings finalizeSettings)
        {
            this.matches = matches;
            FinalizeSettings = finalizeSettings;
        }

        #endregion

        #region  Methods

        public void ComputeNext()
        {
            var matchList = (from m in matches
                             where m.Character.Index == 0 && m.Enabled
                             orderby m.NormalisiedFitness descending
                             select m).ToList();
            //foreach(var match in matchList)

            Parallel.ForEach(matchList, match =>
            {
                var nextDepthMatches = new List<Match> { match };
                DepthSearch(nextDepthMatches, 1);
            });

        }

        public void Stop()
        {
            stop = true;
        }

        private bool RespectsTimeline(params Match[] currentMatches)
        {
            TimeSpanOptions timeline = FinalizeSettings.TimelineMode;
            DateTime currentTime = DateTime.MinValue;

            for (int i = 0; i < currentMatches.Length; i++)
            {
                bool respected = false;
                FileInfo fi = new FileInfo(currentMatches[i].Image.Path);
                DateTime time = fi.CreationTime;

                switch (timeline)
                {
                    case TimeSpanOptions.Total:
                        respected = time >= currentTime;
                        break;
                    case TimeSpanOptions.Year:
                        respected = time.Year >= currentTime.Year || time >= currentTime;
                        break;
                    case TimeSpanOptions.Month:
                        respected = time.Month >= currentTime.Month || time.Year > currentTime.Year || time > currentTime;
                        break;
                    case TimeSpanOptions.Day:
                        respected = time.Day >= currentTime.Day || time.Month > currentTime.Month || time.Year > currentTime.Year || time >= currentTime;
                        break;
                    case TimeSpanOptions.Hour:
                        respected = time.Hour >= currentTime.Hour || time.Day > currentTime.Day || time.Month > currentTime.Month || time.Year > currentTime.Year || time >= currentTime;
                        break;
                    case TimeSpanOptions.Minute:
                        respected = time.Minute >= currentTime.Minute || time.Hour > currentTime.Hour || time.Day > currentTime.Day || time.Month > currentTime.Month || time.Year > currentTime.Year || time >= currentTime;
                        break;
                }
                currentTime = time;
                if (!respected)
                    return false;
            }
            return true;
        }


        private void DepthSearch(List<Match> currentMatches, int depth)
        {
            if (stop)
                return;

            var nextMatch = (from m in matches
                             where m.Character.Index == depth && currentMatches.All(k => k.Image != m.Image) && m.Enabled && (!FinalizeSettings.UseTimeline || RespectsTimeline(currentMatches.Last(), m))
                             orderby m.NormalisiedFitness descending
                             select m).ToArray();

            if (!nextMatch.Any())
            {
                double rating = currentMatches.Sum(t => t.NormalisiedFitness);

                if (bestRating < rating)
                {
                    lock (this)
                    {
                        BestMatches = currentMatches.ToArray();
                        bestRating = rating;
                        NewBestMatch = true;
                    }
                }
                numberMatches++;
                return;
            }

            Action<Match> recursiveCall = match =>
            {
                var nextDepthMatches = new List<Match>();
                nextDepthMatches.AddRange(currentMatches);
                nextDepthMatches.Add(match);
                DepthSearch(nextDepthMatches, depth + 1);
            };

            if (depth < 4)
                Parallel.ForEach(nextMatch, recursiveCall);
            else
            {
                foreach (Match match in nextMatch)
                {
                    recursiveCall(match);
                    var nextDepthMatches = new List<Match>();
                    nextDepthMatches.AddRange(currentMatches);
                    nextDepthMatches.Add(match);
                    DepthSearch(nextDepthMatches, depth + 1);
                }
            }
        }

        #endregion
    }
}