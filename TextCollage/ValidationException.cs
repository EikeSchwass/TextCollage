using System;

namespace TextCollage
{
    public class ValidationException : Exception
    {
        #region  Constructors

        public ValidationException(string message)
            : base(message)
        {
        }

        #endregion
    }
}