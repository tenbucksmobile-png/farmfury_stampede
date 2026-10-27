#if UNITY_IOS
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.iOS.Xcode;
using UnityEngine;

namespace FarmFuryStampede.EditorTools
{
    /// <summary>
    /// Finishes the Xcode project Unity writes for iOS (locally or on Unity Build Automation). Ported from Arcade's
    /// IOSPostProcessBuild, where each of these cost a build cycle on its first TestFlight attempt:
    /// - Info.plist: ITSAppUsesNonExemptEncryption = NO (the game only talks HTTPS, exempt from US export rules, so
    ///   App Store Connect stops asking about encryption on every upload) and GADApplicationIdentifier, the AdMob
    ///   App ID. Google Mobile Ads (in the build as LevelPlay's AdMob adapter) throws at launch without it, before
    ///   Unity even starts - whether or not ads are switched on.
    /// - Removes -ld64 from Other Linker Flags: Unity still adds it and Xcode 26's linker asserts on it, failing the
    ///   archive. A no-op once Unity stops adding it.
    /// - Adds the app's own privacy manifest (Assets/_Project/iOS/PrivacyInfo.xcprivacy: UserDefaults, reason
    ///   CA92.1, for PlayerPrefs), without which Apple rejects the upload. The SDKs bring their own manifests.
    /// </summary>
    public class StampedeIosBuild : IPostprocessBuildWithReport
    {
        /// <summary>AdMob App ID for Farm Fury Stampede (iOS), from admob.google.com > Apps > App settings. Not the
        /// LevelPlay app key or the ad unit ids, which live on AdManager in the scene / in the LevelPlay dashboard.</summary>
        private const string AdMobAppId = "ca-app-pub-1264425755955045~7117772728";

        public int callbackOrder => 100;

        public void OnPostprocessBuild(BuildReport report)
        {
            if (report.summary.platform != BuildTarget.iOS)
            {
                return;
            }

            string outputPath = report.summary.outputPath;

            string projectPath = PBXProject.GetPBXProjectPath(outputPath);
            var project = new PBXProject();
            project.ReadFromFile(projectPath);
            string mainTarget = project.GetUnityMainTargetGuid();
            foreach (string target in new[] { mainTarget, project.GetUnityFrameworkTargetGuid() })
            {
                project.UpdateBuildProperty(target, "OTHER_LDFLAGS", new string[0], new[] { "-ld64" });
            }
            AddPrivacyManifest(project, mainTarget, outputPath);
            File.WriteAllText(projectPath, project.WriteToString());

            string plistPath = Path.Combine(outputPath, "Info.plist");
            var plist = new PlistDocument();
            plist.ReadFromFile(plistPath);
            plist.root.SetBoolean("ITSAppUsesNonExemptEncryption", false);
            plist.root.SetString("GADApplicationIdentifier", AdMobAppId);
            plist.WriteToFile(plistPath);
        }

        private static void AddPrivacyManifest(PBXProject project, string mainTarget, string outputPath)
        {
            string source = Path.Combine(Application.dataPath, "_Project", "iOS", "PrivacyInfo.xcprivacy");
            if (!File.Exists(source))
            {
                Debug.LogWarning("[StampedeIosBuild] " + source + " is missing; the build has no app privacy manifest.");
                return;
            }

            const string fileName = "PrivacyInfo.xcprivacy";
            File.Copy(source, Path.Combine(outputPath, fileName), true);
            project.AddFileToBuild(mainTarget, project.AddFile(fileName, fileName, PBXSourceTree.Source));
        }
    }
}
#endif
