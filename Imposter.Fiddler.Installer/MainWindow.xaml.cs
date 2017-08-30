using MahApps.Metro.Controls;
using Microsoft.Win32;
using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;

namespace Imposter.Fiddler.Installer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : MetroWindow
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void Install_Click(object sender, RoutedEventArgs e)
        {
            Install.IsEnabled = false;
            Progress.IsActive = true;

            var installLocation = GetFiddlerLocation();

            if (installLocation == null || !Directory.Exists(installLocation))
            {
                MessageBox.Show("Failed to locate Fiddler2 Installation Location. Please check Fiddler is installed");
                Install.IsEnabled = true;
                Progress.IsActive = false;
                return;
            }

            string[] files = Directory.GetFiles("Scripts\\");

            foreach (string s in files)
            {
                File.Copy(s, Path.Combine(installLocation, Path.GetFileName(s)), true);
            }

            Tabs.SelectedIndex = _tabIndex = 1;
        }

        private string GetFiddlerLocation()
        {
            try
            {
                using (var key = Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Fiddler2\\InstallerSettings"))
                {
                    if (key != null)
                    {
                        Object o = key.GetValue("ScriptPath");
                        if (o != null)
                        {
                            var path = o as string;
                            path = path.Substring(1, path.Length - 2);
                            return path;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // do something with this
            }

            return null;
        }

        private int _tabIndex = 0;

        private void Tabs_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.OriginalSource != Tabs)
            {
                return;
            }

            if (Tabs.SelectedIndex != _tabIndex)
            {
                Tabs.SelectedIndex = _tabIndex;
            }
        }
    }
}
