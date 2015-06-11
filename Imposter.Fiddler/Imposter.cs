using Fiddler;
using Imposter.Fiddler.Model;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Threading;

namespace Imposter.Fiddler
{
    public class Imposter : IAutoTamper
    {
        public bool IsEnabled { get; set; }
        public bool EnableAutoReload { get; set; }

        private ImposterSettings _settings = null;
        // TODO: switch to List<Profile>, need to set watcher on the profile maybe? then switch poll for changes to have a parameter
        private Profile _currentProfile = null;

        private FileSystemWatcher _watcher = null;
        private bool _hasChanges = false;

        private ToolStripMenuItem _imposterMenu;
        private ToolStripMenuItem _profiles;
        private ToolStripMenuItem _isEnabled;
        private ToolStripMenuItem _autoReload;

        public Imposter()
        {
            _settings = ImposterSettings.Load();
            InitializeMenu();
        }

        /// <summary>
        /// Handles loading the UI
        /// </summary>
        public void OnLoad()
        {
            FiddlerApplication.UI.MainMenuStrip = new MenuStrip();
            FiddlerApplication.UI.MainMenuStrip.Dock = DockStyle.Top;
            FiddlerApplication.UI.MainMenuStrip.Items.Add(_imposterMenu);
            FiddlerApplication.UI.Controls.Add(FiddlerApplication.UI.MainMenuStrip);
        }

        private void Start()
        {
            if (_currentProfile == null)
            {
                MessageBox.Show("In order to start the proxy, you must first select a profile.");
                return;
            }
            if (!Directory.Exists(_currentProfile.LocalDirectory))
            {
                MessageBox.Show(string.Format("The folder located at '{0}' does not exist. Please correct this error before continuing.", _currentProfile.LocalDirectory));
                return;
            }

            if (_watcher == null)
            {
                _watcher = new FileSystemWatcher();
                _watcher.IncludeSubdirectories = true;
                _watcher.NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.DirectoryName | NotifyFilters.CreationTime;
                _watcher.Filter = "*.*";
                _watcher.Changed += FileWatchUpdate;
                _watcher.Created += FileWatchUpdate;
                _watcher.Deleted += FileWatchUpdate;
                _watcher.Renamed += FileWatchUpdate;
            }
            _watcher.Path = _currentProfile.LocalDirectory;
            _watcher.EnableRaisingEvents = EnableAutoReload;
        }

        private void Stop()
        {
            if (_watcher != null)
            {
                _watcher.EnableRaisingEvents = false;
            }
        }

        #region Menu and Menu Events

        private void InitializeMenu()
        {
            _profiles = new ToolStripMenuItem("&Profiles");

            _isEnabled = new ToolStripMenuItem("&Enabled");
            _isEnabled.Click += Enable_Click;
            _isEnabled.Enabled = false;
            _isEnabled.Checked = IsEnabled;

            _autoReload = new ToolStripMenuItem("&Auto Reload");
            _autoReload.Click += AutoReload_Click;
            _autoReload.Enabled = false;
            _autoReload.Checked = EnableAutoReload;

            var icon = Resources.Resources.Imposter;
            var image = icon.ToBitmap();
            _imposterMenu = new ToolStripMenuItem("Imposter", image);
            _imposterMenu.DropDownItems.Add(_profiles);
            _imposterMenu.DropDownItems.Add(new ToolStripSeparator());
            _imposterMenu.DropDownItems.AddRange(new ToolStripMenuItem[] { _isEnabled, _autoReload });

            LoadProfileItems();
        }

        private void LoadProfileItems(string checkedProfile = null)
        {
            _profiles.DropDownItems.Clear();

            var addNew = new ToolStripMenuItem("Add &New");
            addNew.Click += AddNew_Click;
            _profiles.DropDownItems.Add(addNew);
            _profiles.DropDownItems.Add(new ToolStripSeparator());

            foreach (var profile in _settings.Profiles)
            {
                var item = new ToolStripMenuItem(profile.Name);

                var itemEnable = new ToolStripMenuItem("&Enable");
                itemEnable.Click += ProfileEnable_Click;
                item.DropDownItems.Add(itemEnable);

                var itemEdit = new ToolStripMenuItem("Ed&it");
                itemEdit.Click += ProfileEdit_Click;
                item.DropDownItems.Add(itemEdit);

                var itemDelete = new ToolStripMenuItem("&Delete");
                itemDelete.Click += ProfileDelete_Click;
                item.DropDownItems.Add(itemDelete);

                if (profile.Name == checkedProfile)
                {
                    item.Checked = true;
                    itemEnable.Checked = true;
                }

                _profiles.DropDownItems.Add(item);
            }
        }

        private void Enable_Click(object sender, EventArgs e)
        {
            IsEnabled = _isEnabled.Checked = !_isEnabled.Checked;

            if (IsEnabled)
            {
                Start();
            }
            else
            {
                Stop();
            }
        }

        private void AutoReload_Click(object sender, EventArgs e)
        {
            EnableAutoReload = _autoReload.Checked = !_autoReload.Checked;

            _watcher.EnableRaisingEvents = EnableAutoReload;
        }

        private void AddNew_Click(object sender, EventArgs e)
        {
            var profileEditor = new Views.ProfileEditor();
            var result = profileEditor.ShowDialog();

            if (result == true)
            {
                _settings.Profiles.Add(profileEditor.Profile);
                _settings.Save();

                LoadProfileItems();
            }
        }

