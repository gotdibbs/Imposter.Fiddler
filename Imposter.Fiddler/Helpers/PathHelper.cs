using Imposter.Fiddler.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;

namespace Imposter.Fiddler.Helpers
{
    public static class PathHelper
    {
        public static string GetStringAfterSubString(string fullString, string subString)
        {
            int index = fullString.IndexOf(subString);
            return fullString.Substring(index + subString.Length);
        }

        public static string GetLocalFilePath(string urlFragment, string localDirectory, List<Override> overrides)
        {
            var path = localDirectory + @"\" + urlFragment.Replace("/", @"\");

            foreach (var ovr in overrides)
            {
                if (ovr != null &&
                    !string.IsNullOrEmpty(ovr.RemoteFile) &&
                    !string.IsNullOrEmpty(ovr.LocalFile) &&
                    urlFragment.Contains(ovr.RemoteFile.ToLower()) &&
                    CheckIfFileExists(ovr.LocalFile))
                {
                    return ovr.LocalFile;
                }
            }

            if (File.Exists(path))
            {
                return path;
            }

            return null;
        }

        public static bool CheckIfFileExists(string file)
        {
            var result = true;

            result = result && File.Exists(file);

            if (!result)
            {
                MessageBox.Show(string.Format("Imposter says: Override file \"{0}\" does not appear to exist on disk.", file));
            }

            return result;
        }
    }
}
