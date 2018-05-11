using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using TextCollage.Core.Matching;

namespace TextCollage.Core.Finalizing
{
    public class SolutionTree
    {
        #region Fields and Constants

        private readonly Match[] matches;
        private readonly SortedList<double, Node> openNodes = new SortedList<double, Node>();

        #endregion

        #region Properties

        public KeyValuePair<Match[], double>? BestMatching { get; private set; }
        private static SolutionTree Instance { get; set; }

        #endregion

        #region  Constructors

        public SolutionTree(Match[] matches)
        {
            Instance = this;
            this.matches = matches;

            var initNodes = (from m in matches
                             where m.Character.Index == 0
                             select new Node(m, null));
            foreach (Node initNode in initNodes)
            {
                double dublicatePenalty = 0;
                while (openNodes.ContainsKey(initNode.PathRating - dublicatePenalty))
                    dublicatePenalty += 0.0001;
                openNodes.Add(initNode.PathRating - dublicatePenalty, initNode);
            }
        }

        #endregion

        #region  Methods

        public void NextCalculation()
        {
            if (openNodes.Count == 0)
            {
                Task.Delay(1000).Wait();
                return;
            }
            var bestNode = openNodes.First();
            openNodes.RemoveAt(0);
            if (bestNode.Value.Children.Length == 0)
            {
                if (BestMatching == null || bestNode.Key > BestMatching.Value.Value)
                {
                    var matches = (from n in bestNode.Value.Path
                                   select n.Match).ToArray();
                    BestMatching = new KeyValuePair<Match[], double>(matches, bestNode.Key);
                }
            }
            bestNode.Value.Search();
        }

        #endregion

        #region Nested type: Node

        private class Node
        {
            #region Fields and Constants

            private Node[] children;
            private Node[] path;

            #endregion

            #region Properties

            public Node[] Path
            {
                get
                {
                    if (path == null)
                    {
                        var pathNodes = new List<Node>();
                        if (Parent != null)
                            pathNodes.AddRange(Parent.Path);
                        pathNodes.Add(this);
                        path = pathNodes.ToArray();
                    }
                    return path;
                }
            }
            public Node[] Children
            {
                get
                {
                    if (children == null)
                    {
                        var childNodes = new List<Node>();
                        childNodes.AddRange(from match in Instance.matches
                                            where match.Character.Index == Match.Character.Index + 1 && Path.All(n => n.Match.Image != match.Image)
                                            select new Node(match, this));
                        children = childNodes.ToArray();
                    }
                    return children;
                }
            }
            public Match Match { get; set; }
            public double Rating { get; private set; }
            public double PathRating { get; private set; }
            private Node Parent { get; set; }

            #endregion

            #region  Constructors

            public Node(Match match, Node parent)
            {
                Match = match;
                Parent = parent;
                Rating = match.NormalisiedFitness;
                PathRating = Path.Sum(n => n.Rating) + Path.Average(n => n.Rating) * (Instance.matches.Max(m => m.Character.Index) - Match.Character.Index);
            }

            #endregion

            #region  Methods

            public void Search()
            {
                foreach (Node child in Children)
                {
                    double dublicatePenalty = 0;
                    while (Instance.openNodes.ContainsKey(child.PathRating - dublicatePenalty))
                        dublicatePenalty += 0.0001;
                    Instance.openNodes.Add(child.PathRating + dublicatePenalty, child);
                }
            }

            #endregion
        }

        #endregion
    }
}