        private void ProfileEnable_Click(object sender, EventArgs e)
        {
            var item = (ToolStripMenuItem)sender;
            item.Checked = !item.Checked;
            var parent = item.OwnerItem as ToolStripMenuItem;
            parent.Checked = !parent.Checked;

            _currentProfile = _settings.Profiles.Where(p => p.Name == parent.Text).First();

            if (!IsEnabled)
            {
                _isEnabled.Enabled = true;
                _autoReload.Enabled = true;

                _isEnabled.PerformClick();
            }
        }

        private void ProfileEdit_Click(object sender, EventArgs e)
        {
            var item = (ToolStripMenuItem)sender;
            var parent = item.OwnerItem as ToolStripMenuItem;

            var profile = _settings.Profiles.Where(p => p.Name == parent.Text).First();

            var profileEditor = new Views.ProfileEditor(profile);
            var result = profileEditor.ShowDialog();

            if (result == true)
            {
                _settings.Profiles.Remove(profile);
                _settings.Profiles.Add(profileEditor.Profile);
                _settings.Save();

                if (IsEnabled && item.Checked)
                {
                    _currentProfile = profileEditor.Profile;
                }

                LoadProfileItems(profileEditor.Profile.Name);
            }
        }

        private void ProfileDelete_Click(object sender, EventArgs e)
        {
            var item = (ToolStripMenuItem)sender;
            var parent = item.OwnerItem as ToolStripMenuItem;

            _settings.Profiles = _settings.Profiles.Where(p => p.Name != parent.Text).ToList();
            _settings.Save();

            LoadProfileItems();
        }

        #endregion

        public void AutoTamperRequestBefore(Session oSession)
        {
            if (!IsEnabled)
            {
                return;
            }

            string fullString = oSession.fullUrl.ToLower();
            if (fullString.Contains(_currentProfile.RemoteUrl.ToLower()))
            {
                fullString = GetStringAfterSubString(fullString, _currentProfile.RemoteUrl.ToLower()).Split(new char[] { '?' })[0];
                var path = GetLocalFilePath(fullString);
                if (path != null)
                {
                    oSession.utilCreateResponseAndBypassServer();
                    oSession.LoadResponseFromFile(path);
                    oSession.ResponseHeaders.Add("x-imposter", path);
                    if (oSession.ViewItem != null)
                    {
                        oSession.ViewItem.BackColor = Color.SkyBlue;
                    }
                }
            }
            if (fullString.EndsWith("imposter.js") && EnableAutoReload)
            {
                oSession.utilCreateResponseAndBypassServer();
                var js = Path.GetFullPath("Scripts\\imposter.js");
                oSession.LoadResponseFromFile(js);
                oSession.ResponseHeaders.Add("x-imposter", js);
            }
            if (fullString.Contains("/imposter-poll-for-changes") && EnableAutoReload)
            {
                oSession.utilCreateResponseAndBypassServer();
                oSession.ResponseHeaders.Add("x-imposter", "AUTO RELOAD");
                if (_hasChanges)
                {
                    oSession.utilSetResponseBody("true");
                    _hasChanges = false;
                }
                else
                {
                    oSession.utilSetResponseBody("false");
                }
            }
        }

        private Stream GetResponseStream(string path)
        {
            var destinationStream = new MemoryStream();

            using (var sourceStream = File.OpenRead(path))
            {
                sourceStream.CopyTo(destinationStream);
            }

            return destinationStream;
        }

        public void AutoTamperResponseBefore(Session oSession)
        {
            if (!IsEnabled)
            {
                return;
            }

            var fullString = oSession.fullUrl.ToLower();
            if (fullString.Contains(_currentProfile.RemoteUrl.ToLower()) && EnableAutoReload)
            {
                oSession.utilDecodeResponse();
                bool replaced = oSession.utilReplaceInResponse("</body>", "<script type='text/javascript' src='imposter.js'></script></body>");
            }
        }

        private string GetFileNames(string[] paths)
        {
            paths = paths
                .Select(path => Path.GetFileName(path))
                .ToArray();

            return string.Join(", ", paths);
        }

        private void FileWatchUpdate(object sender, FileSystemEventArgs e)
        {
            _hasChanges = true;
        }

        #region Helpers

        private string GetStringAfterSubString(string fullString, string subString)
        {
            int index = fullString.IndexOf(subString);
            return fullString.Substring(index + subString.Length);
        }

        private string GetLocalFilePath(string urlFragment)
        {
            var path = _currentProfile.LocalDirectory + @"\" + urlFragment.Replace("/", @"\");

            if (File.Exists(path))
            {
                return path;
            }

            foreach (var ovr in _currentProfile.Overrides)
            {
                if (urlFragment.Contains(ovr.RemoteFile.ToLower()) && CheckIfFileExists(ovr.LocalFile))
                {
                    return ovr.LocalFile;
                }
            }

            return null;
        }

        private bool CheckIfFileExists(string file)
        {
            var result = true;

            result = result && File.Exists(file);

            if (!result)
            {
                MessageBox.Show(string.Format("File \"{0}\" does not appear to exist on disk.", file));
            }

            return result;
        }

        #endregion Helpers

        #region Not Implemented

        public void AutoTamperRequestAfter(Session oSession)
        {
            return;
        }

        public void AutoTamperResponseAfter(Session oSession)
        {
            return;
        }

        public void OnBeforeReturningError(Session oSession)
        {
            return;
        }

        public void OnBeforeUnload()
        {
            return;
        }

        #endregion
    }
}
