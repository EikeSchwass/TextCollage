using System;

namespace TextCollage.UserControls
{
    public interface ITCControl
    {
        #region Properties

        bool IsActive { set; }
        bool CanLoseFocus { get; }

        #endregion

        #region  Methods

        event Action<ITCControl, bool> CanLoseFocusChanged;

        #endregion
    }
}