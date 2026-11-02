#if UNITY_ANDROID
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor.Android;
using UnityEngine;

namespace FarmFuryStampede.EditorTools
{
    /// <summary>
    /// Sets the name under the Android home screen icon to HomeScreenName, as StampedeIosBuild does for iOS. Only the
    /// label changes: productName stays "Farm Fury Stampede" because the editor's save location depends on it.
    /// </summary>
    public class StampedeAndroidBuild : IPostGenerateGradleAndroidProject
    {
        public const string HomeScreenName = "Stampede";

        public int callbackOrder => 100;

        /// <param name="path">The generated unityLibrary folder; the launcher module sits beside it.</param>
        public void OnPostGenerateGradleAndroidProject(string path)
        {
            string strings = Path.Combine(path, "..", "launcher", "src", "main", "res", "values", "strings.xml");
            if (!File.Exists(strings))
            {
                Debug.LogError("[StampedeAndroidBuild] " + strings + " is missing; the home screen name was not set.");
                return;
            }

            string xml = File.ReadAllText(strings);
            string updated = Regex.Replace(xml, "(<string name=\"app_name\">).*?(</string>)",
                "${1}" + HomeScreenName + "${2}");
            if (updated == xml && !xml.Contains(">" + HomeScreenName + "<"))
            {
                Debug.LogError("[StampedeAndroidBuild] No app_name in " + strings + "; the home screen name was not set.");
                return;
            }
            File.WriteAllText(strings, updated);
        }
    }
}
#endif
