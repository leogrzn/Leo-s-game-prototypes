using System.Collections.Generic;
using System.IO;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace HoldTheMine.EditorTools
{
    /// <summary>Generates everything for Hold the Mine: materials, prefabs, config, the scene, NavMesh and UI.</summary>
    public static partial class HoldTheMineBuilder
    {
        const string Root = "Assets";
        const string MatDir = "Assets/Materials";
        const string PrefabDir = "Assets/Prefabs";
        const string ConfigPath = "Assets/Config/GameConfig.asset";
        const string ScenePath = "Assets/Scenes/HoldTheMine.unity";

        static readonly Dictionary<string, Material> Mats = new Dictionary<string, Material>();

        [MenuItem("Hold the Mine/Build Game (materials, prefabs, scene)")]
        public static void BuildAll()
        {
            Directory.CreateDirectory(MatDir);
            Directory.CreateDirectory(PrefabDir);
            Directory.CreateDirectory("Assets/Config");
            Directory.CreateDirectory("Assets/Scenes");
            Random.InitState(1337);

            var config = AssetDatabase.LoadAssetAtPath<GameConfig>(ConfigPath);
            if (config == null)
            {
                config = ScriptableObject.CreateInstance<GameConfig>();
                AssetDatabase.CreateAsset(config, ConfigPath);
            }

            BuildMaterials();
            BuildLevelMaterials();
            var icons = BuildIcons();
            var prefabs = BuildPrefabs(config);
            BuildScene(config, prefabs, icons);
            AssetDatabase.SaveAssets();
            Debug.Log("[HoldTheMine] Build complete: " + ScenePath);
        }

        // =====================================================================
        // Materials
        // =====================================================================
        static void BuildMaterials()
        {
            Mats.Clear();
            Lit("Sand", "#D9BE83"); Lit("SandDark", "#C7A86C"); Lit("Rock", "#9C8A74"); Lit("RockDark", "#7A6A58");
            Lit("Dirt", "#8C7356"); Lit("ForestGround", "#3E5A2E"); Lit("Road", "#5B5149"); Lit("RoadLine", "#8F8270");
            Lit("Trunk", "#5A3D24"); Lit("Leaves", "#2F6B2A"); Lit("LeavesDark", "#24521F"); Lit("LeavesLight", "#3F8436");
            Lit("MineRock", "#6E6257"); Lit("Wood", "#8A5A2E"); Lit("WoodDark", "#5E3B1D"); Lit("Gold", "#FFC928", 0.6f, 0.7f);
            Lit("Ore", "#5C4A3A"); Lit("Black", "#1E1E22"); Lit("Metal", "#5E646B", 0.5f, 0.4f); Lit("Concrete", "#8B8F93");
            Lit("Sandbag", "#B79E6E"); Lit("Skin", "#F2C79B"); Lit("Cactus", "#5F8F3A");
            // Friendlies
            Lit("PlayerBlue", "#2F6FE4"); Lit("PlayerDark", "#1D3F8A"); Lit("SoldierGreen", "#4E9A3E"); Lit("SoldierDark", "#2F5E26");
            Lit("MinerShirt", "#F08A24"); Lit("MinerJeans", "#2E4D7A"); Lit("HardHat", "#FFE03A");
            // Enemies
            Lit("RiflemanRed", "#C8332B"); Lit("RaiderOrange", "#E2552A"); Lit("AmbusherPurple", "#7B3FA8"); Lit("GrenadierMaroon", "#7D1F2C");
            Lit("HeavyGray", "#3B3B42"); Lit("EnemyPants", "#3A2A22"); Lit("Bandana", "#111111");
            // Vehicles
            Lit("TruckYellow", "#D9A21B"); Lit("TruckWhite", "#E8E8E2"); Lit("Tarp", "#3D7A5A"); Lit("Crate", "#A37444");
            // FX
            Unlit("Fire", "#FF8A1E"); Unlit("Spark", "#FFF27A"); Lit("Debris", "#4A4038"); Lit("GrenadeMat", "#202020");
            Transparent("GhostOk", new Color(0.3f, 1f, 0.3f, 0.4f)); Transparent("GhostBad", new Color(1f, 0.25f, 0.2f, 0.4f));
            var tracerShader = Shader.Find("Sprites/Default") ?? Shader.Find("Universal Render Pipeline/Particles/Unlit");
            SaveMat("Tracer", new Material(tracerShader) { color = Color.white });
        }

        static Color Hex(string h) { ColorUtility.TryParseHtmlString(h, out var c); return c; }

        static Material Lit(string name, string hex, float smooth = 0.1f, float metal = 0f)
        {
            var m = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            m.SetColor("_BaseColor", Hex(hex));
            m.SetFloat("_Smoothness", smooth);
            m.SetFloat("_Metallic", metal);
            m.enableInstancing = true;
            return SaveMat(name, m);
        }

        static Material Unlit(string name, string hex)
        {
            var m = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            m.SetColor("_BaseColor", Hex(hex));
            return SaveMat(name, m);
        }

        static Material Transparent(string name, Color c)
        {
            var m = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            m.SetColor("_BaseColor", c);
            m.SetFloat("_Surface", 1);
            m.SetFloat("_Blend", 0);
            m.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            m.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            m.SetFloat("_ZWrite", 0);
            m.SetOverrideTag("RenderType", "Transparent");
            m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            m.renderQueue = (int)RenderQueue.Transparent;
            return SaveMat(name, m);
        }

        static Material SaveMat(string name, Material m)
        {
            string path = $"{MatDir}/{name}.mat";
            var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing != null)
            {
                existing.shader = m.shader;
                existing.CopyPropertiesFromMaterial(m);
                existing.renderQueue = m.renderQueue;
                Object.DestroyImmediate(m);
                m = existing;
                EditorUtility.SetDirty(m);
            }
            else AssetDatabase.CreateAsset(m, path);
            Mats[name] = m;
            return m;
        }

        static Material M(string n) => Mats[n];

        // =====================================================================
        // Geometry helpers
        // =====================================================================
        static GameObject Cube(Transform parent, string name, Vector3 pos, Vector3 scale, string mat, bool collider = false, Vector3? euler = null)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localScale = scale;
            if (euler.HasValue) go.transform.localEulerAngles = euler.Value;
            go.GetComponent<Renderer>().sharedMaterial = M(mat);
            if (!collider) Object.DestroyImmediate(go.GetComponent<Collider>());
            return go;
        }

        static Transform Empty(Transform parent, string name, Vector3 pos)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            return go.transform;
        }

        // =====================================================================
        // Characters
        // =====================================================================
        class CharLook
        {
            public string body, legs, head = "Skin", hat, arms;
            public bool gun = true, armsHoldGun = true, pickaxe, machete, grenades, armor, hood;
            public string hatShape = "helmet";
        }

        static GameObject BuildCharacter(string name, CharLook look, out Transform muzzle, out BlockyAnimator anim)
        {
            var root = new GameObject(name);
            var model = Empty(root.transform, "Model", Vector3.zero);
            string arms = look.arms ?? look.body;
            Cube(model, "Body", new Vector3(0, 1.15f, 0), new Vector3(0.8f, 0.9f, 0.45f), look.body);
            Cube(model, "Head", new Vector3(0, 1.88f, 0), new Vector3(0.52f, 0.52f, 0.52f), look.head);
            Cube(model, "EyeL", new Vector3(-0.11f, 1.92f, 0.265f), new Vector3(0.08f, 0.1f, 0.02f), "Black");
            Cube(model, "EyeR", new Vector3(0.11f, 1.92f, 0.265f), new Vector3(0.08f, 0.1f, 0.02f), "Black");
            if (look.hat != null)
            {
                if (look.hatShape == "helmet") Cube(model, "Helmet", new Vector3(0, 2.18f, 0), new Vector3(0.6f, 0.18f, 0.6f), look.hat);
                else if (look.hatShape == "hardhat")
                {
                    Cube(model, "HardHat", new Vector3(0, 2.2f, 0), new Vector3(0.58f, 0.2f, 0.58f), look.hat);
                    Cube(model, "Brim", new Vector3(0, 2.12f, 0.1f), new Vector3(0.66f, 0.05f, 0.72f), look.hat);
                }
                else if (look.hatShape == "bandana") Cube(model, "Bandana", new Vector3(0, 1.85f, 0.02f), new Vector3(0.54f, 0.2f, 0.54f), look.hat);
                else if (look.hatShape == "hood") Cube(model, "Hood", new Vector3(0, 1.98f, -0.04f), new Vector3(0.6f, 0.5f, 0.56f), look.hat);
            }
            if (look.armor)
            {
                Cube(model, "ChestPlate", new Vector3(0, 1.2f, 0.25f), new Vector3(0.84f, 0.7f, 0.1f), "Metal");
                Cube(model, "ShoulderL", new Vector3(-0.5f, 1.58f, 0), new Vector3(0.36f, 0.16f, 0.5f), "Metal");
                Cube(model, "ShoulderR", new Vector3(0.5f, 1.58f, 0), new Vector3(0.36f, 0.16f, 0.5f), "Metal");
            }
            if (look.grenades)
                for (int i = 0; i < 3; i++) Cube(model, "Bomb" + i, new Vector3(-0.25f + i * 0.25f, 0.8f, 0.26f), new Vector3(0.18f, 0.18f, 0.18f), "GrenadeMat");

            var legL = Empty(model, "LegL", new Vector3(-0.2f, 0.7f, 0));
            Cube(legL, "Leg", new Vector3(0, -0.35f, 0), new Vector3(0.36f, 0.7f, 0.4f), look.legs);
            var legR = Empty(model, "LegR", new Vector3(0.2f, 0.7f, 0));
            Cube(legR, "Leg", new Vector3(0, -0.35f, 0), new Vector3(0.36f, 0.7f, 0.4f), look.legs);
            var armL = Empty(model, "ArmL", new Vector3(-0.54f, 1.52f, 0));
            Cube(armL, "Arm", new Vector3(0, -0.34f, 0), new Vector3(0.26f, 0.72f, 0.3f), arms);
            Cube(armL, "Hand", new Vector3(0, -0.74f, 0), new Vector3(0.24f, 0.14f, 0.26f), "Skin");
            var armR = Empty(model, "ArmR", new Vector3(0.54f, 1.52f, 0));
            Cube(armR, "Arm", new Vector3(0, -0.34f, 0), new Vector3(0.26f, 0.72f, 0.3f), arms);
            Cube(armR, "Hand", new Vector3(0, -0.74f, 0), new Vector3(0.24f, 0.14f, 0.26f), "Skin");

            muzzle = null;
            if (look.gun)
            {
                var gun = Empty(model, "Gun", new Vector3(0.18f, 1.3f, 0.45f));
                Cube(gun, "Barrel", new Vector3(0, 0, 0.25f), new Vector3(0.12f, 0.14f, 0.95f), "Black");
                Cube(gun, "Stock", new Vector3(0, -0.1f, -0.1f), new Vector3(0.1f, 0.22f, 0.3f), "WoodDark");
                muzzle = Empty(gun, "Muzzle", new Vector3(0, 0.02f, 0.78f));
            }
            if (look.pickaxe)
            {
                Cube(armR, "Handle", new Vector3(0, -0.8f, 0.3f), new Vector3(0.07f, 0.07f, 0.8f), "Wood");
                Cube(armR, "PickHead", new Vector3(0, -0.8f, 0.68f), new Vector3(0.08f, 0.5f, 0.08f), "Metal");
            }
            if (look.machete)
                Cube(armR, "Blade", new Vector3(0, -0.85f, 0.35f), new Vector3(0.04f, 0.1f, 0.75f), "Metal");
            if (muzzle == null) muzzle = Empty(model, "Muzzle", new Vector3(0.2f, 1.4f, 0.5f));

            anim = root.AddComponent<BlockyAnimator>();
            anim.leftArm = armL; anim.rightArm = armR; anim.leftLeg = legL; anim.rightLeg = legR;
            anim.armsHoldGun = look.armsHoldGun;
            return root;
        }

        static void AddCapsule(GameObject go, float radius = 0.42f, float height = 2.1f)
        {
            var c = go.AddComponent<CapsuleCollider>();
            c.radius = radius; c.height = height; c.center = new Vector3(0, height / 2f, 0);
        }

        static NavMeshAgent AddAgent(GameObject go, float speed, float stopping)
        {
            var a = go.AddComponent<NavMeshAgent>();
            a.radius = 0.45f; a.height = 2f; a.speed = speed; a.stoppingDistance = stopping;
            a.acceleration = 16f; a.angularSpeed = 540f;
            a.obstacleAvoidanceType = ObstacleAvoidanceType.MedQualityObstacleAvoidance;
            return a;
        }

        static Health AddHealth(GameObject go, Team team, UnitKind kind, float hp)
        {
            var h = go.AddComponent<Health>();
            h.team = team; h.kind = kind; h.maxHealth = hp; h.current = hp;
            return h;
        }

        static GameObject SavePrefab(GameObject go, string name)
        {
            string path = $"{PrefabDir}/{name}.prefab";
            var p = PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
            return p;
        }

        // =====================================================================
        // Prefabs
        // =====================================================================
        class PrefabSet
        {
            public GameObject soldier, miner, tower, barricade, goldTruck, foodTruck;
            public GameObject[] enemies = new GameObject[5];
        }

        static PrefabSet BuildPrefabs(GameConfig c)
        {
            var set = new PrefabSet();

            set.soldier = BuildSoldier(c);
            set.miner = BuildMiner(c);
            // Enemies
            var looks = new Dictionary<EnemyType, CharLook>
            {
                { EnemyType.Rifleman, new CharLook { body = "RiflemanRed", legs = "EnemyPants", hat = "Bandana", hatShape = "bandana" } },
                { EnemyType.Raider, new CharLook { body = "RaiderOrange", legs = "EnemyPants", hat = "Bandana", hatShape = "bandana", gun = false, armsHoldGun = false, machete = true } },
                { EnemyType.Ambusher, new CharLook { body = "AmbusherPurple", legs = "EnemyPants", hat = "AmbusherPurple", hatShape = "hood" } },
                { EnemyType.Grenadier, new CharLook { body = "GrenadierMaroon", legs = "EnemyPants", hat = "Bandana", hatShape = "helmet", gun = false, armsHoldGun = false, grenades = true } },
                { EnemyType.Heavy, new CharLook { body = "HeavyGray", legs = "Black", hat = "Metal", hatShape = "helmet", armor = true } },
            };
            foreach (var kv in looks)
            {
                var s = c.GetEnemy(kv.Key);
                var go = BuildCharacter("Enemy_" + kv.Key, kv.Value, out var muzzle, out _);
                AddCapsule(go);
                AddHealth(go, Team.Enemy, UnitKind.Enemy, s.health);
                var agent = AddAgent(go, s.moveSpeed, 0.5f);
                agent.avoidancePriority = 60;
                var ai = go.AddComponent<EnemyAI>(); ai.type = kv.Key; ai.muzzle = muzzle;
                if (kv.Key == EnemyType.Heavy) go.transform.localScale = Vector3.one * 1.3f;
                if (kv.Key == EnemyType.Raider) go.transform.localScale = Vector3.one * 0.92f;
                set.enemies[(int)kv.Key] = SavePrefab(go, "Enemy_" + kv.Key);
            }

            set.barricade = BuildBarricade(c);
            set.tower = BuildTower(c);
            set.goldTruck = BuildTruck("GoldTruck", TruckKind.Gold, "TruckYellow", c);
            set.foodTruck = BuildTruck("FoodTruck", TruckKind.Food, "TruckWhite", c);
            return set;
        }

        static GameObject BuildTruck(string name, TruckKind kind, string bodyMat, GameConfig c)
        {
            var go = new GameObject(name);
            var t = go.transform;
            Cube(t, "Chassis", new Vector3(0, 0.75f, 0), new Vector3(2.3f, 0.4f, 5.2f), "Black");
            Cube(t, "Cab", new Vector3(0, 1.6f, 1.75f), new Vector3(2.2f, 1.4f, 1.5f), bodyMat);
            Cube(t, "Windshield", new Vector3(0, 1.9f, 2.51f), new Vector3(1.9f, 0.6f, 0.05f), "Metal");
            Cube(t, "Bed", new Vector3(0, 1.15f, -0.85f), new Vector3(2.3f, 0.4f, 3.4f), bodyMat);
            Cube(t, "SideL", new Vector3(-1.1f, 1.6f, -0.85f), new Vector3(0.1f, 0.6f, 3.4f), bodyMat);
            Cube(t, "SideR", new Vector3(1.1f, 1.6f, -0.85f), new Vector3(0.1f, 0.6f, 3.4f), bodyMat);
            foreach (var z in new[] { 1.7f, -0.5f, -1.8f })
                foreach (var x in new[] { -1.15f, 1.15f })
                    Cube(t, "Wheel", new Vector3(x, 0.5f, z), new Vector3(0.35f, 1f, 1f), "Black");
            var cargo = Empty(t, "Cargo", Vector3.zero).gameObject;
            if (kind == TruckKind.Gold)
            {
                for (int i = 0; i < 6; i++)
                    Cube(cargo.transform, "GoldBar", new Vector3(-0.5f + (i % 2) * 1f, 1.5f + (i / 4) * 0.35f, -1.8f + (i % 3) * 0.9f), new Vector3(0.8f, 0.3f, 0.7f), "Gold");
            }
            else
            {
                Cube(cargo.transform, "Tarp", new Vector3(0, 2.0f, -0.85f), new Vector3(2.2f, 1.2f, 3.3f), "Tarp");
                for (int i = 0; i < 2; i++) Cube(cargo.transform, "Crate", new Vector3(-0.5f + i, 1.6f, -2.75f), new Vector3(0.8f, 0.6f, 0.4f), "Crate");
            }
            var bc = go.AddComponent<BoxCollider>(); bc.center = new Vector3(0, 1.3f, 0); bc.size = new Vector3(2.4f, 2.4f, 5.2f);
            AddHealth(go, Team.Friendly, UnitKind.Truck, c.truckHealth);
            var truck = go.AddComponent<Truck>(); truck.kind = kind; truck.cargoVisual = cargo;
            return SavePrefab(go, name);
        }

        // =====================================================================
        // Scene
        // =====================================================================
        static readonly Vector3[] RoadPts =
        {
            new Vector3(0, 0, -68), new Vector3(-5, 0, -48), new Vector3(4, 0, -30), new Vector3(0, 0, -16), new Vector3(0, 0, -5)
        };

        static void BuildScene(GameConfig config, PrefabSet prefabs, UIManager.Icons icons)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            RockSpots.Clear(); TreeSpots.Clear();

            // Lighting
            var sunGo = new GameObject("Sun");
            var sun = sunGo.AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.color = new Color(1f, 0.95f, 0.85f);
            sun.intensity = 1.25f;
            sun.shadows = LightShadows.Soft;
            sunGo.transform.rotation = Quaternion.Euler(52f, -35f, 0);
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.62f, 0.68f, 0.78f);
            RenderSettings.ambientEquatorColor = new Color(0.55f, 0.52f, 0.46f);
            RenderSettings.ambientGroundColor = new Color(0.32f, 0.28f, 0.22f);
            RenderSettings.skybox = null;
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = new Color(0.78f, 0.74f, 0.65f);
            RenderSettings.fogStartDistance = 60f;
            RenderSettings.fogEndDistance = 140f;

            // World
            var world = new GameObject("World").transform;
            var ground = Cube(world, "Ground", new Vector3(0, -0.5f, 0), new Vector3(140, 1, 140), "Dirt", true);
            var decor = Empty(world, "GroundTiles", Vector3.zero);
            Cube(decor, "DesertTile", new Vector3(-42.5f, 0.005f, 0), new Vector3(55, 0.01f, 140), "Sand");
            Cube(decor, "DesertBlend", new Vector3(-13.5f, 0.004f, 0), new Vector3(3, 0.008f, 140), "SandDark");
            Cube(decor, "ForestTile", new Vector3(42.5f, 0.005f, 0), new Vector3(55, 0.01f, 140), "ForestGround");
            for (int i = 0; i < 40; i++)
                Cube(decor, "SandRipple", new Vector3(Random.Range(-68f, -18f), 0.012f, Random.Range(-68f, 68f)), new Vector3(Random.Range(2f, 6f), 0.01f, Random.Range(0.4f, 1.2f)), "SandDark", false, new Vector3(0, Random.Range(-20f, 20f), 0));
            // Road
            var road = Empty(world, "Road", Vector3.zero);
            for (int i = 0; i < RoadPts.Length - 1; i++)
            {
                Vector3 a = RoadPts[i], b = RoadPts[i + 1];
                Vector3 mid = (a + b) * 0.5f;
                float len = Vector3.Distance(a, b);
                float yaw = Quaternion.LookRotation(b - a).eulerAngles.y;
                Cube(road, "RoadSeg", mid + Vector3.up * 0.02f, new Vector3(6.5f, 0.02f, len + 6f), "Road", false, new Vector3(0, yaw, 0));
                Cube(road, "RoadLine", mid + Vector3.up * 0.035f, new Vector3(0.25f, 0.01f, len), "RoadLine", false, new Vector3(0, yaw, 0));
            }
            Cube(road, "Depot", new Vector3(0, 0.025f, -4), new Vector3(10, 0.02f, 8), "Road");

            // Mine
            var mineGo = new GameObject("GoldMine");
            mineGo.transform.SetParent(world, false);
            mineGo.transform.localPosition = new Vector3(0, 0, 7);
            var mt = mineGo.transform;
            Cube(mt, "Mound", new Vector3(0, 2.5f, 1), new Vector3(12, 5, 8), "MineRock");
            Cube(mt, "MoundTop", new Vector3(-1, 5.5f, 1.5f), new Vector3(8, 2, 6), "MineRock", false, new Vector3(0, 8, 0));
            Cube(mt, "MoundL", new Vector3(-5.5f, 1.8f, 0.5f), new Vector3(3, 3.6f, 5), "RockDark", false, new Vector3(0, 20, 0));
            Cube(mt, "MoundR", new Vector3(5.4f, 2f, 1.2f), new Vector3(3.2f, 4f, 5.5f), "RockDark", false, new Vector3(0, -15, 0));
            Cube(mt, "Opening", new Vector3(0, 1.5f, -3.02f), new Vector3(3, 3, 0.1f), "Black");
            Cube(mt, "BeamL", new Vector3(-1.65f, 1.6f, -3.1f), new Vector3(0.35f, 3.2f, 0.35f), "Wood");
            Cube(mt, "BeamR", new Vector3(1.65f, 1.6f, -3.1f), new Vector3(0.35f, 3.2f, 0.35f), "Wood");
            Cube(mt, "BeamTop", new Vector3(0, 3.3f, -3.1f), new Vector3(3.8f, 0.4f, 0.45f), "Wood");
            Cube(mt, "Sign", new Vector3(0, 4.0f, -3.15f), new Vector3(2.6f, 0.7f, 0.1f), "WoodDark");
            Cube(mt, "SignGold", new Vector3(0, 4.0f, -3.22f), new Vector3(2.0f, 0.3f, 0.05f), "Gold");
            for (int i = 0; i < 4; i++) Cube(mt, "Rail", new Vector3(i % 2 == 0 ? -0.5f : 0.5f, 0.05f, -4.5f - (i / 2) * 1.5f), new Vector3(0.1f, 0.1f, 1.6f), "Metal");
            for (int i = 0; i < 6; i++) Cube(mt, "GoldVein", new Vector3(Random.Range(-5f, 5f), Random.Range(1f, 4.5f), -3.01f + Random.Range(0f, 0.02f)), new Vector3(Random.Range(0.3f, 0.8f), Random.Range(0.2f, 0.4f), 0.08f), "Gold");
            var mineCol = mineGo.AddComponent<BoxCollider>();
            mineCol.center = new Vector3(0, 3f, 1f); mineCol.size = new Vector3(12f, 6f, 8f);
            var mineHealth = AddHealth(mineGo, Team.Friendly, UnitKind.Mine, config.mine.health[0]);
            BuildMineLevels(mineGo);
            mineGo.AddComponent<Upgradeable>();

            var mineEntrance = Empty(world, "MineEntrance", new Vector3(0, 0, 1.5f));
            var goldPile = Empty(world, "GoldPile", new Vector3(4.5f, 0, 1f));
            Cube(goldPile, "Pile", new Vector3(0, 0.1f, 0), new Vector3(0.5f, 0.2f, 0.5f), "Gold");
            Cube(goldPile, "Cart", new Vector3(-1.4f, 0.45f, 0.3f), new Vector3(1f, 0.6f, 1.4f), "Metal");

            var ores = new List<Transform>();
            foreach (var p in new[] { new Vector3(-17, 0, 9), new Vector3(16, 0, 11), new Vector3(-2, 0, 23) })
            {
                var ore = Empty(world, "OrePile", p);
                ores.Add(ore);
                for (int i = 0; i < 5; i++)
                {
                    var rock = Cube(ore, "OreRock", new Vector3(Random.Range(-1.3f, 1.3f), Random.Range(0.3f, 0.6f), Random.Range(-1.3f, 1.3f)), Vector3.one * Random.Range(0.8f, 1.5f), "Ore", i == 0, new Vector3(Random.Range(0, 40), Random.Range(0, 90), 0));
                    Cube(rock.transform, "Fleck", new Vector3(0.3f, 0.3f, -0.51f), new Vector3(0.3f, 0.2f, 0.05f), "Gold");
                }
            }

            // Desert rocks
            var desert = Empty(world, "Desert", Vector3.zero);
            int placed = 0;
            for (int tries = 0; tries < 400 && placed < 48; tries++)
            {
                var p = new Vector3(Random.Range(-63f, -16f), 0, Random.Range(-63f, 63f));
                if (p.x < -58f && Mathf.Abs(p.z) < 45f) continue;           // keep spawn edge clear
                if (NearRoad(p, 7f) || NearAny(p, ores, 5f)) continue;
                RockSpots.Add(p);
                var rock = Empty(desert, "Rock", p);
                rock.localEulerAngles = new Vector3(0, Random.Range(0, 360), 0);
                float w = Random.Range(1.6f, 3.6f), h = Random.Range(1.6f, 3.2f), d = Random.Range(1.4f, 3f);
                Cube(rock, "Body", new Vector3(0, h / 2f, 0), new Vector3(w, h, d), Random.value < 0.5f ? "Rock" : "RockDark", true);
                Cube(rock, "Top", new Vector3(Random.Range(-0.3f, 0.3f), h + 0.25f, 0), new Vector3(w * 0.6f, 0.6f, d * 0.7f), "Rock", false, new Vector3(0, Random.Range(0, 45), 0));
                placed++;
            }
            for (int i = 0; i < 14; i++)
            {
                var p = new Vector3(Random.Range(-62f, -18f), 0, Random.Range(-62f, 62f));
                if (NearRoad(p, 7f)) continue;
                var cactus = Empty(desert, "Cactus", p);
                Cube(cactus, "Stem", new Vector3(0, 1.1f, 0), new Vector3(0.5f, 2.2f, 0.5f), "Cactus", true);
                Cube(cactus, "Arm", new Vector3(0.45f, 1.3f, 0), new Vector3(0.4f, 0.3f, 0.3f), "Cactus");
                Cube(cactus, "ArmUp", new Vector3(0.6f, 1.65f, 0), new Vector3(0.3f, 0.7f, 0.3f), "Cactus");
            }

            // Forest
            var forest = Empty(world, "Forest", Vector3.zero);
            var trees = new List<Vector3>();
            for (int tries = 0; tries < 5000 && trees.Count < 300; tries++)
            {
                var p = new Vector3(Random.Range(15f, 66f), 0, Random.Range(-66f, 66f));
                if (NearRoad(p, 7f) || NearAny(p, ores, 4f)) continue;
                if (p.x > 60f && Mathf.Abs(p.z) < 45f && Random.value < 0.6f) continue; // thinner at the spawn edge
                bool ok = true;
                foreach (var q in trees) if ((q - p).sqrMagnitude < 3.4f * 3.4f) { ok = false; break; }
                if (!ok) continue;
                trees.Add(p);
                TreeSpots.Add(p);
                var tree = Empty(forest, "Tree", p);
                tree.localEulerAngles = new Vector3(0, Random.Range(0, 90), 0);
                float th = Random.Range(2.2f, 3.2f);
                Cube(tree, "Trunk", new Vector3(0, th / 2f, 0), new Vector3(0.55f, th, 0.55f), "Trunk", true);
                string leaf = Random.value < 0.33f ? "Leaves" : Random.value < 0.5f ? "LeavesDark" : "LeavesLight";
                float s = Random.Range(2.6f, 3.6f);
                Cube(tree, "Canopy", new Vector3(0, th + s * 0.4f, 0), new Vector3(s, s * 0.8f, s), leaf);
                Cube(tree, "CanopyTop", new Vector3(0, th + s * 0.95f, 0), new Vector3(s * 0.6f, s * 0.5f, s * 0.6f), leaf);
            }
            for (int i = 0; i < 70; i++)
            {
                var p = new Vector3(Random.Range(15f, 66f), 0, Random.Range(-66f, 66f));
                if (NearRoad(p, 6f)) continue;
                Cube(forest, "Bush", p + Vector3.up * 0.45f, new Vector3(Random.Range(1f, 1.8f), Random.Range(0.7f, 1.1f), Random.Range(1f, 1.8f)), "LeavesDark", false, new Vector3(0, Random.Range(0, 90), 0));
            }

            // Map border fence posts (visual)
            var border = Empty(world, "Border", Vector3.zero);
            for (int i = -66; i <= 66; i += 6)
            {
                Cube(border, "PostN", new Vector3(i, 0.6f, 67), new Vector3(0.3f, 1.2f, 0.3f), "WoodDark");
                if (Mathf.Abs(i) > 8) Cube(border, "PostS", new Vector3(i, 0.6f, -67), new Vector3(0.3f, 1.2f, 0.3f), "WoodDark");
            }

            foreach (Transform t in world.GetComponentsInChildren<Transform>())
                if (t != world && t.GetComponentInParent<Health>(true) == null)
                    GameObjectUtility.SetStaticEditorFlags(t.gameObject, StaticEditorFlags.BatchingStatic | StaticEditorFlags.ContributeGI);

            // NavMesh
            var surface = world.gameObject.AddComponent<NavMeshSurface>();
            surface.collectObjects = CollectObjects.Children;
            surface.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
            surface.BuildNavMesh();
            string navPath = "Assets/Scenes/HoldTheMine_NavMesh.asset";
            AssetDatabase.DeleteAsset(navPath);
            AssetDatabase.CreateAsset(surface.navMeshData, navPath);
            EditorUtility.SetDirty(surface);

            // Points
            var points = new GameObject("Points").transform;
            Transform[] Pts(string n, params Vector3[] ps)
            {
                var group = Empty(points, n, Vector3.zero);
                var list = new Transform[ps.Length];
                for (int i = 0; i < ps.Length; i++) list[i] = Empty(group, n + i, ps[i]);
                return list;
            }
            var roadPoints = Pts("Road", RoadPts);
            var desertSpawns = Pts("DesertSpawn", new Vector3(-63, 0, -30), new Vector3(-63, 0, -10), new Vector3(-63, 0, 10), new Vector3(-63, 0, 30));
            var forestSpawns = Pts("ForestSpawn", new Vector3(63, 0, -30), new Vector3(63, 0, -10), new Vector3(63, 0, 10), new Vector3(63, 0, 30));
            var roadSpawns = Pts("RoadSpawn", new Vector3(-22, 0, -64), new Vector3(22, 0, -64));
            var ambush = Pts("AmbushPoint", new Vector3(-13, 0, -44), new Vector3(12, 0, -34), new Vector3(-10, 0, -22), new Vector3(10, 0, -56));
            var soldierStarts = Pts("SoldierStart", new Vector3(-9, 0, -3), new Vector3(9, 0, -3));

            var units = new GameObject("Units").transform;
            var enemies = new GameObject("Enemies").transform;

            // Player
            var playerGo = BuildCharacter("Player", new CharLook { body = "PlayerBlue", legs = "PlayerDark", hat = "PlayerDark" }, out var pMuzzle, out _);
            playerGo.transform.position = new Vector3(0, 0, -4);
            var cc = playerGo.AddComponent<CharacterController>();
            cc.center = new Vector3(0, 1.05f, 0); cc.height = 2.1f; cc.radius = 0.42f; cc.skinWidth = 0.05f;
            AddHealth(playerGo, Team.Friendly, UnitKind.Player, config.playerHealth);
            var player = playerGo.AddComponent<PlayerController>();
            player.muzzle = pMuzzle; player.ghostOkMat = M("GhostOk"); player.ghostBadMat = M("GhostBad");
            // little marker so the player stands out
            Cube(playerGo.transform.Find("Model"), "Flag", new Vector3(-0.2f, 2.55f, -0.2f), new Vector3(0.06f, 0.7f, 0.06f), "Black");
            Cube(playerGo.transform.Find("Model"), "FlagCloth", new Vector3(0.0f, 2.75f, -0.2f), new Vector3(0.4f, 0.28f, 0.04f), "PlayerBlue");

            // Camera
            var camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            var cam = camGo.AddComponent<Camera>();
            cam.fieldOfView = 38f;
            cam.nearClipPlane = 0.5f; cam.farClipPlane = 250f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.78f, 0.74f, 0.65f);
            camGo.AddComponent<AudioListener>();
            var iso = camGo.AddComponent<IsoCamera>();
            iso.target = playerGo.transform;
            iso.SnapToTarget();
            player.cam = cam;

            // Managers
            var managers = new GameObject("Managers");
            var gm = managers.AddComponent<GameManager>();
            var waves = managers.AddComponent<WaveManager>();
            var trucks = new GameObject("Trucks").AddComponent<TruckDispatcher>();
            trucks.transform.SetParent(managers.transform);
            gm.config = config;
            gm.player = player;
            gm.mine = mineHealth;
            gm.mineEntrance = mineEntrance;
            gm.goldPile = goldPile;
            gm.orePiles = ores.ToArray();
            gm.soldierStartPoints = soldierStarts;
            gm.unitsRoot = units;
            gm.waves = waves;
            gm.trucks = trucks;
            gm.soldierPrefab = prefabs.soldier;
            gm.minerPrefab = prefabs.miner;
            gm.towerPrefab = prefabs.tower;
            gm.barricadePrefab = prefabs.barricade;
            gm.upgradeMat = M("UpgradeGlow");
            var sel = managers.AddComponent<SelectionManager>();
            sel.lineMat = M("Tracer");
            gm.tracerMat = M("Tracer"); gm.fireMat = M("Fire"); gm.sparkMat = M("Spark"); gm.debrisMat = M("Debris"); gm.grenadeMat = M("GrenadeMat");

            waves.desertSpawns = desertSpawns;
            waves.forestSpawns = forestSpawns;
            waves.roadSpawns = roadSpawns;
            waves.ambushPoints = ambush;
            waves.enemiesRoot = enemies;
            waves.enemyPrefabs = prefabs.enemies;

            trucks.roadPoints = roadPoints;
            trucks.goldTruckPrefab = prefabs.goldTruck;
            trucks.foodTruckPrefab = prefabs.foodTruck;

            // UI
            var canvasGo = new GameObject("Canvas", typeof(RectTransform));
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            canvasGo.AddComponent<GraphicRaycaster>();
            var ui = canvasGo.AddComponent<UIManager>();
            ui.BuildUI(Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"), icons, BuildMinimapSprite());
            PlayerSettings.fullScreenMode = FullScreenMode.FullScreenWindow;
            PlayerSettings.resizableWindow = true;
            gm.ui = ui;

            var es = new GameObject("EventSystem");
            es.AddComponent<EventSystem>();
            es.AddComponent<InputSystemUIInputModule>();

            EditorUtility.SetDirty(gm);
            EditorUtility.SetDirty(ui);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
        }

        static bool NearRoad(Vector3 p, float dist)
        {
            for (int i = 0; i < RoadPts.Length - 1; i++)
            {
                Vector3 a = RoadPts[i], b = RoadPts[i + 1];
                Vector3 ab = b - a;
                float t = Mathf.Clamp01(Vector3.Dot(p - a, ab) / ab.sqrMagnitude);
                if (Vector3.Distance(p, a + ab * t) < dist) return true;
            }
            return p.z < -2f && p.z > -10f && Mathf.Abs(p.x) < 8f;
        }

        static bool NearAny(Vector3 p, List<Transform> ts, float d)
        {
            foreach (var t in ts) if (Vector3.Distance(p, t.position) < d) return true;
            return false;
        }
    }
}
