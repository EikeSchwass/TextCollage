using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace TextCollage.Core.Matching
{
    public abstract class AlgorithmSettings : INotifyPropertyChanged
    {
        #region  Methods

        protected void InvokePropertyChanged([CallerMemberName] string name = "")
        {
            if (name == "")
                return;
            PropertyChangedEventHandler pc = PropertyChanged;
            if (pc != null)
                pc(this, new PropertyChangedEventArgs(name));
        }

        #endregion

        #region INotifyPropertyChanged Members

        public event PropertyChangedEventHandler PropertyChanged;

        #endregion
    }

    #region Genetic

    public class GeneticAlgorithmSettings : AlgorithmSettings
    {
        #region Fields and Constants

        private GeneticAlgorithmSelectionMode geneticAlgorithmSelectionMode;
        private double mutationProbability;
        private int populationSize;
        private GeneticAlgorithmSelectionSettings selectionSettings;

        #endregion

        #region Properties

        public int PopulationSize
        {
            get { return populationSize; }
            set
            {
                if (value < 2 || value > 10000)
                    throw new ValidationException("The value has to be between 2 and 10000");
                populationSize = value;
                InvokePropertyChanged();
            }
        }
        public double MutationProbability
        {
            get { return mutationProbability; }
            set
            {
                if (value < 0 || value > 1)
                    throw new ValidationException("The value has to be between 0 and 1");
                mutationProbability = value;
                InvokePropertyChanged();
            }
        }
        public GeneticAlgorithmSelectionMode GeneticAlgorithmSelectionMode
        {
            get { return geneticAlgorithmSelectionMode; }
            set
            {
                switch (value)
                {
                    case GeneticAlgorithmSelectionMode.Elite:
                        SelectionSettings = new ElitismSelectionSettings();
                        break;
                    case GeneticAlgorithmSelectionMode.Tournament:
                        SelectionSettings = new TournamentSelectionSettings();
                        break;
                }
                geneticAlgorithmSelectionMode = value;
                InvokePropertyChanged();
            }
        }
        public GeneticAlgorithmSelectionSettings SelectionSettings
        {
            get { return selectionSettings; }
            set
            {
                selectionSettings = value;
                InvokePropertyChanged();
            }
        }

        #endregion

        #region  Constructors

        public GeneticAlgorithmSettings()
        {
            GeneticAlgorithmSelectionMode = GeneticAlgorithmSelectionMode.Tournament;
            PopulationSize = 100;
            MutationProbability = 0.01;
        }

        #endregion
    }

    #region Selection Settings

    public abstract class GeneticAlgorithmSelectionSettings : INotifyPropertyChanged
    {
        #region  Methods

        protected void InvokePropertyChanged([CallerMemberName] string name = "")
        {
            if (name == "")
                return;
            PropertyChangedEventHandler pc = PropertyChanged;
            if (pc != null)
                pc(this, new PropertyChangedEventArgs(name));
        }

        #endregion

        #region INotifyPropertyChanged Members

        public event PropertyChangedEventHandler PropertyChanged;

        #endregion
    }

    public class ElitismSelectionSettings : GeneticAlgorithmSelectionSettings
    {
        #region Fields and Constants

        private double overallSelectionRatio;

        #endregion

        #region Properties

        public double OverallSelectionRatio
        {
            get { return overallSelectionRatio; }
            set
            {
                if (value <= 0 || value > 1)
                    throw new ValidationException("The value has to be between 0 and 1");
                overallSelectionRatio = value;
                InvokePropertyChanged();
            }
        }

        #endregion

        #region  Constructors

        public ElitismSelectionSettings()
        {
            OverallSelectionRatio = 0.35;
        }

        #endregion
    }

    public class TournamentSelectionSettings : GeneticAlgorithmSelectionSettings
    {
        #region Fields and Constants

        private double overallSelectionRatio;
        private double tournamentSize;

        #endregion

        #region Properties

        public double TournamentSize
        {
            get { return tournamentSize; }
            set
            {
                if (value <= 0 || value > 1)
                    throw new ValidationException("The value has to be between 0 and 1");
                tournamentSize = value;
                InvokePropertyChanged();
            }
        }
        public double OverallSelectionRatio
        {
            get { return overallSelectionRatio; }
            set
            {
                if (value <= 0 || value > 1)
                    throw new ValidationException("The value has to be between 0 and 1");
                overallSelectionRatio = value;
                InvokePropertyChanged();
            }
        }

        #endregion

        #region  Constructors

        public TournamentSelectionSettings()
        {
            OverallSelectionRatio = 0.35;
            TournamentSize = 0.1;
        }

        #endregion
    }

    #endregion

    public enum GeneticAlgorithmSelectionMode
    {
        Elite,
        Tournament
    }

    #endregion

    #region Linear

    public class LinearAlgorithmSettings : AlgorithmSettings
    {
        #region Fields and Constants

        private double sizeStep, horizontalStepOffset, verticalStepOffset;

        #endregion

        #region Properties

        public double HorizontalStepOffset
        {
            get { return horizontalStepOffset; }
            set
            {
                if (value <= 1 || value > 100)
                    throw new ValidationException("The value has to be between 1 and 100");
                horizontalStepOffset = value;
                InvokePropertyChanged();
            }
        }
        public double VerticalStepOffset
        {
            get { return verticalStepOffset; }
            set
            {
                if (value <= 1 || value > 100)
                    throw new ValidationException("The value has to be between 1 and 100");
                verticalStepOffset = value;
                InvokePropertyChanged();
            }
        }
        public double SizeStep
        {
            get { return sizeStep; }
            set
            {
                if (value <= 0 || value >= 1)
                    throw new ValidationException("The value has to be between 0 and 1");
                sizeStep = value;
                InvokePropertyChanged();
            }
        }

        #endregion

        #region  Constructors

        public LinearAlgorithmSettings()
        {
            SizeStep = 0.1;
            HorizontalStepOffset = 4;
            VerticalStepOffset = 4;
        }

        #endregion
    }

    #endregion

    #region Random

    public class RandomAlgorithmSettings : AlgorithmSettings
    {
        public RandomAlgorithmSettings()
        {
            
        }
    }

    #endregion
}