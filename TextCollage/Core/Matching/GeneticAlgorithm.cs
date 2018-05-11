using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;

namespace TextCollage.Core.Matching
{
    public class GeneticAlgorithm : FittingAlgorithm
    {
        #region Fields and Constants

        private readonly IList<Phenotype> phenotypes = new List<Phenotype>();

        #endregion

        #region Properties

        public GeneticAlgorithmSettings Settings { get; private set; }

        #endregion

        #region  Constructors

        public GeneticAlgorithm(Match match, GeneticAlgorithmSettings settings)
            : base(match)
        {
            Settings = settings;
            Load();
            for (int i = 0; i < settings.PopulationSize; i++)
            {
                phenotypes.Add(Phenotype.CreateNew(this));
            }
            UnLoad();
        }

        #endregion

        #region  Methods

        private Phenotype TournamentSelection(TournamentSelectionSettings settings)
        {
            var currentTournament = new List<Phenotype>();
            while (currentTournament.Count < settings.TournamentSize * phenotypes.Count)
            {
                Phenotype currentPhenotype = phenotypes.ElementAt(ThreadStaticRandom.Next(phenotypes.Count));
                while (currentTournament.Contains(currentPhenotype))
                    currentPhenotype = phenotypes.ElementAt(ThreadStaticRandom.Next(phenotypes.Count));
                currentTournament.Add(currentPhenotype);
            }
            return currentTournament.OrderByDescending(p => p.Rating).First();
        }

        public override IterationResults NextIteration()
        {
            var newGeneration = new List<Phenotype>();

            switch (Settings.GeneticAlgorithmSelectionMode)
            {
                case GeneticAlgorithmSelectionMode.Tournament:
                    TournamentSelectionSettings tournamentSettings = Settings.SelectionSettings as TournamentSelectionSettings;
                    Debug.Assert(tournamentSettings != null, "settings != null");

                    while (newGeneration.Count < phenotypes.Count)
                    {
                        Phenotype firstPhenotype = TournamentSelection(tournamentSettings);
                        Phenotype secondPhenotype = TournamentSelection(tournamentSettings);

                        var children = Phenotype.CrossOver(firstPhenotype, secondPhenotype);

                        newGeneration.Add(children.Item1);
                        newGeneration.Add(children.Item2);
                    }

                    break;
                case GeneticAlgorithmSelectionMode.Elite:
                    ElitismSelectionSettings elitismSettings = Settings.SelectionSettings as ElitismSelectionSettings;
                    Debug.Assert(elitismSettings != null, "elitismSettings != null");

                    var orderedPhenotypes = phenotypes.OrderByDescending(p => p.Rating);

                    for (int i = 0; i < orderedPhenotypes.Count() * elitismSettings.OverallSelectionRatio; i++)
                    {
                        newGeneration.Add(orderedPhenotypes.ElementAt(i));
                    }

                    while (newGeneration.Count < phenotypes.Count)
                    {
                        var children = Phenotype.CrossOver(phenotypes.ElementAt(ThreadStaticRandom.Next(phenotypes.Count)), phenotypes.ElementAt(ThreadStaticRandom.Next(phenotypes.Count)));
                        newGeneration.Add(children.Item1);
                        newGeneration.Add(children.Item2);
                    }

                    break;
            }

            foreach (Phenotype p in newGeneration)
            {
                double chance = ThreadStaticRandom.NextDouble();
                if (chance <= Settings.MutationProbability)
                    p.Mutate();
            }

            phenotypes.Clear();
            foreach (Phenotype p in newGeneration)
                phenotypes.Add(p);

            foreach (Phenotype dns in phenotypes)
            {
                dns.Rating = Rate(dns.Rectangle);
            }

            var results = (from dns in phenotypes
                           orderby dns.Rating descending
                           select dns);
            Phenotype bestResult = results.First();
            Phenotype worstResult = results.Last();
            Phenotype averageResult = results.ElementAt(results.Count() / 2);

            IterationResults iterationResults = new IterationResults(new IterationResult(bestResult.Rating, bestResult.Rectangle), new IterationResult(worstResult.Rating, worstResult.Rectangle), new IterationResult(averageResult.Rating, averageResult.Rectangle));

            AddIterationResult(iterationResults);

            return iterationResults;
        }

