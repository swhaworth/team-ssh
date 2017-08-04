using System.Runtime.CompilerServices;
using System.Text;

namespace TeamSSHLibrary.Extensions
{
    public static class ObjectExt
    {
        #region Public Methods

        public static string LogPrefix(this object source, string name = "", [CallerMemberName] string memberName = "")
        {
            var prefix = new StringBuilder();
            prefix.Append(source.GetType().Name);
            if (!string.IsNullOrEmpty(memberName))
            {
                prefix.Append('.').Append(memberName);
            }
            if (!string.IsNullOrEmpty(name))
            {
                prefix.Append(" '").Append(name).Append("'");
            }
            prefix.Append(": ");
            return prefix.ToString();
        }

        public static string LogAll(this object source, string name = "", [CallerMemberName] string memberName = "")
        {
            var prefix = new StringBuilder();
            prefix.Append(source.GetType().Name);
            if (!string.IsNullOrEmpty(memberName))
            {
                prefix.Append('.').Append(memberName);
            }
            if (!string.IsNullOrEmpty(name))
            {
                prefix.Append(" '").Append(name).Append("'");
            }
            return prefix.ToString();
        }

        #endregion
    }
}
