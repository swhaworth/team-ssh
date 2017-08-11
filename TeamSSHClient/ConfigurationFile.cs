using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using TeamSSHLibrary.Helpers;

namespace TeamSSHClient
{
    internal sealed class ConfigurationFile
    {
        #region Fields

        private JObject _file;

        #endregion

        #region Ctors

        private ConfigurationFile(string fileName, ArgumentHandler arguments)
        {
            this.Arguments = arguments;
            this.FileName = fileName;
        }

        #endregion

        #region Properties

        public bool AddConsoleLogger => this.GetBoolConfigurationItem(nameof(ConfigurationFile.AddConsoleLogger), true);
        public bool AddDebugLogger => this.GetBoolConfigurationItem(nameof(ConfigurationFile.AddDebugLogger), true);
        public ArgumentHandler Arguments { get; }
        public IEnumerable<ConfigurationItem> Clients => this.GetClients();
        public static string ConfigurationFileName => ".team-ssh.json";
        public int ConnectionId => this.GetIntConfigurationItem(nameof(ConfigurationFile.ConnectionId), "--id", 1);
        public string FileName { get; }
        public int LocalPort => this.GetIntConfigurationItem(nameof(ConfigurationFile.LocalPort), "--lport", 22);
        public string LocalUri => this.GetStringConfigurationItem(nameof(ConfigurationFile.LocalUri), "--luri", IPAddress.Loopback.ToString());
        public string LoggerCategoryName => this.GetStringConfigurationItem(nameof(ConfigurationFile.LoggerCategoryName), "TeamSSHClient");
        public IEnumerable<ConfigurationItem> Servers => this.GetServers();
        public int ServerPort => this.GetIntConfigurationItem(nameof(ConfigurationFile.ServerPort), "--sport", 10022);
        public string ServerUri => this.GetStringConfigurationItem(nameof(ConfigurationFile.ServerUri), "--suri", string.Empty);

        #endregion        

        #region Public Methods

        public void AddClient(ConfigurationItem client)
        {
            var file = this.Load();
            var clientArray = (JArray)file[nameof(ConfigurationFile.Clients)];
            if (clientArray == null)
            {
                file.Add(nameof(ConfigurationFile.Clients), new JArray());
                clientArray = (JArray)file[nameof(ConfigurationFile.Clients)];
            }
            for (var c = 0; c < clientArray.Count; ++c)
            {
                var clientInfo = clientArray[c].ToObject<ConfigurationItem>();
                if (clientInfo.ConnectionId == client.ConnectionId)
                {
                    clientArray.RemoveAt(c);
                }
            }
            clientArray.Add(JObject.FromObject(client));
            this.Save();
        }

        public void AddServer(ConfigurationItem server)
        {
            var file = this.Load();
            var serverArray = (JArray)file[nameof(ConfigurationFile.Servers)];
            if (serverArray == null)
            {
                file.Add(nameof(ConfigurationFile.Servers), new JArray());
                serverArray = (JArray)file[nameof(ConfigurationFile.Servers)];
            }
            for (var c = 0; c < serverArray.Count; ++c)
            {
                var serverInfo = serverArray[c].ToObject<ConfigurationItem>();
                if (serverInfo.ConnectionId == server.ConnectionId)
                {
                    serverArray.RemoveAt(c);
                }
            }
            serverArray.Add(JObject.FromObject(server));
            this.Save();
        }

        public static ConfigurationFile Load(ArgumentHandler arguments)
        {
            var configArgument = arguments.Find("--config");
            if (!string.IsNullOrEmpty(configArgument))
            {
                return new ConfigurationFile(Path.GetFullPath(configArgument), arguments);
            }
            return new ConfigurationFile(Path.Combine(OperatingSystemHelpers.GetHomeDirectory(), ConfigurationFile.ConfigurationFileName), arguments);
        }

