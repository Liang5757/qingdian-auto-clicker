using System;
using System.IO;
using System.Xml;
using System.Xml.Serialization;

namespace WindowsAutoClicker
{
    internal sealed class SettingsStore
    {
        private readonly string path;
        private static readonly XmlSerializer Serializer = new XmlSerializer(typeof(Settings));

        public SettingsStore(string path = null)
        {
            this.path = path ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "WindowsAutoClicker", "settings.xml");
        }

        public Settings Load(out string warning)
        {
            warning = null;
            try
            {
                if (!File.Exists(path)) return new Settings();
                var options = new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null, MaxCharactersInDocument = 65536 };
                using (var reader = XmlReader.Create(path, options))
                {
                    var settings = Serializer.Deserialize(reader) as Settings;
                    if (settings == null) throw new InvalidOperationException("Empty settings document.");
                    settings.Validate();
                    return settings;
                }
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException || ex is InvalidOperationException || ex is XmlException)
            {
                warning = "配置读取失败，已使用默认设置。";
                return new Settings();
            }
        }

        public bool TrySave(Settings settings, out string warning)
        {
            warning = null;
            string temporary = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path)));
                using (var stream = File.Create(temporary)) Serializer.Serialize(stream, settings);
                if (File.Exists(path)) File.Replace(temporary, path, null);
                else File.Move(temporary, path);
                return true;
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException || ex is InvalidOperationException)
            {
                warning = "配置保存失败，本次设置仍可使用。请检查配置目录权限。";
                return false;
            }
            finally
            {
                try { if (File.Exists(temporary)) File.Delete(temporary); }
                catch (IOException) { }
                catch (UnauthorizedAccessException) { }
            }
        }
    }
}
