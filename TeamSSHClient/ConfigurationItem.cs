namespace TeamSSHClient
{
    internal class ConfigurationItem
    {
        #region Properties

        public int ConnectionId { get; set; }
        public int LocalPort { get; set; }
        public string LocalUri { get; set; }
        public string ServerUri { get; set; }

        #endregion        
    }
}