        public void RemoveClient(int connectionId)
        {
            var file = this.Load();
            var clientArray = (JArray)file[nameof(ConfigurationFile.Clients)];
            if (clientArray == null)
            {
                return;
            }
            for (var c = 0; c < clientArray.Count; ++c)
            {
                var clientInfo = clientArray[c].ToObject<ConfigurationItem>();
                if (clientInfo.ConnectionId == connectionId)
                {
                    clientArray.RemoveAt(c);
                    this.Save();
                    return;
                }
            }
        }

        public void RemoveServer(int connectionId)
        {
            var file = this.Load();
            var serverArray = (JArray)file[nameof(ConfigurationFile.Servers)];
            if (serverArray == null)
            {
                return;
            }
            for (var c = 0; c < serverArray.Count; ++c)
            {
                var serverInfo = serverArray[c].ToObject<ConfigurationItem>();
                if (serverInfo.ConnectionId == connectionId)
                {
                    serverArray.RemoveAt(c);
                    this.Save();
                    return;
                }
            }
        }

        public void Save()
        {
            var tempFile = Path.GetTempFileName();
            try
            {
                var file = this.Load();
                File.WriteAllText(tempFile, file.ToString(Formatting.Indented));
                File.Copy(tempFile, this.FileName, true);
            }
            finally
            {
                ExceptionHelpers.Wrap(() => File.Delete(tempFile));
            }
        }

        #endregion

        #region Private Methods

        private bool GetBoolConfigurationItem(string name, bool defaultValue)
        {
            var file = this.Load();
            var token = file[name];
            if (token == null)
            {
                return defaultValue;
            }
            return token.ToObject<bool>();
        }

        private bool GetBoolConfigurationItem(string name, string argumentName, bool defaultValue)
        {
            var argumentValue = this.Arguments.GetBoolArgument(argumentName);
            if (argumentValue.HasValue)
            {
                return argumentValue.Value;
            }
            return this.GetBoolConfigurationItem(name, defaultValue);
        }

        private IEnumerable<ConfigurationItem> GetClients()
        {
            var file = this.Load();
            var token = file[nameof(ConfigurationFile.Clients)];
            if (token == null)
            {
                return Enumerable.Empty<ConfigurationItem>();
            }
            return token.ToObject<ConfigurationItem[]>();
        }

        private int GetIntConfigurationItem(string name, int defaultValue)
        {
            var file = this.Load();
            var token = file[name];
            if (token == null)
            {
                return defaultValue;
            }
            return token.ToObject<int>();
        }

        private int GetIntConfigurationItem(string name, string argumentName, int defaultValue)
        {
            var argumentValue = this.Arguments.GetIntArgument(argumentName);
            if (argumentValue.HasValue)
            {
                return argumentValue.Value;
            }
            return this.GetIntConfigurationItem(name, defaultValue);
        }

        private IEnumerable<ConfigurationItem> GetServers()
        {
            var file = this.Load();
            var token = file[nameof(ConfigurationFile.Servers)];
            if (token == null)
            {
                return Enumerable.Empty<ConfigurationItem>();
            }
            return token.ToObject<ConfigurationItem[]>();
        }

        private string GetStringConfigurationItem(string name, string defaultValue)
        {
            var file = this.Load();
            var token = file[name];
            if (token == null)
            {
                return defaultValue;
            }
            return token.ToObject<string>();
        }

        private string GetStringConfigurationItem(string name, string argumentName, string defaultValue)
        {
            var argumentValue = this.Arguments.GetStringArgument(argumentName);
            if (!string.IsNullOrEmpty(argumentValue))
            {
                return argumentValue;
            }
            return this.GetStringConfigurationItem(name, defaultValue);
        }

        private JObject Load()
        {
            if (_file == null)
            {
                try
                {
                    _file = JObject.Parse(File.ReadAllText(this.FileName));
                }
                catch (Exception)
                {
                    _file = new JObject();
                }
            }
            return _file;
        }

        #endregion
    }
}
