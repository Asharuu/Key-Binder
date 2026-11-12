using System;
using System.IO;
using System.Xml.Serialization;
using SafeKeyBinder.Models;

namespace SafeKeyBinder.Services
{
    public class ConfigService
    {
        private readonly string _configFilePath;

        public ConfigService(string configFilePath)
        {
            _configFilePath = configFilePath;
        }

        public string ConfigFilePath
        {
            get { return _configFilePath; }
        }

        public AppConfig Load()
        {
            if (!File.Exists(_configFilePath))
            {
                AppConfig defaults = AppConfig.CreateDefault();
                Save(defaults);
                return defaults;
            }

            try
            {
                XmlSerializer serializer = new XmlSerializer(typeof(AppConfig));
                using (FileStream stream = new FileStream(_configFilePath, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    AppConfig loaded = serializer.Deserialize(stream) as AppConfig;
                    if (loaded != null)
                    {
                        if (loaded.Keybinds == null)
                        {
                            loaded.Keybinds = new System.Collections.Generic.List<KeybindItem>();
                        }
                        if (loaded.WindowFilterKeyword == null)
                        {
                            loaded.WindowFilterKeyword = string.Empty;
                        }
                        if (loaded.AutoSequence == null)
                        {
                            loaded.AutoSequence = new AutoSequenceConfig();
                        }
                        if (loaded.ManualPrefixDelayMs <= 0)
                        {
                            loaded.ManualPrefixDelayMs = 80;
                        }
                        return loaded;
                    }
                }
            }
            catch
            {
                // Fall back to defaults cleanly if config file was corrupted
            }

            return AppConfig.CreateDefault();
        }

        public void Save(AppConfig config)
        {
            if (config == null) return;

            string dir = Path.GetDirectoryName(_configFilePath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            string tempPath = _configFilePath + ".tmp";
            XmlSerializer serializer = new XmlSerializer(typeof(AppConfig));
            using (FileStream stream = new FileStream(tempPath, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                serializer.Serialize(stream, config);
            }

            if (File.Exists(_configFilePath))
            {
                File.Delete(_configFilePath);
            }
            File.Move(tempPath, _configFilePath);
        }
    }
}
