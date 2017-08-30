using Imposter.Fiddler.Helpers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Windows.Forms;

namespace Imposter.Fiddler.Model
{
    [DataContract]
    public class Profile
    {
        private FileWatcher _watcher;

        [IgnoreDataMember]
        public bool EnableWatcher
        {
            get { return _watcher.EnableRaisingEvents; }
            set { _watcher.EnableRaisingEvents = value; }
        }

        [IgnoreDataMember]
        public bool HasChanges { get; set; }

        [IgnoreDataMember]
        public bool IsRunning { get; set; }

        [DataMember(Name = "profileId")]
        public Guid ProfileId { get; set; }

        [DataMember(Name = "name")]
        public string Name { get; set; }

        [DataMember(Name = "localDirectory")]
        public string LocalDirectory { get; set; }

        [DataMember(Name = "remoteUrl")]
        public string RemoteUrl { get; set; }

        [DataMember(Name = "overrides")]
        public List<Override> Overrides { get; set; }

        public override string ToString()
        {
            return Name.ToString();
        }

        public void Start(bool enableAutoReload)
        {
            if (!Directory.Exists(LocalDirectory))
            {
                MessageBox.Show($"Profile '{this.Name}' has an invalid directory selected. Please correct this issue.");
                return;
            }

            IsRunning = true;

            _watcher = new FileWatcher(LocalDirectory, enableAutoReload);
            _watcher.Handler = FileWatchUpdate;
        }

        public void Stop()
        {
            IsRunning = false;

            if (_watcher != null)
            {
                _watcher.Dispose();
                _watcher = null;
            }
        }

        public string GetFileMatch(string url)
        {
            if (url.Contains(RemoteUrl.ToLower()))
            {
                url = PathHelper.GetStringAfterSubString(url, RemoteUrl.ToLower()).Split(new char[] { '?' })[0];

                var path = PathHelper.GetLocalFilePath(url, LocalDirectory, Overrides);

                return path;
            }

            return null;
        }

        private void FileWatchUpdate(object sender, FileSystemEventArgs e)
        {
            HasChanges = true;
        }
    }
}
