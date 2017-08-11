using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace TeamSSHClient
{
    internal sealed class ArgumentHandler
    {
        #region Ctors

        public ArgumentHandler(string[] args)
        {
            this.Arguments = args;
        }

        #endregion

        #region Properties

        public IEnumerable<string> Arguments { get; }
        public ClientMode ExecutionMode { get => this.GetExecutionMode(); }

        #endregion

        #region Public Methods

        public string Find(string firstArgumentName)
        {
            for (var c = 0; c < this.Arguments.Count(); ++c)
            {
                if (StringComparer.OrdinalIgnoreCase.Equals(this.Arguments.ElementAt(c), firstArgumentName))
                {
                    return this.Arguments.ElementAtOrDefault(c + 1);
                }
            }
            return null;
        }

        public bool? GetBoolArgument(string firstArgumentName)
        {
            var secondArgumentValue = this.Find(firstArgumentName);
            if (string.IsNullOrEmpty(secondArgumentValue))
            {
                return null;
            }
            if (bool.TryParse(secondArgumentValue, out var boolValue))
            {
                return boolValue;
            }
            if (int.TryParse(secondArgumentValue, out var intValue))
            {
                return intValue != 0;
            }
            return null;
        }

        public bool GetBoolArgument(string firstArgumentName, bool defaultValue)
        {
            return this.GetBoolArgument(firstArgumentName).GetValueOrDefault(defaultValue);
        }

        public int? GetIntArgument(string firstArgumentName)
        {
            var secondArgumentValue = this.Find(firstArgumentName);
            if (string.IsNullOrEmpty(secondArgumentValue))
            {
                return null;
            }
            if (int.TryParse(secondArgumentValue, out var intValue))
            {
                return intValue;
            }
            return null;
        }

        public int GetIntArgument(string firstArgumentName, int defaultValue)
        {
            return this.GetIntArgument(firstArgumentName).GetValueOrDefault(defaultValue);
        }

        public string GetStringArgument(string firstArgumentName)
        {
            return this.Find(firstArgumentName);
        }

        public string GetStringArgument(string firstArgumentName, string defaultValue)
        {
            var value = this.GetStringArgument(firstArgumentName);
            return !string.IsNullOrEmpty(value) ? value : defaultValue;
        }

        #endregion

        #region Private Methods

        private ClientMode GetExecutionMode()
        {
            for (var c = 0; c < this.Arguments.Count(); ++c)
            {
                if (Enum.TryParse<ClientMode>(this.Arguments.ElementAt(c), true, out var mode))
                {
                    return mode;
                }
            }
            return ClientMode.Help;
        }

        #endregion
    }
}