        #endregion

        #region Nested type: Phenotype

        private class Phenotype
        {
            #region Properties

            public double[] DNS { get; private set; }
            public GeneticAlgorithm Parent { get; private set; }
            public double Rating { get; set; }
            public Rectangle Rectangle
            {
                get { return new Rectangle((int)DNS[0], (int)DNS[1], (int)(Parent.CharacterAlphaImage.Width * DNS[2]), (int)(Parent.CharacterAlphaImage.Height * DNS[2])); }
            }

            #endregion

            #region  Constructors

            public Phenotype(GeneticAlgorithm parent)
            {
                Parent = parent;
            }

            #endregion

            #region  Methods

            public static Phenotype CreateNew(GeneticAlgorithm parent)
            {
                Phenotype phenotype = new Phenotype(parent);

                double x, y, sizeFactor;

                do
                {
                    x = ThreadStaticRandom.NextDouble() * phenotype.Parent.ROIAlphaImage.Width;
                    y = ThreadStaticRandom.NextDouble() * phenotype.Parent.ROIAlphaImage.Height;
                    sizeFactor = ThreadStaticRandom.NextDouble();
                    phenotype.DNS = new[] {x, y, sizeFactor};
                } while (!phenotype.IsValid());

                phenotype.Rating = parent.Rate(phenotype.Rectangle);

                return phenotype;
            }

            public static Tuple<Phenotype, Phenotype> CrossOver(Phenotype firstPhenotype, Phenotype secondPhenotype)
            {
                if (firstPhenotype == secondPhenotype)
                    return new Tuple<Phenotype, Phenotype>(firstPhenotype.Clone(), secondPhenotype.Clone());

                int splitIndex = ThreadStaticRandom.Next(2) + 1;

                Phenotype result1 = new Phenotype(firstPhenotype.Parent);
                Phenotype result2 = new Phenotype(firstPhenotype.Parent);
                switch (splitIndex)
                {
                    case 1:
                        result1.DNS = new[] {firstPhenotype.DNS[0], secondPhenotype.DNS[1], secondPhenotype.DNS[2]};
                        result2.DNS = new[] {firstPhenotype.DNS[0], firstPhenotype.DNS[1], secondPhenotype.DNS[2]};
                        break;
                    case 2:
                        result2.DNS = new[] {firstPhenotype.DNS[0], secondPhenotype.DNS[1], secondPhenotype.DNS[2]};
                        result1.DNS = new[] {firstPhenotype.DNS[0], firstPhenotype.DNS[1], secondPhenotype.DNS[2]};
                        break;
                }

                if (!result1.IsValid())
                    result1 = firstPhenotype.Clone();

                if (!result2.IsValid())
                    result2 = secondPhenotype.Clone();

                return new Tuple<Phenotype, Phenotype>(result1, result2);
            }

            public bool IsValid()
            {
                if (DNS[0] < 0 || DNS[1] < 0 || DNS[2] < 0.1)
                    return false;
                return DNS[0] + Parent.CharacterAlphaImage.Width * DNS[2] < Parent.ROIAlphaImage.Width && DNS[1] + Parent.CharacterAlphaImage.Height * DNS[2] < Parent.ROIAlphaImage.Height;
            }

            public void Mutate()
            {
                var currentDNS = DNS.Clone() as double[];
                Debug.Assert(currentDNS != null, "currentDNS != null");

                do
                {
                    DNS = new[] {currentDNS[0], currentDNS[1], currentDNS[2]};
                    int index = ThreadStaticRandom.Next(0, 3);
                    DNS[index] += index != 2 ? ThreadStaticRandom.NextDouble() * 10 - 5 : ThreadStaticRandom.NextDouble() * 0.3 - 0.15;
                } while (!IsValid());
            }

            private Phenotype Clone()
            {
                return new Phenotype(Parent) {DNS = DNS, Rating = Rating};
            }

            #endregion
        }

        #endregion
    }
}