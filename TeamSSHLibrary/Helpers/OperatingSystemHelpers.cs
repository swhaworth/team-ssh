using System;
using System.IO;

namespace TeamSSHLibrary.Helpers
{
    public static class OperatingSystemHelpers
    {
        #region Public Methods

        public static string GetHomeDirectory()
        {
            var homeVariable = Environment.GetEnvironmentVariable("HOME");
            if (!string.IsNullOrEmpty(homeVariable) && Directory.Exists(homeVariable))
            {
                return homeVariable;
            }
            var homeDriveVariable = Environment.GetEnvironmentVariable("HOMEDRIVE");
            var homePathVariable = Environment.GetEnvironmentVariable("HOMEPATH");
            if (!string.IsNullOrEmpty(homeDriveVariable) && !string.IsNullOrEmpty(homePathVariable))
            {
                return homeDriveVariable + homePathVariable;
            }
            throw new NotSupportedException("Could not determine home directory.");
        }

        #endregion
    }
}
