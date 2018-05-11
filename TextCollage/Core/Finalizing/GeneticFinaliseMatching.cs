using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;

using TextCollage.Core.Matching;

namespace TextCollage.Core.Finalizing
{
    public class GeneticFinaliseMatching
    {
        #region Fields and Constants

        public static object LockObject = new object();

        private Match[] matches;
        private List<Phenotype> phenotypes = new List<Phenotype>();

        #endregion

        #region Properties
        private Func<Match[], double> RatingFunc { get; set; }
        public Phenotype BestPhenotype { get; private set; }
        public List<Phenotype[]> Generations { get; private set; }

        #endregion

        public GeneticFinaliseMatching()
        {
            Generations = new List<Phenotype[]>();
        }

        #region  Methods

        public void Init(Match[] matchCollection, int phenoCount, Func<Match[], double> ratingFunc)
        {
            lock (LockObject)
            {
                Generations.Clear();
                RatingFunc = ratingFunc;
                BestPhenotype = null;
                matches = new Match[matchCollection.Length];
                for (int i = 0; i < matchCollection.Length; i++)
                {
                    matches[i] = matchCollection[i];
                }
                phenotypes = new List<Phenotype>();

                while (phenotypes.Count < phenoCount)
                {
                    var chars = (from match in matches
                                 select match.Character).Distinct().OrderBy(m => m.Index);

                    Match[] dns = new Match[chars.Count()];
                    for (int i = 0; i < dns.Length; i++)
                    {
                        var candidates = (from match in matches
                                          where match.Character.Index == i && dns.All(m => m == null || m.Image != match.Image)
                                          select match).ToList();
                        dns[i] = candidates.ElementAt(ThreadStaticRandom.Next(candidates.Count()));
                    }

                    Phenotype newPhenotype = new Phenotype(dns, ratingFunc(dns));
                    phenotypes.Add(newPhenotype);
                }
                phenotypes = phenotypes.OrderByDescending(p => p.Rating).ToList();
                BestPhenotype = phenotypes[0];
            }
        }

        public void NextGeneration(double mutationProbability = 0.025, double tournamentSize = 0.1)
        {
            lock (LockObject)
            {
                List<Phenotype> newGeneration = new List<Phenotype>();
                while (newGeneration.Count < phenotypes.Count)
                {
                    Phenotype p1;
                    {
                        int size = (int)Math.Ceiling(phenotypes.Count * tournamentSize);
                        List<Phenotype> tournament = new List<Phenotype>();

                        var indicies = Enumerable.Range(0, phenotypes.Count).Shuffle().ToList();

                        while (tournament.Count < size)
                        {
                            tournament.Add(phenotypes[indicies[0]]);
                            indicies.RemoveAt(0);
                        }
                        p1 = tournament.OrderByDescending(p => p.Rating).First();
                    }
                    Phenotype p2;
                    {
                        int size = (int)Math.Ceiling(phenotypes.Count * 0.1);
                        List<Phenotype> tournament = new List<Phenotype>();

                        var indicies = Enumerable.Range(0, phenotypes.Count).Shuffle().ToList();

                        while (tournament.Count < size)
                        {
                            tournament.Add(phenotypes[indicies[0]]);
                            indicies.RemoveAt(0);
                        }
                        p2 = tournament.OrderByDescending(p => p.Rating).First();
                    }
                    var result = Merge(p1, p2, mutationProbability);
                    newGeneration.Add(result.Item1);
                    newGeneration.Add(result.Item2);
                }
                phenotypes = newGeneration.OrderByDescending(p => p.Rating).ToList();
                Generations.Add(phenotypes.ToArray());
                var currentBest = phenotypes.First();
                if (currentBest.Rating > BestPhenotype.Rating)
                    BestPhenotype = currentBest;
            }
        }

        private Tuple<Phenotype, Phenotype> Merge(Phenotype p1, Phenotype p2, double mutationProbability = 0.125)
        {
            Match[] newDNS1 = new Match[p1.DNS.Length];
            Match[] newDNS2 = new Match[p2.DNS.Length];
            for (int i = 0; i < newDNS1.Length; i++)
            {
                newDNS1[i] = p1.DNS[i];
                newDNS2[i] = p2.DNS[i];
            }

            //var indicies = Enumerable.Range(0, p1.DNS.Length).Shuffle();
            //for (int i = 0; i < indicies.Count; i++)
            //{
            //    int index = indicies[i];
            //    for (int j = 0; j < index; j++)
            //    {
            //        newDNS1[j] = p1.DNS[j];
            //        newDNS2[j] = p2.DNS[j];
            //    }
            //    for (int j = index; j < newDNS1.Length; j++)
            //    {
            //        newDNS1[j] = p2.DNS[j];
            //        newDNS2[j] = p1.DNS[j];
            //    }
            //    if (IsLegal(newDNS1) && IsLegal(newDNS2))
            //    {
            //        break;
            //    }
            //}


            double mutate1 = ThreadStaticRandom.NextDouble();
            if (mutate1 <= mutationProbability)
            {
                int index = ThreadStaticRandom.Next(newDNS1.Length);
                var candidates = (from match in matches
                                  where match.Character.Index == newDNS1[index].Character.Index && !newDNS1.Contains(match) && newDNS1.All(m => m.Image.Path != match.Image.Path)
                                  select match).ToList();
                if (candidates.Count > 0)
                {
                    Match newMatch = candidates[ThreadStaticRandom.Next(candidates.Count)];
                    newDNS1[index] = newMatch;
                }
            }
            double mutate2 = ThreadStaticRandom.NextDouble();
            if (mutate2 <= mutationProbability)
            {
                int index = ThreadStaticRandom.Next(newDNS2.Length);
                var candidates = (from match in matches
                                  where match.Character.Index == newDNS2[index].Character.Index && !newDNS2.Contains(match) && newDNS2.All(m => m.Image.Path != match.Image.Path)
                                  select match).ToList();
                if (candidates.Count > 0)
                {
                    Match newMatch = candidates[ThreadStaticRandom.Next(candidates.Count)];
                    newDNS2[index] = newMatch;
                }
            }

            Phenotype phenotype1 = new Phenotype(newDNS1, RatingFunc(newDNS1));
            Phenotype phenotype2 = new Phenotype(newDNS2, RatingFunc(newDNS2));

            return new Tuple<Phenotype, Phenotype>(phenotype1, phenotype2);

        }

        private bool IsLegal(Match[] newDNS)
        {
            if ((from m in newDNS select m.Image.Path).Distinct().Count() < newDNS.Length)
                return false;


            for (int i = 0; i < newDNS.Length; i++)
            {
                if (!newDNS[i].Character.Equals(Project.Instance.TextSettings.CharacterCollection[i]))
                {
                    return false;
                }

                if (newDNS.Count(m => m.Image.Path == newDNS[i].Image.Path) >= 2)
                    return false;
            }
            return true;
        }

        #endregion
    }

    public class Phenotype
    {
        #region Properties

        public double Rating { get; private set; }
        public Match[] DNS { get; private set; }

        #endregion

        #region  Constructors

        public Phenotype(Match[] dns, double rating)
        {
            Rating = rating;
            DNS = new Match[dns.Length];
            for (int i = 0; i < dns.Length; i++)
            {
                DNS[i] = dns[i];
            }
        }

        #endregion
    }
}