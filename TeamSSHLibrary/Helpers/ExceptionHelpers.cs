using System;

namespace TeamSSHLibrary.Helpers
{
    public static class ExceptionHelpers
    {
        #region Public Methods

        public static bool WrapObjectDisposedException(Action action)
        {
            try
            {
                action();
                return true;
            }
            catch (ObjectDisposedException)
            {
                return false;
            }
        }

        #endregion
    }
}
