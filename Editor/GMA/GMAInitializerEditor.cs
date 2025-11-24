using CustomAds.GMA;
using CustomAds.Utils;
using System.Linq;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(GMAInitializer))]
public class GMAInitializerEditor : Editor {
    public override void OnInspectorGUI() {
#if GMA_DEPENDENCIES_INSTALLED
        if (GUILayout.Button("Remove GMA Define")) {
            Utils.RemoveDefine("GMA_DEPENDENCIES_INSTALLED");
        }
#else
        // Check if GMA package exists by checking for assembly or namespace
        bool hasGmaPackage = false;

        // Method 1: Check for assembly file
        var assemblies = System.AppDomain.CurrentDomain.GetAssemblies();
        foreach (var assembly in assemblies) {
            if (assembly.GetName().Name.Contains("GoogleMobileAds")) {
                hasGmaPackage = true;
                break;
            }
        }

        if (!hasGmaPackage) {
            // Check registry
            bool hasOpenUpm = false;
            var allRegisteredPackages = UnityEditor.PackageManager.PackageInfo.GetAllRegisteredPackages();
            if (allRegisteredPackages != null) {
                hasOpenUpm = allRegisteredPackages.Any(p =>
                    p.registry != null && p.registry.url == "https://package.openupm.com");
            }

            if (!hasOpenUpm) {
                EditorGUILayout.HelpBox("GMA dependencies not detected. Please install Google Mobile Ads SDK via Package Manager or UPM. Go to Edit > Project Settings > Package Manager to add the OpenUPM registry.", MessageType.Warning);
                EditorGUILayout.TextField("Name: ", "Google");
                EditorGUILayout.TextField("URL: ", "https://package.openupm.com");
                EditorGUILayout.TextField("Scope(s): ", "com.google");
                if(GUILayout.Button("Open Package Manager Settings")) {
                    SettingsService.OpenProjectSettings("Project/Package Manager");
                }
                return;
            }

            EditorGUILayout.HelpBox("Next Add Package: com.google.ads.mobile", MessageType.Warning);
            if (GUILayout.Button("Add GMA Package")) {
                UnityEditor.PackageManager.Client.Add("com.google.ads.mobile");
            }
            return;
        }

        EditorGUILayout.HelpBox("GMA package detected! Click below to add the scripting define.", MessageType.Info);
        if (GUILayout.Button("Add GMA Define")) {
            Utils.AddDefine("GMA_DEPENDENCIES_INSTALLED");
        }
#endif
        DrawDefaultInspector();
    }
}
