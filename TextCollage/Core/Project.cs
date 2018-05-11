using System;
using System.Threading.Tasks;

using TextCollage.Core.Import;
using TextCollage.Core.Text;

namespace TextCollage.Core
{
    [Serializable]
    public class Project
    {
        #region Fields and Constants

        private static Project project = new Project();

        #endregion

        #region Properties

        public static Project Instance
        {
            get { return project ?? (project = new Project()); }
        }
        public TextSettings TextSettings { get; set; }
        public ImportImageSettings ImportImageSettings { get; set; }
        public TaskScheduler UIScheduler { get; private set; }

        #endregion

        #region  Constructors

        private Project()
        {
            UIScheduler = TaskScheduler.FromCurrentSynchronizationContext();
            TextSettings = new TextSettings();
            ImportImageSettings = new ImportImageSettings();
        }

        #endregion
    }
}