using TextCollage.UserControls;

namespace TextCollage.Core.Finalizing
{
    public struct FinalizeSettings
    {
        #region Fields and Constants

        private readonly bool useTimeline;
        private readonly TimeSpanOptions timelineMode;

        #endregion

        #region Properties

        public bool UseTimeline
        {
            get { return useTimeline; }
        }
        public TimeSpanOptions TimelineMode
        {
            get { return timelineMode; }
        }

        #endregion

        #region  Constructors

        public FinalizeSettings(bool useTimeline, TimeSpanOptions timelineMode)
        {
            this.useTimeline = useTimeline;
            this.timelineMode = timelineMode;
        }

        #endregion
    }
}