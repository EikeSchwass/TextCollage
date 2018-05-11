namespace TextCollage.Core.Import
{
    public class ImportImageSettings
    {
        #region Properties

        public ImportImageCollection ImportImageCollection { get; private set; }

        #endregion

        #region  Constructors

        public ImportImageSettings()
        {
            ImportImageCollection = new ImportImageCollection();
        }

        #endregion
    }
}