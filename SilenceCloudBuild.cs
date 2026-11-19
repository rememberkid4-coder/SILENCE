#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.XR.Management;
using UnityEditor.XR.Management;
using UnityEditor.XR.Management.Metadata;
using UnityEngine.XR.OpenXR;
using UnityEngine.XR.OpenXR.Features.Interactions;
using UnityEngine.XR.OpenXR.Features.MetaQuestSupport;

namespace Silence.EditorTools
{
    /// <summary>
    /// Unity Build Automation: Advanced > Script Hooks > Pre-export method:
    /// Silence.EditorTools.SilenceCloudBuild.PreExport
    ///
    /// Runs inside the cloud Unity Editor, so no local editor or scene generation is needed.
    /// This prepares a Quest-oriented development build, but cannot replace on-device validation.
    /// </summary>
    public static class SilenceCloudBuild
    {
        public static void PreExport()
        {
            Debug.Log("SILENCE Cloud: beginning Android/Quest configuration and city generation");
            PlayerSettings.companyName = "Independent";
            PlayerSettings.productName = "SILENCE...";
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, "com.silencevr.survival");
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel29;
            PlayerSettings.colorSpace = ColorSpace.Linear;
            EditorUserBuildSettings.buildAppBundle = false;  // APK, not Android App Bundle.
            ConfigureOpenXR();
            SilenceSceneGenerator.GenerateBatch();
            AssetDatabase.SaveAssets();
            Debug.Log("SILENCE Cloud: generated city and configured Android player");
        }

        private static void ConfigureOpenXR()
        {
            var settings = XRGeneralSettingsPerBuildTarget.XRGeneralSettingsForBuildTarget(BuildTargetGroup.Android);
            if (settings == null)
            {
                XRGeneralSettingsPerBuildTarget all;
                if (!EditorBuildSettings.TryGetConfigObject(XRGeneralSettings.k_SettingsKey, out all) || all == null)
                {
                    Directory.CreateDirectory("Assets/XR");
                    all = ScriptableObject.CreateInstance<XRGeneralSettingsPerBuildTarget>();
                    AssetDatabase.CreateAsset(all, "Assets/XR/SILENCE_XRGeneralSettings.asset");
                    EditorBuildSettings.AddConfigObject(XRGeneralSettings.k_SettingsKey, all, true);
                }
                all.CreateDefaultManagerSettingsForBuildTarget(BuildTargetGroup.Android);
                settings = all.SettingsForBuildTarget(BuildTargetGroup.Android);
            }
            if (settings == null || settings.Manager == null)
                throw new InvalidOperationException("XR settings for Android could not be created. An XR Management template may be required.");
            bool assigned = XRPackageMetadataStore.AssignLoader(
                settings.Manager, "UnityEngine.XR.OpenXR.OpenXRLoader", BuildTargetGroup.Android);
            if (!assigned && settings.Manager.activeLoaders.Count == 0)
                throw new InvalidOperationException("Could not assign Android OpenXR loader. See Unity XR Plug-in Management setup.");

            var openXR = OpenXRSettings.GetSettingsForBuildTargetGroup(BuildTargetGroup.Android);
            if (openXR == null)
                throw new InvalidOperationException("Android OpenXR settings missing. Verify com.unity.xr.openxr is installed.");
            var meta = openXR.GetFeature<MetaQuestFeature>();
            if (meta == null)
                throw new InvalidOperationException("Meta Quest Support feature missing from Android OpenXR settings.");
            meta.enabled = true;
            var controllers = openXR.GetFeature<OculusTouchControllerProfile>();
            if (controllers == null)
                throw new InvalidOperationException("Oculus Touch Controller interaction profile missing from Android OpenXR settings.");
            controllers.enabled = true;
            EditorUtility.SetDirty(openXR);
            Debug.Log("SILENCE Cloud: Android OpenXR and Quest/Touch features configured");
        }
    }
}
#endif
