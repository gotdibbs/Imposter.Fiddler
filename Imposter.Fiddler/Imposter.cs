using Fiddler;
using GitHubUpdate;
using Imposter.Fiddler.Model;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
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

        private string _version = null;

        private ImposterSettings _settings = null;
        private List<Profile> _enabledProfiles = null;

        private ToolStripMenuItem _imposterMenu;
        private ToolStripMenuItem _profiles;
        private ToolStripMenuItem _isEnabled;
        private ToolStripMenuItem _autoReload;

        public Imposter()
        {
            _version = Assembly.GetExecutingAssembly().GetName().Version.ToString();
            _version = _version.Substring(0, _version.LastIndexOf("."));

            _enabledProfiles = new List<Profile>();
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

            CheckForUpdates();
        }

        private async void CheckForUpdates()
        {
            var checker = new UpdateChecker("gotdibbs", "Imposter.Fiddler", _version);

            var update = await checker.CheckUpdate();

            if (update != UpdateType.None)
            {
                var result = new UpdateNotifyDialog(checker).ShowDialog();

                if (result == DialogResult.Yes)
                {
                    MessageBox.Show("Nope");
                }
            }
        }

        private void Start()
        {
            if (_enabledProfiles == null || _enabledProfiles.Count == 0)
            {
                MessageBox.Show("In order to start the proxy, you must first select a profile.");
                return;
            }
            else
            {
                foreach (var profile in _enabledProfiles)
                {
                    if (!profile.IsRunning)
                    {
                        profile.Start(EnableAutoReload);
                    }
                }

                _imposterMenu.Text = "Imposter (Enabled)";
            }
        }

        private void Stop()
        {
            foreach (var profile in _enabledProfiles)
            {
                profile.Stop();
            }

            _imposterMenu.Text = "Imposter";
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

            var version = new ToolStripMenuItem(string.Format("v{0}", _version));
            version.Enabled = false;

            var icon = Resources.Resources.Imposter;
            var image = icon.ToBitmap();
            _imposterMenu = new ToolStripMenuItem("&Imposter", image);
            _imposterMenu.DropDownItems.Add(_profiles);
            _imposterMenu.DropDownItems.Add(new ToolStripSeparator());
            _imposterMenu.DropDownItems.AddRange(new ToolStripMenuItem[] { _isEnabled, _autoReload });
            _imposterMenu.DropDownItems.Add(new ToolStripSeparator());
            _imposterMenu.DropDownItems.Add(version);

            LoadProfileItems();
        }

        private void LoadProfileItems(string checkedProfile = null)
        {
            _profiles.DropDownItems.Clear();

            var addNew = new ToolStripMenuItem("Add &New");
            addNew.Click += AddNew_Click;
            _profiles.DropDownItems.Add(addNew);
            _profiles.DropDownItems.Add(new ToolStripSeparator());

            foreach (var profile in _settings.Profiles.OrderBy(p => p.Name))
            {
                var item = new ToolStripMenuItem(profile.Name);
                item.Tag = profile.ProfileId;

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

            foreach (var profile in _enabledProfiles)
            {
                profile.EnableWatcher = EnableAutoReload;
            }
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

            if (item.Checked)
            {
                IsEnabled = true;
                _isEnabled.Checked = true;
                _isEnabled.Enabled = true;
                _autoReload.Enabled = true;

                _enabledProfiles.Add(_settings.Profiles.Where(p => p.ProfileId == (Guid)parent.Tag).First());

                Start();
            }
            else
            {
                var profile = _enabledProfiles.Where(p => p.ProfileId == (Guid)parent.Tag).First();
                profile.Stop();

                _enabledProfiles.Remove(profile);

                if (_enabledProfiles.Count == 0)
                {
                    IsEnabled = false;
                    _isEnabled.Checked = false;
                    _isEnabled.Enabled = false;
                    _autoReload.Enabled = false;

                    Stop();
                }
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

                if (IsEnabled && parent.Checked)
                {
                    _enabledProfiles.RemoveAll(p => p.ProfileId == (Guid)parent.Tag);
                    _enabledProfiles.Add(profileEditor.Profile);

                    LoadProfileItems(profileEditor.Profile.Name);
                }
                else
                {
                    LoadProfileItems();
                }
            }
        }

        private void ProfileDelete_Click(object sender, EventArgs e)
        {
            var item = (ToolStripMenuItem)sender;
            var parent = item.OwnerItem as ToolStripMenuItem;

            var profile = _settings.Profiles.Where(p => p.ProfileId == (Guid)parent.Tag).First();

            // If the profile is enabled
            if (_enabledProfiles.Contains(profile))
            {
                // Stop it if it is running
                if (profile.IsRunning)
                {
                    profile.Stop();
                }

                // Remove it from the active list
                _enabledProfiles.Remove(profile);

                // If there are no other active profiles, call it a day
                if (_enabledProfiles.Count == 0)
                {
                    IsEnabled = false;
                    _isEnabled.Checked = false;
                    _isEnabled.Enabled = false;
                    _autoReload.Enabled = false;

                    Stop();
                }
            }

            // Remove the item from the menu
            parent.Dispose();

            _settings.Profiles = _settings.Profiles.Where(p => p.ProfileId != (Guid)parent.Tag).ToList();
            _settings.Save();
        }

        #endregion

        public void AutoTamperRequestBefore(Session oSession)
        {
            if (!IsEnabled)
            {
                return;
            }

            bool isTampered = false;

            string fullString = oSession.fullUrl.ToLower();

            if (fullString.EndsWith("imposter.js") && EnableAutoReload)
            {
                oSession.utilCreateResponseAndBypassServer();
                var js = Path.GetFullPath("Scripts\\imposter.js");
                oSession.LoadResponseFromFile(js);
                oSession.ResponseHeaders.Add("x-imposter", js);

                isTampered = true;
            }

            if (fullString.ToLower().Contains("/imposter-poll-for-changes?profileid=") && EnableAutoReload)
            {
                var profileIdIndex = fullString.ToLower().IndexOf("/imposter-poll-for-changes?profileid=");
                var profileIdFragment = fullString.Substring(profileIdIndex + "/imposter-poll-for-changes?profileid=".Length);

                Guid profileId;
                var success = Guid.TryParse(profileIdFragment, out profileId);

                oSession.utilCreateResponseAndBypassServer();
                oSession.ResponseHeaders.Add("x-imposter", "AUTO RELOAD");

                if (success && _enabledProfiles.Any(p => p.ProfileId == profileId && p.HasChanges))
                {
                    oSession.utilSetResponseBody("true");
                    _enabledProfiles.ForEach(p => p.HasChanges = false);
                }
                else
                {
                    oSession.utilSetResponseBody("false");
                }

                isTampered = true;
            }

            foreach (var profile in _enabledProfiles)
            {
                var path = profile.GetFileMatch(fullString);

                if (path == null)
                {
                    continue;
                }

                oSession.utilCreateResponseAndBypassServer();
                oSession.LoadResponseFromFile(path);
                oSession.ResponseHeaders.Add("x-imposter", path);
                if (oSession.ViewItem != null)
                {
                    oSession["ui-backcolor"] = "#4683ea";
                    oSession["ui-color"] = "#ffffff";
                }

                isTampered = true;

                // Only swap for the first match
                break;
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
            if (!IsEnabled || !EnableAutoReload)
            {
                return;
            }

            var fullString = oSession.fullUrl.ToLower();

            foreach (var profile in _enabledProfiles)
            {
                if (fullString.Contains(profile.RemoteUrl.ToLower()))
                {
                    oSession.utilDecodeResponse();
                    bool replaced = oSession.utilReplaceInResponse("</body>", string.Format(
                        @"<script type='text/javascript'>window.__IMPOSTER = {{ profileId: '{0}' }};</script>
                          <script type='text/javascript' src='imposter.js'></script>
                          </body>", profile.ProfileId));

                    break;
                }
            }
        }

        private string GetFileNames(string[] paths)
        {
            paths = paths
                .Select(path => Path.GetFileName(path))
                .ToArray();

            return string.Join(", ", paths);
        }

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
