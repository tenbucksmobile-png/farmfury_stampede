#if UNITY_IOS
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.iOS.Xcode;

namespace FarmFuryStampede.EditorTools
{
    /// <summary>
    /// Finishes the Xcode project Unity writes for iOS (locally or on Unity Build Automation). The game only talks
    /// HTTPS, which is exempt from US export rules, so Info.plist says so and App Store Connect stops asking about
    /// encryption on every upload.
    /// </summary>
    public class StampedeIosBuild : IPostprocessBuildWithReport
    {
        public int callbackOrder => 100;

        public void OnPostprocessBuild(BuildReport report)
        {
            if (report.summary.platform != BuildTarget.iOS)
            {
                return;
            }

            string plistPath = Path.Combine(report.summary.outputPath, "Info.plist");
            var plist = new PlistDocument();
            plist.ReadFromFile(plistPath);
            plist.root.SetBoolean("ITSAppUsesNonExemptEncryption", false);
            plist.WriteToFile(plistPath);
        }
    }
}
#endif
