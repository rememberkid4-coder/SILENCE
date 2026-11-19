# SILENCE... / Unity 6 / Standalone Meta Quest

Project type: editable Unity 6 source bundle with an Editor-driven city scene generator.

STATUS: This is a substantial uncompiled gameplay foundation / procedural vertical slice. It is **not** a completed AAA game, not a tested Quest APK, and not ready for commercial release. A Unity Editor must generate the `.unity` scene. There are no commissioned/photogrammetry models, authored character rigs or cinematic animation assets included. The generated city consists of textured primitives and modular level-blockout geometry.

## 1. What is in this download

- 10 contiguous urban districts, 20 openable ground-level building layouts, multiple rooms per building, roads, abandoned-vehicle meshes and environmental debris; subway/highway/communications structures are scenic blockouts.
- Quest controller pose tracking (6DoF), manual headset pose, CharacterController smooth walking, sprinting, crouch through HMD height, snap/smooth turning, climbing on grippable poles, flashlight.
- Two-handed proximity grip selection with movable physics objects, client-host synchronized props, impact noises and distraction throws.
- Doors, drawers, collectible recordings and fuse pickups, final fuse panel and powered doors.
- Host-authoritative Listener hearing/suspicion/patrol/investigation/search behavior using NavMesh and remembered sound locations, plus procedural limb movement, directional creature sound and checkpoints after being caught.
- Unity Netcode for GameObjects (NGO) owner head/hand pose replication and server-owned gameplay; Unity Relay co-op with host share-code and a headset VR code-entry keypad (up to 4 players including host).
- Solo play on local Netcode host; campaign chapter progression, checkpoints, a rough story arc, and host-side JSON save on device; optionally enabled mic-volume noise detection (not raw voice transmission).
- Unity Editor generator for textured materials, a scene with a scene-baked NavMesh, network objects, X/R controllers, world-space 3D pause/lobby menu, initial lighting, and a Development APK build command.

## 2. Prerequisites

- Unity Editor **6000.0.66f2** or a compatible Unity 6 version; Android Build Support, OpenJDK, Android SDK & NDK modules installed in Unity Hub.
- Android / Meta Quest build platform; a Quest 2/3/3S headset for real on-device tests.
- Internet during initial package import. The Package Manager may resolve package versions slightly differently on your installed 6.x release; if so, use recommended compatible Unity registry releases.
- Unity account and a Unity Services project ID linked in **Edit > Project Settings > Services** to use online Relay. Unity Relay quotas/billing may apply.
- Recommended: start from Unity 6's **Universal 3D (URP)** template to get its URP renderer asset and graphics defaults. This bundle has the `Assets/` and `Packages/` content but does not ship a pre-generated URP renderer asset.

Important: standalone Quest/Android cannot run the Unity Editor. A PC/Mac/Linux running the Editor or access to a remote machine/CI environment with Unity installed is needed to generate and compile an APK. The bundle is not a sideloadable APK.

## 3. Setup

1. Create a new **Universal 3D** project in Unity 6. Close Unity.
2. Copy `Assets/SILENCE/` from this bundle to the new Unity project's `Assets/` and merge the supplied `Packages/manifest.json` dependencies into its own manifest. Alternatively open this folder as a Unity project and create/configure a URP Asset manually via **Assets > Create > Rendering > URP Asset (with Universal Renderer)**, then assign it in **Project Settings > Graphics**. An existing URP template is easier and safer.
3. Reopen Unity and wait for compilation/package import. Install/enable **XR Plug-in Management** and **OpenXR** under Android. In OpenXR features, enable **Meta Quest Support**, supported Touch controller interaction profile, and resolve OpenXR project validation warnings. OpenXR is required for modern Meta Quest applications even though this prototype uses a small custom XR input layer rather than XRI.
4. **Project Settings > Player > Other Settings**: `Active Input Handling = Both` (required for the editor keyboard fallback); scripting backend `IL2CPP`; architecture `ARM64`; color space `Linear`; choose Android minimum API level supported by your target Meta headset; verify package identifier.
5. Switch active target to **Android** via **File > Build Profiles**. Set Vulkan or OpenGLES3 according to your tested headset and Unity/OpenXR compatibility; begin with Vulkan and resolve Android/OpenXR validation warnings.
6. Choose menu **SILENCE... > Generate Playable City Scene**. The tool generates `Assets/SILENCE/Scenes/SILENCE_City.unity`, `Prefabs/NetworkAvatar.prefab`, procedural PBR-like textures/materials and a baked NavMesh. The generated scene is added to Build Profiles automatically. The process may take some time inside the Editor.
7. Ensure the generated scene appears in the build scene list. Press Play in Editor for input/menu testing, then use **SILENCE... > Build Meta Quest APK** for a *Development* build. The APK will be located at `Builds/SILENCE_Quest.apk` in the actual Unity project directory.
8. For co-op, link a Unity Services project to enable anonymous authentication and Relay, and allow any necessary network traffic. HOST generates a code. Other players choose JOIN, use the in-headset keypad, then RESUME.
9. Install the APK through your normal developer-mode Quest sideload workflow; test on the physical headset for pose correctness, menu orientation, audio, frame time and controller bindings. Development mode and sideload permissions may be required.

## 4. Controls

| Action | Meta Quest controllers | Unity Editor fallback |
|---|---|---|
| Move | Left thumbstick | WASD / movement axes |
| Sprint | Click left thumbstick | Left Shift |
| Turn | Right thumbstick | Q / E |
| Aim/Select/Interact | Hand orientation + trigger | Left mouse (approximation) |
| Grab, hold and throw | Grip near an object | Right mouse (approximation) |
| Flashlight | Right A | F |
| Pause / menu | Left secondary button (usually Y/B depending profile) | Esc |
| Climb pole | Grip within 22 cm of climbable | Needs tracked controller to evaluate |
| Crouch / hide | Physically lower headset | Requires headset tracking |

