#if UNITY_EDITOR
using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Rendering;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using Unity.AI.Navigation;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;

namespace Silence.EditorTools
{
    public static class SilenceSceneGenerator
    {
        private const string Base = "Assets/SILENCE";
        private static readonly string[] names = { "AWAKENING", "RESIDENTIAL STREETS", "FINANCIAL DISTRICT",
            "APARTMENT BLOCKS", "SUBWAY DEPOT", "HOSPITAL", "SHOPPING COMPLEX",
            "INTERSTATE", "POLICE PRECINCT", "COMMUNICATIONS FACILITY" };
        private static readonly Dictionary<string, Material> mats = new Dictionary<string, Material>();
        private static readonly List<Transform> checkpoints = new List<Transform>();
        private static readonly List<Transform> chapterMarkers = new List<Transform>();

        [MenuItem("SILENCE.../Generate Playable City Scene")]
        public static void Generate()
        {
            if (!EditorUtility.DisplayDialog("SILENCE...", "Generate a new playable city scene, VR rig, networking and NavMesh? Existing generated scene will be overwritten.", "Generate", "Cancel")) return;
            GenerateCore();
            EditorUtility.DisplayDialog("SILENCE...", "Scene generated. Configure Android OpenXR in XR Plug-in Management, link Unity Services, then Build for Quest. See README.md.", "OK");
        }

        public static void GenerateBatch() => GenerateCore();

