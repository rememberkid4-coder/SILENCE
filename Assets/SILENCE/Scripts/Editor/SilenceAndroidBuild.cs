#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Silence.EditorTools
{
    public static class SilenceAndroidBuild
    {
        [MenuItem("SILENCE.../Build Meta Quest APK")]
        public static void Build()
        {
            string scene = "Assets/SILENCE/Scenes/SILENCE_City.unity";
            if (!File.Exists(scene)) SilenceSceneGenerator.GenerateBatch();
            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android)
                throw new Exception("Switch Build Profile to Android first (Build Profiles > Android > Switch Platform).");
            PlayerSettings.companyName = "Independent";
            PlayerSettings.productName = "SILENCE...";
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, "com.silencevr.survival");
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel29;
            PlayerSettings.colorSpace = ColorSpace.Linear;
            QualitySettings.vSyncCount = 0;
            Directory.CreateDirectory("Builds");
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions {
                scenes = new[] { scene },
                locationPathName = "Builds/SILENCE_Quest.apk",
                target = BuildTarget.Android,
                options = BuildOptions.Development
            });
            if (report.summary.result != BuildResult.Succeeded)
                throw new Exception("Android build failed. Check the Console for compiler/package errors.");
            Debug.Log("APK: " + Path.GetFullPath("Builds/SILENCE_Quest.apk"));
        }
    }
}
#endif
