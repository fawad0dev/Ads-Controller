using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;
namespace CustomAds.Utils {
    /// <summary>
    /// Utility functions for editor operations
    /// </summary>
    public class Utils {
        public static void AddDefine(string define) {
#if UNITY_6000_0_OR_NEWER
            var buildTarget = NamedBuildTarget.FromBuildTargetGroup(
                EditorUserBuildSettings.selectedBuildTargetGroup
            );
            var defines = PlayerSettings.GetScriptingDefineSymbols(buildTarget);
            if (!defines.Contains(define)) {
                PlayerSettings.SetScriptingDefineSymbols(buildTarget, defines + ";" + define);
            }
#else
        var defines = PlayerSettings.GetScriptingDefineSymbolsForGroup(
            EditorUserBuildSettings.selectedBuildTargetGroup
        );
        if (!defines.Contains(define))
        {
            PlayerSettings.SetScriptingDefineSymbolsForGroup(
                EditorUserBuildSettings.selectedBuildTargetGroup,
                defines + ";" + define
            );
        }
#endif
        }

        public static void RemoveDefine(string define) {
#if UNITY_6000_0_OR_NEWER
            var buildTarget = NamedBuildTarget.FromBuildTargetGroup(
                EditorUserBuildSettings.selectedBuildTargetGroup
            );
            var defines = PlayerSettings.GetScriptingDefineSymbols(buildTarget);
            if (!string.IsNullOrEmpty(defines) && defines.Contains(define)) {
                var updated = defines.Replace(";" + define, "").Replace(define + ";", "").Replace(define, "");
                // Normalize accidental double semicolons and trim edges
                while (updated.Contains(";;")) updated = updated.Replace(";;", ";");
                updated = updated.Trim(';', ' ');
                PlayerSettings.SetScriptingDefineSymbols(buildTarget, updated);
            }
#else
        var defines = PlayerSettings.GetScriptingDefineSymbolsForGroup(
            EditorUserBuildSettings.selectedBuildTargetGroup
        );
        if (!string.IsNullOrEmpty(defines) && defines.Contains(define))
        {
            var updated = defines.Replace(";" + define, "").Replace(define + ";", "").Replace(define, "");
            while (updated.Contains(";;")) updated = updated.Replace(";;", ";");
            updated = updated.Trim(';', ' ');
            PlayerSettings.SetScriptingDefineSymbolsForGroup(
                EditorUserBuildSettings.selectedBuildTargetGroup,
                updated
            );
        }
#endif
        }
        public static void AddScopeRegistry(string name, string url, string scope) {
            string manifestPath = Path.Combine(UnityEngine.Application.dataPath, "..", "Packages", "manifest.json");
            if (!File.Exists(manifestPath)) {
                Debug.LogError("manifest.json not found at: " + manifestPath);
                return;
            }
            try {
                string manifestContent = File.ReadAllText(manifestPath);
                if (manifestContent.Contains($"\"name\": \"{name}\"") &&
                    manifestContent.Contains($"\"url\": \"{url}\"") &&
                    manifestContent.Contains($"\"{scope}\"")) {
                    Debug.Log($"{name} package registry already exists in manifest.json");
                    return;
                }
                if (!manifestContent.Contains("\"scopedRegistries\"")) {
                    int lastBraceIndex = manifestContent.LastIndexOf('}');
                    string registryEntry = $",\n  \"scopedRegistries\": [\n    {{\n      \"name\": \"{name}\",\n      \"url\": \"{url}\",\n      \"scopes\": [\n        \"{scope}\"\n      ]\n    }}\n  ]\n";
                    manifestContent = manifestContent.Insert(lastBraceIndex, registryEntry);
                } else {
                    string registryEntry = $"    {{\n      \"name\": \"{name}\",\n      \"url\": \"{url}\",\n      \"scopes\": [\n        \"{scope}\"\n      ]\n    }},\n";
                    int registriesIndex = manifestContent.IndexOf("\"scopedRegistries\": [") + "\"scopedRegistries\": [".Length;
                    manifestContent = manifestContent.Insert(registriesIndex + 1, "\n" + registryEntry);
                }
                File.WriteAllText(manifestPath, manifestContent);
                AssetDatabase.Refresh();
                Debug.Log($"{name} package registry added to manifest.json successfully!");
            } catch (System.Exception e) {
                Debug.LogError($"Failed to add {name} registry to manifest.json: " + e.Message);
            }
        }
        public static void AddPackageByName(string packageName) {
            UnityEditor.PackageManager.Client.Add(packageName);
        }
    }
}