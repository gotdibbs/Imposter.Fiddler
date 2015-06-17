using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Windows;

namespace Imposter.Fiddler.Model
{
    [DataContract]
    public class ImposterSettings
    {
        private const string FILE_NAME = "settings.json";
        public static string FOLDER_PATH = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Imposter.Fiddler");
        private static string FILE_PATH = Path.Combine(FOLDER_PATH, FILE_NAME);

        [DataMember(Name = "version")]
        public string Version { get; set; }

        [DataMember(Name = "profiles")]
        public List<Profile> Profiles { get; set; }

        public static ImposterSettings Load()
        {
            try
            {
                if (!Directory.Exists(FOLDER_PATH))
                {
                    Directory.CreateDirectory(FOLDER_PATH);
                }

                if (File.Exists(FILE_PATH))
                {
                    return Read();
                }
                else
                {
                    try
                    {
                        var profileStub = string.Join(string.Empty,
                            "{ \"profiles\": [], \"version\": \"",
                            Assembly.GetCallingAssembly().GetName().Version.ToString(),
                            "\" }");

                        File.WriteAllText(FILE_PATH, profileStub);

                        return Load();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("A problem was encountered while attempting to create the settings file. Detail: " + ex.Message);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("A problem was encountered while attempting to load settings. Detail: " + ex.Message);
            }

            return null;
        }

        private static ImposterSettings Read()
        {
            var settingsJson = File.ReadAllText(FILE_PATH);

            var json = new DataContractJsonSerializer(typeof(ImposterSettings));

            using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(settingsJson)))
            {
                var settings = (Model.ImposterSettings)json.ReadObject(stream);

                return Upgrade(settings);
            }
        }

        private static ImposterSettings Upgrade(ImposterSettings settings)
        {
            if (settings == null)
            {
                return settings;
            }

            if (string.IsNullOrEmpty(settings.Version))
            {
                var result = MessageBox.Show("Imposter says: Invalid version found in settings.json. Drop all settings and recreate?", 
                    string.Empty, MessageBoxButton.YesNo);

                if (result == MessageBoxResult.Yes)
                {
                    File.Delete(FILE_PATH);

                    return Load();
                }

                return null;
            }
            else
            {
                // We're up to date
                return settings;
            }
        }

        public void Save()
        {
            try
            {
                var serializer = new DataContractJsonSerializer(this.GetType());

                using (var ms = new MemoryStream())
                {
                    serializer.WriteObject(ms, this);
                    byte[] json = ms.ToArray();
                    File.WriteAllText(FILE_PATH, Encoding.UTF8.GetString(json, 0, json.Length));
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("A problem was encountered while attempting to save settings. Detail: " + ex.Message);
            }
        }
    }
}
