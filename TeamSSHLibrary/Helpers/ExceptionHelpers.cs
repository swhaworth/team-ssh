using System;

namespace TeamSSHLibrary.Helpers
{
    public static class ExceptionHelpers
    {
        #region Public Methods

        public static bool Wrap(Action action)
        {
            try
            {
                action();
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
        
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