Menu: SOLO starts an offline host; HOST starts Relay co-op; JOIN opens keypad; RESUME enters the city. The menu can only be dismissed after a session is active. SNAP toggles snap/smooth turn, HEIGHT+/- adjusts height offset, MIC requests opt-in permission and uses RMS level to generate virtual noise.

## 5. Folder map

```
Assets/SILENCE/
  Scripts/
    AI/ListenerAI.cs                      navmesh creature and original synth
    Core/SoundBus.cs                     centralized host noise events
    Core/GameDirector.cs                 chapters, checkpoints, save
    Interaction/InteractiveObject.cs     doors, panels, fuse and log pickups
    Interaction/NetworkDrawer.cs         shared sliding drawers
    Interaction/NetworkProp.cs           grip/release/throw physics replication
    Network/NetworkAvatar.cs             replicated headset/controller poses
    Network/RelaySession.cs              solo + Unity Relay multiplayer
    VR/QuestRig.cs                       tracked HMD and controllers + movement
    VR/VRInteractor.cs                   trigger/grip interactions and torch
    VR/ClimbInteractor.cs                climbable pipe locomotion
    VR/HandVisual.cs                     segmented placeholder glove finger curls
    VR/MicrophoneListener.cs             optional RMS loudness only
    VR/QuestPerformance.cs               mobile-oriented frame defaults
    VR/WorldMenu.cs                      world-space VR interface and keypad
    VR/WristDisplay.cs                   progression/status/radio text
    World/DistrictCuller.cs              geometry renderer culling
    Editor/SilenceSceneGenerator.cs      generated 10-area city and NavMesh
    Editor/SilenceAndroidBuild.cs        Android development APK build
```

## 6. Current limitations: necessary work toward the requested full game

- **Not photoreal:** procedurally textured blockout shapes are not AAA environment art. Replace each with licensed or original LOD-equipped photogrammetry/optimized environment assets, advanced PBR maps, detailed architecture, and believable weather. The current five creature instances use an original low-detail procedural model rather than a skeletal/IK-authored animation set. Procedural segmented glove fingers are included, but no realistic anatomical finger animation or skinned hand mesh exists yet.
- **No polished climbing/physics IK:** climbing is a simple grip-anchor locomotion behavior, not a full-body system; doors/drawers animate on interact instead of a constrained joint/true pulling force. No detailed finger articulation, wrist rotation visual model, or object hand poses.
- **City architecture:** street continuity is real in the generated scene, but many named scenic features (subway depth, collapsed tower interiors, hospital surgery suites etc.) have limited geometry. Significant level art, puzzles and environmental storytelling remain unbuilt.
- **Multiplayer validation:** server authorization and head/hand pose replication are implemented but the complete gameplay and API compile have not been verified in a running Unity Editor or multiple Quest headsets. High-latency pose prediction, disconnection recovery, mid-session reconnect, cheats, and production-grade security need implementation/testing. Relay may require service configuration and incurs potential usage costs.
- **No polished soundbank:** the creature produces synthesized original tonal audio; there are no authored footsteps, radio VO, thunder, rain, room reverb or cinematic audio mixes included. Physics collisions currently make AI sound events but do not all play audible impact clips.
- **Rendering:** materials use procedural texture noise, one directional moonlight and a flashlight; this is not pre-baked realistic GI. Bake lighting and author lightmaps/light probes/occlusion, add Quest-friendly fog/rain shaders, and profile foveation using the correct headset APIs.
- **Quest 72 FPS target unverified.** `Application.targetFrameRate = 72` requests a target; it does not guarantee stable 72 FPS. Test frame timing on each Quest model and optimize batching, polycount, overdraw and memory accordingly.
- **Campaign:** basic chapter traversal, collectibles, electrical gate, radio text and ending are wired, but not a handcrafted multi-hour narrative, cinematic pacing, original voice performances or a full series of bespoke puzzle set pieces.
- **Editor-only scene generation:** no pre-baked `.unity` scene or `.apk` is included, because the Unity Editor is unavailable in this environment. The generator creates the scene when invoked from Unity.

### Suggested next production milestones

1. Import a realistic city asset set with properly licensed meshes and 3D spatial sounds; establish a stable lighting/rendering benchmark on Quest.
2. Replace prototype avatar/Listener with skinned hand/body rigs, animation layers and a blended locomotion/IK state machine.
3. Add a dedicated sound propagation model (portals/material occlusion), rich creature behaviors and synchronized audio events.
4. Author each location as a proper playable level with 3D puzzles, more recordings and environmental storytelling.
5. Implement production multiplayer flow, reconnection, scene streaming and multiplayer VR interaction tests.
6. Add UI comfort, interaction tutorials, accessibility options, regression testing, save migration, profiling, certification and release QA.

## 7. References

- Unity OpenXR/XR scene guidance: https://docs.unity.com/en-us/engine/6000.0/manual/xr/configuring-project-for/scene-setup/origin
- Unity Relay manual integration: https://docs.unity.com/en-us/mps-sdk/tutorials/relay-and-ngo
- Netcode for GameObjects / sessions: https://docs.unity.com/en-us/mps-sdk/tutorials/build-your-first-session
- Meta Unity/Quest setup: https://developers.meta.com/vr/documentation/unity/unity-tutorial-hello-world-manual-steps/

All game design and C# source in this archive was generated for SILENCE... . No third-party art, copyrighted music, fonts or downloaded game assets are bundled.
