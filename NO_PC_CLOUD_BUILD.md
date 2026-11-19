# SILENCE... / no-PC Quest APK workflow (Unity 6)

The package is a Unity project SOURCE bundle. It cannot be installed directly onto a Meta Quest headset. You can build it remotely via **Unity Build Automation** using a phone or a headset browser to manage the process, but you'll need to put the extracted project files in source control first.

## Cloud path (not a guaranteed one-click build)

1. Obtain this archive. Extract the `SILENCE_Unity6_Quest` folder. The **repository root** must directly contain `Assets`, `Packages`, and `ProjectSettings` (not an extra outer folder).
2. Create an online Git repository such as a GitHub repository. Upload/commit the entire extracted Unity project folder's contents. A ZIP uploaded as a single blob is **not** enough; Unity Build Automation needs actual project files. On mobile, a Git client or terminal environment may be required for committing folders.
3. Open https://cloud.unity.com/ in the Quest browser or mobile browser. Create a Unity Cloud project and set up **DevOps > Build Automation**. Unity has a free allowance; builds past the plan limits can incur charges. Never enable paid billing without understanding the plan.
4. Connect Build Automation to the repository under Source Control. Select your repository and branch. Keep access permissions minimal, and never send account access tokens in chat.
5. Configure a new **Android** target for the Unity Editor version named in `ProjectSettings/ProjectVersion.txt` (`6000.0.66f2`). If that exact version isn't offered by your cloud builder, a compatible Unity 6 editor might be required and packages may need updating.
6. In **Advanced settings > Script hooks > Pre-export method**, enter exactly:

   `Silence.EditorTools.SilenceCloudBuild.PreExport`

   The hook (a) configures Android ARM64 IL2CPP and app identifier, (b) tries to enable Android OpenXR, Meta Quest support and Oculus Touch controller profile, and (c) generates the city scene, prefabs and NavMesh **inside the cloud Editor**.
7. Make sure the Android build output is **APK** (not AAB) and choose the normal development/unsigned test build options available in the dashboard. Use Android ARM64 and a Quest-compatible SDK. Select or inherit the generated scene from the project's scene list. Set up Android signing requirements if the dashboard requests them.
8. Start a build. View the build log and download the APK artifact when (and only when) the build reports success.
9. Install the APK onto a developer-enabled Meta Quest using a supported sideloading workflow. Verify it runs as immersive VR; if it opens as a 2D app or black screen, inspect OpenXR Android settings and the build log first.

## Important limitations

- **No APK is included.** This project has never been compiled or tested in Unity. The first cloud build might report C# package/API errors that need fixes.
- The resulting build, if successful, is an unpolished procedural city prototype, not a commercial-quality complete game.
- The online Relay mode also needs a Unity Services project linked. Only the offline SOLO mode is intended to operate without linking services; that mode is also unverified.
- The source archive does not have an authored URP renderer asset. The scene generator chooses a compatible material shader if there is no URP asset, prioritizing visible geometry over URP fidelity.
- The cloud script is not a substitute for headset testing. Unity's cloud builders or available Unity versions can change, and Android XR initialization may require extra adjustments.
- Browser-only repository upload can be awkward on Quest. A phone Git client or supported web-based development environment may be the easiest way to push many files. Do not upload a single ZIP to the Git repository in place of the extracted files.

## Official documentation

- Unity Build Automation setup: https://docs.unity.com/en-us/build-automation
- Connect a Git repository: https://docs.unity.com/en-us/build-automation/get-started-with-build-automation/connect-your-version-control-system
- Run pre-export scripts: https://docs.unity.com/en-us/build-automation/advanced-build-configuration/run-custom-scripts-during-the-build-process
- Meta OpenXR setup: https://developers.meta.com/vr/documentation/unity/unity-xr-plugin/
