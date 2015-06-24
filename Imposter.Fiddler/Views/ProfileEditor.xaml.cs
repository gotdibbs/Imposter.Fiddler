using Imposter.Fiddler.Model;
using MahApps.Metro.Controls;
using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;

namespace Imposter.Fiddler.Views
{
    /// <summary>
    /// Interaction logic for ProfileEditor.xaml
    /// </summary>
    public partial class ProfileEditor : MetroWindow
    {
        public Guid ProfileId { get; set; }

        public Profile Profile
        {
            get
            {
                var overrides = new List<Override>(Overrides.ItemsSource as IEnumerable<Override>);
                overrides.RemoveAll(o => string.IsNullOrEmpty(o.LocalFile) || string.IsNullOrEmpty(o.RemoteFile));

                return new Profile
                {
                    ProfileId = ProfileId,
                    Name = Name.Text,
                    LocalDirectory = Local.Text,
                    RemoteUrl = Remote.Text,
                    Overrides = overrides
                };
            }
            set
            {
                ProfileId = value.ProfileId;
                Name.Text = value.Name == "[Dat One Unknown Profile Doe]" ? string.Empty : value.Name;
                Local.Text = value.LocalDirectory;
                Remote.Text = value.RemoteUrl;
                Overrides.ItemsSource = value.Overrides;
            }
        }

        public ProfileEditor(Profile profile = null)
        {
            InitializeComponent();

            if (profile != null)
            {
                Profile = profile;
            }
            else
            {
                ProfileId = Guid.NewGuid();
                Overrides.ItemsSource = new List<Override>();
            }

            Save.Click += Save_Click;
            Cancel.Click += Cancel_Click;
        }


        private void Save_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(Name.Text) || string.IsNullOrEmpty(Local.Text) || string.IsNullOrEmpty(Remote.Text))
            {
                MessageBox.Show("Name, Base Url, Local Directory and Port are required fields. Please fill them in before continuing.");
                return;
            }

            // Check to make sure the directory we're supposed to be serving from actually exists
            if (!Directory.Exists(Local.Text))
            {
                if (MessageBox.Show("The chosen local directory does not exist. Attempt to create it?", string.Empty, MessageBoxButton.OKCancel) == MessageBoxResult.OK)
                {
                    try 
                    {
                        Directory.CreateDirectory(Local.Text);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Failed to create local directory. This is probably a permissions issue. Please correct this issue before continuining.");
                        return;
                    }
                }
                else
                {
                    // Disallow save if the directory is not valid.
                    return;
                }
            }

            DialogResult = true;
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
        }
    }
}