        private static void GenerateCore()
        {
            Directory.CreateDirectory(Base + "/Scenes");
            Directory.CreateDirectory(Base + "/Materials");
            Directory.CreateDirectory(Base + "/Prefabs");
            mats.Clear(); checkpoints.Clear(); chapterMarkers.Clear();
            CreateMaterials();
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            GameObject world = new GameObject("SILENCE | 10 DISTRICTS");
            SetupAtmosphere();
            for (int i = 0; i < names.Length; i++) MakeDistrict(i, world.transform);
            CreateGameSystems();
            CreatePlayerRig();
            CreateCreatures();
            var navigation = new GameObject("NavMesh Bake Source").AddComponent<NavMeshSurface>();
            navigation.collectObjects = CollectObjects.All;
            navigation.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
            navigation.overrideTileSize = true;
            navigation.tileSize = 128;
            navigation.BuildNavMesh();
            string path = Base + "/Scenes/SILENCE_City.unity";
            EditorSceneManager.SaveScene(scene, path);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(path, true) };
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private static Material Mat(string name, Color color, float metallic, float smoothness, int seed)
        {
            string filepath = Base + "/Materials/" + name + ".png";
            if (!File.Exists(filepath))
            {
                int size = 128;
                var texture = new Texture2D(size, size, TextureFormat.RGBA32, true);
                var pixels = new Color[size * size];
                var random = new System.Random(seed);
                float[,] values = new float[size, size];
                for (int y = 0; y < size; y++) for (int x = 0; x < size; x++)
                {
                    float macro = Mathf.PerlinNoise(x * 0.055f + seed, y * 0.055f + seed);
                    float micro = (float)random.NextDouble();
                    float speck = random.NextDouble() > 0.995 ? 0.45f : 0f;
                    float factor = Mathf.Clamp(0.65f + 0.48f * macro + 0.13f * micro - speck, 0.18f, 1.2f);
                    pixels[y * size + x] = new Color(
                        Mathf.Clamp01(color.r * factor), Mathf.Clamp01(color.g * factor),
                        Mathf.Clamp01(color.b * factor), 1);
                }
                texture.SetPixels(pixels); texture.Apply(true);
                File.WriteAllBytes(filepath, texture.EncodeToPNG());
                UnityEngine.Object.DestroyImmediate(texture);
            }
            AssetDatabase.ImportAsset(filepath);
            var importer = AssetImporter.GetAtPath(filepath) as TextureImporter;
            if (importer != null)
            {
                importer.mipmapEnabled = true;
                importer.textureCompression = TextureImporterCompression.Compressed;
                var android = importer.GetPlatformTextureSettings("Android");
                android.name = "Android"; android.overridden = true;
                android.format = TextureImporterFormat.ASTC_6x6;
                android.maxTextureSize = 512;
                importer.SetPlatformTextureSettings(android);
                importer.SaveAndReimport();
            }
            // Cloud source bundles have no URP renderer asset. Use a compatible shader
            // instead of producing magenta materials when the URP asset is absent.
            var hasURP = GraphicsSettings.currentRenderPipeline != null;
            var shader = hasURP ? Shader.Find("Universal Render Pipeline/Lit") : Shader.Find("Standard");
            var mat = new Material(shader ?? Shader.Find("Standard"));
            mat.name = name;
            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(filepath);
            if (mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", tex);
            if (mat.HasProperty("_MainTex")) mat.SetTexture("_MainTex", tex);
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", Color.white);
            mat.SetFloat("_Metallic", metallic);
            mat.SetFloat("_Smoothness", smoothness);
            mat.enableInstancing = true;
            string path = Base + "/Materials/" + name + ".mat";
            var old = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (old) AssetDatabase.DeleteAsset(path);
            AssetDatabase.CreateAsset(mat, path);
            mats[name] = mat;
            return mat;
        }
        private static void CreateMaterials()
        {
            Mat("Asphalt", new Color(.13f,.14f,.15f), 0, .12f, 1);
            Mat("Concrete", new Color(.37f,.37f,.35f), 0, .2f, 2);
            Mat("Brick", new Color(.33f,.24f,.22f), 0, .19f, 3);
            Mat("Rust", new Color(.38f,.20f,.12f), .48f, .16f, 4);
            Mat("Steel", new Color(.23f,.28f,.30f), .7f, .27f, 5);
            Mat("Wood", new Color(.30f,.23f,.17f), 0, .18f, 6);
            Mat("Glass", new Color(.13f,.20f,.23f), .15f, .57f, 7);
            Mat("Fabric", new Color(.26f,.28f,.25f), 0, .08f, 8);
            Mat("Dark", new Color(.08f,.10f,.12f), 0, .15f, 9);
            Mat("Moss", new Color(.18f,.24f,.15f), 0, .14f, 10);
            Mat("Yellow", new Color(.62f,.53f,.24f), .2f, .25f, 11);
            Mat("Red", new Color(.45f,.11f,.11f), .12f, .28f, 12);
        }
        private static GameObject Shape(PrimitiveType type, string name, Transform parent,
            Vector3 localPosition, Vector3 localScale, string material, bool collider = true)
        {
            GameObject go = GameObject.CreatePrimitive(type);
            go.name = name; go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition; go.transform.localScale = localScale;
            var renderer = go.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = mats[material];
            if (!collider) UnityEngine.Object.DestroyImmediate(go.GetComponent<Collider>());
            else go.isStatic = true;
            return go;
        }
        private static GameObject Root(string name, Transform parent, Vector3 position)
        {
            var root = new GameObject(name);
            if (parent) root.transform.SetParent(parent, false);
            root.transform.localPosition = position;
            return root;
        }
        private static void Label(string text, Transform parent, Vector3 position, float size = 0.12f)
        {
            var textObject = Root("Label: " + text, parent, position);
            TextMesh mesh = textObject.AddComponent<TextMesh>();
            mesh.text = text; mesh.fontSize = 32; mesh.characterSize = size;
            mesh.anchor = TextAnchor.MiddleCenter; mesh.alignment = TextAlignment.Center;
            mesh.color = new Color(.72f,.8f,.75f);
            textObject.transform.localRotation = Quaternion.Euler(0,180,0);
        }
        private static void SetupAtmosphere()
        {
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(.22f,.25f,.32f);
            RenderSettings.fog = true; RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogDensity = 0.008f;
            RenderSettings.fogColor = new Color(.11f,.14f,.17f);
            var sun = Root("Overcast moonlight", null, Vector3.up * 30).AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.color = new Color(.57f,.67f,.82f);
            sun.intensity = .67f;
            sun.transform.rotation = Quaternion.Euler(37, -33, 0);
            sun.shadows = LightShadows.Soft;
            QualitySettings.shadowDistance = 18f;
        }
        private static void MakeDistrict(int index, Transform parent)
        {
            float z = index * 90f;
            var district = Root($"{index+1:00} - {names[index]}", parent, new Vector3(0, 0, z));
            var fixedWorld = Root("Gameplay + collision", district.transform, Vector3.zero);
            var visuals = Root("Distance culled scenery", district.transform, Vector3.zero);
            var culler = district.AddComponent<DistrictCuller>();
            culler.visualRoot = visuals.transform;
            culler.showWithin = 148f;
            Shape(PrimitiveType.Cube, "Road", fixedWorld.transform, new Vector3(0,-.4f,0),new Vector3(16,.65f,90),"Asphalt");
            Shape(PrimitiveType.Cube,"Left sidewalk",fixedWorld.transform,new Vector3(-11,-.28f,0),new Vector3(6,.55f,90),"Concrete");
            Shape(PrimitiveType.Cube,"Right sidewalk",fixedWorld.transform,new Vector3(11,-.28f,0),new Vector3(6,.55f,90),"Concrete");
            for(int m=-4; m<=4; m++)
            {
                Shape(PrimitiveType.Cube,"Faded line",visuals.transform,new Vector3(0,-.052f,m*10),new Vector3(.14,.01f,3.3f),"Yellow",false);
                var light = Root("Street lamp",visuals.transform,new Vector3(-13,0,m*10));
                Shape(PrimitiveType.Cylinder,"Pole",light.transform,new Vector3(0,3.2f,0),new Vector3(.075f,3.2f,.075f),"Steel",false);
                Shape(PrimitiveType.Cube,"Lamp hood",light.transform,new Vector3(0,6.25f,0),new Vector3(.8f,.2f,.5f),"Rust",false);
            }
            BuildBuilding(index, -1, fixedWorld.transform, visuals.transform);
            BuildBuilding(index, 1, fixedWorld.transform, visuals.transform);
            BuildDebris(index, visuals.transform, fixedWorld.transform);
            var cp = Root("Checkpoint", fixedWorld.transform, new Vector3(0,.2f,-35));
            checkpoints.Add(cp.transform);
            var gate = Root("Chapter entrance", fixedWorld.transform, new Vector3(0,1.5f,-38));
            var trigger = gate.AddComponent<BoxCollider>();
            trigger.isTrigger = true; trigger.size = new Vector3(16,3,3);
            gate.AddComponent<ChapterTrigger>().chapterIndex = index;
            chapterMarkers.Add(gate.transform);
            if (index == 2) SpawnPickup("Fuse / tower", fixedWorld.transform, new Vector3(-23,.8f,0), Pickup.Type.Fuse);
            if (index == 3) SpawnPickup("Fuse / apartments", fixedWorld.transform, new Vector3(22,.8f,3), Pickup.Type.Fuse);
            if (index == 6) SpawnPickup("Fuse / mall", fixedWorld.transform, new Vector3(21,.8f,-5), Pickup.Type.Fuse);
            if (index == 9)
            {
                MakePanel(fixedWorld.transform, new Vector3(-6,1.5f,-20));
                Label("RADIO TRANSMITTER / REQUIRES POWER",visuals.transform,new Vector3(-8,4,-22),.1f);
            }
            if (index % 2 == 0) SpawnPickup("Recording / " + index, fixedWorld.transform,
                new Vector3(index % 4 == 0 ? -22 : 22, .9f,-8), Pickup.Type.Recording,
                RecordingText(index));
            Label(names[index], visuals.transform, new Vector3(0,6,-44),.19f);
        }

        private static string RecordingText(int index)
        {
            switch(index)
            {
                case 0: return "DAY 11. We were told the noise outside was thunder. It wasn't.";
                case 2: return "DAY 29. They don't see us. The quiet ones survive.";
                case 4: return "DAY 47. The underground radios are broadcasting the same signal in reverse.";
                case 6: return "DAY 83. The creatures came from the tower's lowest frequency band.";
                default: return "DAY 96. There are survivors at the facility. But the transmitter is still calling them.";
            }
        }
        private static void BuildBuilding(int index, int side, Transform parent, Transform decorations)
        {
            int id = side < 0 ? 0 : 1;
            Vector3 center = new Vector3(side*27f,0,side<0 ? -3 : 7);
            var building = Root(names[index] + " interior " + id,parent,center);
            const float w = 19f, d = 31f;
            float height = index == 2 ? 10f : 6f;
            Shape(PrimitiveType.Cube,"Floor",building.transform,new Vector3(0,-.25f,0),new Vector3(w,.5f,d),"Concrete");
            Shape(PrimitiveType.Cube,"Roof",building.transform,new Vector3(0,height,0),new Vector3(w,.4f,d),"Rust");
            Shape(PrimitiveType.Cube,"Rear wall",building.transform,new Vector3(side*9.5f,height/2,0),new Vector3(.35f,height,d),"Brick");
            Shape(PrimitiveType.Cube,"North wall",building.transform,new Vector3(0,height/2,15.5f),new Vector3(w,height,.35f),"Concrete");
            Shape(PrimitiveType.Cube,"South wall",building.transform,new Vector3(0,height/2,-15.5f),new Vector3(w,height,.35f),"Concrete");
            // Front-facing road entrance: solid wall segments leave a traversable opening.
            float frontX = -side*9.5f;
            // The building must actually connect to the sidewalk; don't leave an unwalkable void.
            Shape(PrimitiveType.Cube,"Entrance footbridge",building.transform,
                new Vector3(frontX-side*1.75f,-.18f,0),new Vector3(4f,.35f,8f),"Concrete");
            Shape(PrimitiveType.Cube,"Front wall A",building.transform,new Vector3(frontX,height/2,-9.5f),new Vector3(.35f,height,12),"Brick");
            Shape(PrimitiveType.Cube,"Front wall B",building.transform,new Vector3(frontX,height/2,9.5f),new Vector3(.35f,height,12),"Brick");
            Shape(PrimitiveType.Cube,"Lintel",building.transform,new Vector3(frontX,height-0.5f,0),new Vector3(.4f,1.0f,7),"Steel");
            // Two internal partitions with gaps create three connected rooms.
            for (int partition=0; partition<2; partition++)
            {
                float iz = -5.5f + partition*10.5f;
                Shape(PrimitiveType.Cube,"Internal wall A",building.transform,new Vector3(-4.8f,1.5f,iz),new Vector3(9.4f,3,.25f),"Concrete");
                Shape(PrimitiveType.Cube,"Internal wall B",building.transform,new Vector3(6f,1.5f,iz),new Vector3(6.9f,3,.25f),"Concrete");
            }
            var roomDecor = Root("Furniture",decorations,center);
            for (int room=0; room<3; room++)
            {
                float rz = -10.7f + 10.6f * room;
                string primary = index==5 ? "Steel" : index==4 ? "Rust" : "Wood";
                Shape(PrimitiveType.Cube,"Table / bed",roomDecor.transform,new Vector3(2f,.7f,rz+1.3f),
                    new Vector3(2.4f,1.3f,1.2f),primary,false);
                Shape(PrimitiveType.Cube,"Cabinet",roomDecor.transform,new Vector3(6.8f,1.1f,rz-1.5f),
                    new Vector3(1.5f,2.2f,1.0f),"Steel",false);
                if(room == 1)
                    Shape(PrimitiveType.Cube,"Sofa",roomDecor.transform,new Vector3(-3f,.55f,rz),
                        new Vector3(3.0f,1.1f,1.2f),"Fabric",false);
            }
            // Non-playable upper storeys read as distant architectural silhouettes.
            var towers = Root("Upper storeys",decorations,center);
            if (index == 2 || index == 6 || index == 8)
            {
                Shape(PrimitiveType.Cube,"Damaged tower",towers.transform,new Vector3(0,height + 9,0),new Vector3(17,18,28),"Concrete",false);
                for(int k=0; k<4; k++)
                    Shape(PrimitiveType.Cube,"Dark glass",towers.transform,new Vector3(-side*8.7f,height+2.7f+k*4.5f,0),
                        new Vector3(.08f,2.8f,22),"Glass",false);
            }
            if(index == 5) Label("ST. AGNES HOSPITAL",roomDecor.transform,new Vector3(0,3,-14),.1f);
            if(index == 8) Label("CITY POLICE",roomDecor.transform,new Vector3(0,3,-14),.1f);
            if(index == 9) Label("SOUTH TOWER / COMMUNICATIONS",roomDecor.transform,new Vector3(0,3,-14),.09f);
            if (index == 1 || index == 5 || index == 8 || index == 9)
                MakeDoor(building.transform,new Vector3(frontX,0,0),side, index == 9);
            MakeMovableBox(building.transform,new Vector3(2f,1.0f,-7f));
            MakeDrawer(building.transform,new Vector3(-5f,1.0f,7f));
            MakeClimbable(building.transform,new Vector3(4f,2.5f,-14f));
        }
        private static void BuildDebris(int index, Transform visuals, Transform fixedWorld)
        {
            var random = new System.Random(101 + index * 107);
            for(int a=0;a<22;a++)
            {
                float x=(float)(random.NextDouble()*78-39);
                float z=(float)(random.NextDouble()*84-42);
                if (Mathf.Abs(x)<7.5f) continue;
                float scale=(float)(random.NextDouble()*.75+.2);
                var chunk=Shape(PrimitiveType.Cube,"Fallen rubble",visuals,new Vector3(x,scale*.22f,z),
                    new Vector3(scale*1.2f,scale*.45f,scale*.7f),a%3==0?"Rust":"Concrete",false);
                chunk.transform.localRotation=Quaternion.Euler((float)random.NextDouble()*8,
                    (float)random.NextDouble()*180, (float)random.NextDouble()*12);
            }
            for (int car=0;car<5;car++)
            {
                float z=-34+car*17;
                float x=car%2==0 ? -5f : 5f;
                var vehicle=Root("Abandoned vehicle",visuals,new Vector3(x,0,z));
                vehicle.transform.localRotation=Quaternion.Euler(0, car%2==0 ? 0:180, 0);
                Shape(PrimitiveType.Cube,"Car hull",vehicle.transform,new Vector3(0,.75f,0),new Vector3(2.2f,.9f,4.3f),car%3==0?"Rust":"Steel",false);
                Shape(PrimitiveType.Cube,"Windows",vehicle.transform,new Vector3(0,1.4f,-.2f),new Vector3(1.9f,.5f,2.4f),"Glass",false);
                for(int wheel=0;wheel<4;wheel++)
                {
                    var mesh=Shape(PrimitiveType.Cylinder,"Wheel",vehicle.transform,new Vector3(wheel%2==0?-1.1f:1.1f,.45f,wheel/2==0?-1.4f:1.4f),new Vector3(.48f,.16f,.48f),"Dark",false);
                    mesh.transform.localRotation=Quaternion.Euler(0,0,90);
                }
            }
            if(index==4)
            {
                var tunnel=Root("Subway tunnel",visuals,new Vector3(0,0,25));
                Shape(PrimitiveType.Cube,"Subway facade",tunnel.transform,new Vector3(0,2,0),new Vector3(11,4,3),"Dark",false);
                Label("SUBWAY / CLOSED",tunnel.transform,new Vector3(0,3.5f,-2),.12f);
            }
            if(index==7)
            {
                var highway=Root("Collapsed overpass",visuals,new Vector3(0,0,10));
                Shape(PrimitiveType.Cube,"Bridge slab",highway.transform,new Vector3(0,9,0),new Vector3(33,1.0f,16),"Concrete",false);
                for(int c=-1;c<=1;c+=2)
                    Shape(PrimitiveType.Cube,"Pillar",highway.transform,new Vector3(c*14,4.3f,0),new Vector3(1.2f,8.6f,1.2f),"Concrete",false);
            }
        }
        private static void MakeDrawer(Transform parent,Vector3 position)
        {
            var root=Root("Interactive drawer",parent,position);
            root.AddComponent<NetworkObject>();
            var shelf=Shape(PrimitiveType.Cube,"Stationary cabinet",root.transform,new Vector3(0,0,.35f),new Vector3(.95f,1f,.6f),"Wood");
            var sliding=Shape(PrimitiveType.Cube,"Sliding drawer",root.transform,new Vector3(0,0,-.05f),new Vector3(.8f,.28f,.4f),"Steel");
            sliding.isStatic=false;
            var drawer=root.AddComponent<NetworkDrawer>(); drawer.drawer=sliding.transform;
            drawer.prompt="Pull / Push drawer";
        }
        private static void MakeClimbable(Transform parent,Vector3 position)
        {
            var rail=Shape(PrimitiveType.Cube,"Grippable pipe",parent,position,new Vector3(.18f,4f,.18f),"Steel");
            rail.isStatic=false;
            rail.AddComponent<Climbable>();
        }
        private static void MakeMovableBox(Transform parent,Vector3 position)
        {
            var box=Shape(PrimitiveType.Cube,"Throwable equipment case",parent,position,new Vector3(.36f,.35f,.38f),"Steel");
            box.isStatic=false;
            var rb=box.AddComponent<Rigidbody>(); rb.mass=1.5f; rb.collisionDetectionMode=CollisionDetectionMode.ContinuousDynamic;
            box.AddComponent<NetworkObject>();
            box.AddComponent<NetworkProp>().prompt="Grab / Throw";
            box.AddComponent<ImpactNoise>();
        }
        private static void SpawnPickup(string name,Transform parent,Vector3 position,Pickup.Type type,string text="")
        {
            var go=Shape(PrimitiveType.Cube,name,parent,position,new Vector3(.28f,.28f,.16f),type==Pickup.Type.Fuse?"Yellow":"Dark");
            go.isStatic=false;
            go.AddComponent<NetworkObject>();
            var pickup=go.AddComponent<Pickup>();
            pickup.itemType=type; pickup.prompt=type==Pickup.Type.Fuse?"Collect fuse":"Listen to recording";
            pickup.recordingText=text;
        }
        private static void MakeDoor(Transform parent,Vector3 position,int side,bool locked)
        {
            var root=Root("Hinged interactive door",parent,position);
            root.AddComponent<NetworkObject>();
            var leaf=Shape(PrimitiveType.Cube,"Door leaf",root.transform,new Vector3(0,1.25f,1.2f),new Vector3(.2f,2.5f,2.4f),"Wood");
            leaf.isStatic=false;
            var door=root.AddComponent<NetworkDoor>();
            door.hinge=leaf.transform; door.prompt="Open / Close door"; door.requiresPower=locked;
        }
        private static void MakePanel(Transform parent, Vector3 position)
        {
            var go=Shape(PrimitiveType.Cube,"Power restoration fuse panel",parent,position,new Vector3(1.6f,2f,.4f),"Rust");
            go.isStatic=false; go.AddComponent<NetworkObject>();
            var component=go.AddComponent<PowerPanel>();
            component.prompt="Install fuses / Restore power";
            component.fusesRequired=2;
        }
        private static void CreateGameSystems()
        {
            var root=Root("Session + Campaign",null,Vector3.zero);
            var nm=Root("NetworkManager",root.transform,Vector3.zero);
            var transport=nm.AddComponent<UnityTransport>();
            var manager=nm.AddComponent<NetworkManager>();
            manager.NetworkConfig.NetworkTransport=transport;
            manager.NetworkConfig.EnableSceneManagement=true;
            manager.NetworkConfig.PlayerPrefab=CreateAvatarPrefab();
            var session=Root("Relay Lobby",root.transform,Vector3.zero);
            session.AddComponent<RelaySession>();
            var director=Root("Campaign state",root.transform,Vector3.zero);
            director.AddComponent<NetworkObject>();
            var game=director.AddComponent<GameDirector>();
            game.checkpointPositions=checkpoints.ToArray();
            game.chapterEntrances=chapterMarkers.ToArray();
        }
        private static GameObject CreateAvatarPrefab()
        {
            var avatar=Root("Network Avatar",null,Vector3.zero);
            avatar.AddComponent<NetworkObject>();
            var script=avatar.AddComponent<NetworkAvatar>();
            var head=Shape(PrimitiveType.Sphere,"Head",avatar.transform,new Vector3(0,1.6f,0),new Vector3(.22f,.28f,.2f),"Dark",false);
            var left=Shape(PrimitiveType.Sphere,"Left Hand",avatar.transform,new Vector3(-.25f,1.3f,.35f),new Vector3(.14f,.12f,.18f),"Fabric",false);
            var right=Shape(PrimitiveType.Sphere,"Right Hand",avatar.transform,new Vector3(.25f,1.3f,.35f),new Vector3(.14f,.12f,.18f),"Fabric",false);
            script.visualHead=head.transform; script.visualLeft=left.transform; script.visualRight=right.transform;
            script.hideForOwner=new Renderer[]{head.GetComponent<Renderer>(),left.GetComponent<Renderer>(),right.GetComponent<Renderer>()};
            string path=Base+"/Prefabs/NetworkAvatar.prefab";
            GameObject prefab=PrefabUtility.SaveAsPrefabAsset(avatar,path);
            UnityEngine.Object.DestroyImmediate(avatar);
            return prefab;
        }
        private static void CreatePlayerRig()
        {
            var root=Root("LOCAL XR RIG (6DoF)",null,new Vector3(0,.1f,-32));
            root.AddComponent<CharacterController>();
            var rig=root.AddComponent<QuestRig>();
            var camera=Root("Tracked HMD",root.transform,new Vector3(0,1.65f,0));
            camera.tag="MainCamera";
            var view=camera.AddComponent<Camera>();
            view.nearClipPlane=.05f; view.farClipPlane=200f;
            view.clearFlags=CameraClearFlags.SolidColor;
            view.backgroundColor=new Color(.055f,.075f,.095f);
            camera.AddComponent<AudioListener>();
            var left=Root("Tracked Left Controller",root.transform,new Vector3(-.2f,1.3f,.3f));
            var right=Root("Tracked Right Controller",root.transform,new Vector3(.2f,1.3f,.3f));
            rig.head=camera.transform; rig.leftHand=left.transform; rig.rightHand=right.transform;
            MakeHandVisual(left.transform,rig,true);
            MakeHandVisual(right.transform,rig,false);
            var torch=Root("Hand flashlight",right.transform,new Vector3(0,-.03f,.09f)).AddComponent<Light>();
            torch.type=LightType.Spot; torch.spotAngle=44; torch.range=19; torch.intensity=3.0f;
            torch.color=new Color(.97f,.95f,.87f); torch.shadows=LightShadows.None;
            torch.enabled=false;
            var controls=root.AddComponent<VRInteractor>(); controls.rig=rig; controls.flashlight=torch;
            root.AddComponent<ClimbInteractor>().rig=rig;
            root.AddComponent<QuestPerformance>();
            var ui=Root("Wrist info",left.transform,new Vector3(0,.06f,.15f));
            var wrist=ui.AddComponent<WristDisplay>();
            wrist.viewer=camera.transform;
            var label=ui.AddComponent<TextMesh>();
            label.characterSize=.018f; label.fontSize=48; label.anchor=TextAnchor.MiddleCenter;
            label.color=new Color(.74f,.88f,.84f); wrist.text=label;
            CreateVRMenu(root.transform,camera.transform,rig);
        }
        private static void MakeHandVisual(Transform parent,QuestRig rig,bool left)
        {
            var mesh=Root(left?"Left glove":"Right glove",parent,Vector3.zero);
            var glove=mesh.AddComponent<HandVisual>(); glove.rig=rig; glove.leftHand=left;
            Shape(PrimitiveType.Cube,"Padded palm",mesh.transform,new Vector3(0,0,.035f),
                new Vector3(.085f,.035f,.13f),"Fabric",false);
            glove.proximal=new Transform[5]; glove.middle=new Transform[5]; glove.distal=new Transform[5];
            for (int i=0;i<5;i++)
            {
                float x=(i-2)*.019f;
                float z=i==0 ? .05f : .10f;
                var joint=Root("Finger " + i + " proximal",mesh.transform,new Vector3(x,0,z));
                var seg=Shape(PrimitiveType.Capsule,"Proximal segment",joint.transform,new Vector3(0,0,.026f),
                    new Vector3(.015f,.03f,.015f),"Fabric",false);
                seg.transform.localRotation=Quaternion.Euler(90,0,0);
                var mid=Root("Middle",joint.transform,new Vector3(0,0,.052f));
                var midShape=Shape(PrimitiveType.Capsule,"Middle segment",mid.transform,new Vector3(0,0,.020f),
                    new Vector3(.014f,.025f,.014f),"Fabric",false);
                midShape.transform.localRotation=Quaternion.Euler(90,0,0);
                var tip=Root("Distal",mid.transform,new Vector3(0,0,.04f));
                var tipShape=Shape(PrimitiveType.Capsule,"Finger tip",tip.transform,new Vector3(0,0,.016f),
                    new Vector3(.013f,.023f,.013f),"Fabric",false);
                tipShape.transform.localRotation=Quaternion.Euler(90,0,0);
                glove.proximal[i]=joint.transform; glove.middle[i]=mid.transform; glove.distal[i]=tip.transform;
            }
        }
        private static void CreateVRMenu(Transform root,Transform camera,QuestRig rig)
        {
            var gameMenu=Root("VR Menu Controller",root,Vector3.zero);
            var script=gameMenu.AddComponent<WorldMenu>(); script.rig=rig;
            gameMenu.AddComponent<MicrophoneListener>();
            var panel=Root("Floating menu",camera,new Vector3(0,0,1.55f));
            panel.transform.localRotation=Quaternion.Euler(0,180,0);
            script.panel=panel.transform;
            Shape(PrimitiveType.Cube,"Background",panel.transform,new Vector3(0,0,-.10f),new Vector3(1.3f,1.3f,.06f),"Dark",false);
            var headline=Root("Title",panel.transform,new Vector3(0,.54f,0));
            var title=headline.AddComponent<TextMesh>();
            title.text="SILENCE..."; title.anchor=TextAnchor.MiddleCenter; title.fontSize=64;
            title.characterSize=.014f; title.color=new Color(.85f,.89f,.88f);
            script.header=title;
            var subtitle=Root("Status",panel.transform,new Vector3(0,-.48f,0));
            var status=subtitle.AddComponent<TextMesh>(); status.anchor=TextAnchor.MiddleCenter;
            status.fontSize=42; status.characterSize=.009f; status.color=Color.white;
            script.status=status;
            var beam=gameMenu.AddComponent<LineRenderer>();
            beam.positionCount=2; beam.startWidth=.002f; beam.endWidth=.001f;
            beam.useWorldSpace=true;
            beam.sharedMaterial=mats["Yellow"];
            script.line=beam;
            var main=Root("Main buttons",panel.transform,Vector3.zero); script.mainButtons=main;
            string[] options={"SOLO","HOST","JOIN","RESUME","SNAP","MIC","HEIGHT+","HEIGHT-","QUIT"};
            for(int i=0;i<options.Length;i++)
                CreateButton(options[i],main.transform,new Vector3((i%3-1)*.4f,.24f-(i/3)*.26f,0),.34f,.18f);
            var board=Root("Join code keys",panel.transform,Vector3.zero); script.keyboard=board;
            string chars="ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            for(int i=0;i<chars.Length;i++)
                CreateButton(chars[i].ToString(),board.transform,new Vector3((i%9-4)*.129f,.31f-(i/9)*.17f,0),.114f,.125f);
            CreateButton("DEL",board.transform,new Vector3(-.35f,-.35f,0),.25f,.12f);
            CreateButton("GO",board.transform,new Vector3(0,-.35f,0),.25f,.12f);
            CreateButton("BACK",board.transform,new Vector3(.35f,-.35f,0),.25f,.12f);
            board.SetActive(false);
        }
        private static void CreateButton(string name,Transform parent,Vector3 position,float w,float h)
        {
            var go=Shape(PrimitiveType.Cube,"Button: "+name,parent,position,new Vector3(w,h,.025f),"Steel");
            go.isStatic=false;
            go.AddComponent<MenuKey>().value=name;
            var caption=Root("Text",go.transform,new Vector3(0,0,.56f));
            var mesh=caption.AddComponent<TextMesh>();
            mesh.text=name; mesh.fontSize=60; mesh.characterSize=.015f;
            mesh.anchor=TextAnchor.MiddleCenter;
            mesh.color=Color.white;
            caption.transform.localScale=new Vector3(1/go.transform.localScale.x,1/go.transform.localScale.y,1);
        }
        private static void CreateCreatures()
        {
            int[] locations={1,3,4,6,8};
            foreach(int id in locations)
            {
                var monster=Root("THE LISTENER / "+id,null,new Vector3(3.5f,0,id*90+6));
                monster.AddComponent<NetworkObject>();
                var agent=monster.AddComponent<NavMeshAgent>();
                agent.radius=.55f; agent.height=2.3f; agent.speed=1.4f;
                agent.angularSpeed=220f; agent.acceleration=12f; agent.stoppingDistance=.55f;
                var torso=Shape(PrimitiveType.Capsule,"Gaunt thorax",monster.transform,new Vector3(0,1.3f,0),new Vector3(.6f,1.1f,.75f),"Dark",false);
                var skull=Shape(PrimitiveType.Sphere,"Split jaw / eyeless head",monster.transform,new Vector3(0,2.35f,.24f),new Vector3(.65f,.56f,.75f),"Fabric",false);
                var legs=new Transform[4];
                for(int a=0;a<4;a++)
                {
                    float sx=a%2==0?-1:1;
                    float sz=a/2==0?-.3f:.4f;
                    var limb=Shape(PrimitiveType.Capsule,"Elongated jointed limb",monster.transform,
                        new Vector3(sx*.43f,.68f,sz),new Vector3(.16f,.65f,.16f),"Dark",false);
                    legs[a]=limb.transform;
                }
                var mind=monster.AddComponent<ListenerAI>();
                mind.agent=agent; mind.face=skull.transform; mind.limbs=legs;
            }
        }
    }
}
#endif
