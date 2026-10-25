using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace FarmFuryStampede.EditorTools
{
    /// <summary>
    /// Player settings both stores require, pinned so they can't drift (audit 2026-10-25): IL2CPP for Android (Google
    /// Play only takes 64-bit builds, and ARM64 needs IL2CPP; the project had no backend set, leaving it to the
    /// editor's default) with ARM64 as the target; iOS is always IL2CPP. Applied on every editor load and checked again
    /// before each build, which also warns about what would get a store build rejected: an Android release build
    /// signed with the debug key.
    /// </summary>
    [InitializeOnLoad]
    public class StampedeBuildSettings : IPreprocessBuildWithReport
    {
        static StampedeBuildSettings()
        {
            EditorApplication.delayCall += Apply;   // after the editor has finished loading the project
        }

        public int callbackOrder => -100;

        public static void Apply()
        {
            if (PlayerSettings.GetScriptingBackend(NamedBuildTarget.Android) != ScriptingImplementation.IL2CPP)
            {
                PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
                Debug.Log("[BuildSettings] Android scripting backend set to IL2CPP.");
            }
            if ((PlayerSettings.Android.targetArchitectures & AndroidArchitecture.ARM64) == 0)
            {
                PlayerSettings.Android.targetArchitectures |= AndroidArchitecture.ARM64;
                Debug.Log("[BuildSettings] Android ARM64 target added.");
            }
            if (PlayerSettings.GetScriptingBackend(NamedBuildTarget.iOS) != ScriptingImplementation.IL2CPP)
            {
                PlayerSettings.SetScriptingBackend(NamedBuildTarget.iOS, ScriptingImplementation.IL2CPP);
            }
        }

        public void OnPreprocessBuild(BuildReport report)
        {
            Apply();
            bool development = (report.summary.options & BuildOptions.Development) != 0;
            if (report.summary.platform == BuildTarget.Android && !development && !PlayerSettings.Android.useCustomKeystore)
            {
                Debug.LogWarning("[BuildSettings] Android release build without a custom keystore: it is signed with the " +
                                 "debug key, which Google Play rejects. Set one in Player Settings > Publishing Settings.");
            }
        }
    }
}
