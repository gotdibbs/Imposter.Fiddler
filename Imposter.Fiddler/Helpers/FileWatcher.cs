using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Imposter.Fiddler.Helpers
{
    public class FileWatcher : IDisposable
    {
        private FileSystemWatcher _watcher;

        public bool EnableRaisingEvents
        {
            get { return _watcher.EnableRaisingEvents; }
            set { _watcher.EnableRaisingEvents = value; }
        }

        public FileSystemEventHandler Handler
        {
            set
            {
                _watcher.Changed += value;
                _watcher.Created += value;
                _watcher.Deleted += value;
            }
        }

        public FileWatcher(string path, bool enabled)
        {
            if (_watcher == null)
            {
                _watcher = new FileSystemWatcher();
                _watcher.IncludeSubdirectories = true;
                _watcher.NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.DirectoryName | NotifyFilters.CreationTime;
                _watcher.Filter = "*.*";
            }
            _watcher.Path = path;
            _watcher.EnableRaisingEvents = enabled;
        }

        public void Start()
        {
            _watcher.EnableRaisingEvents = true;
        }

        public void Stop()
        {
            _watcher.EnableRaisingEvents = false;
        }

        public void Dispose()
        {
            if (_watcher != null)
            {
                _watcher.Dispose();
            }
        }
    }
}
