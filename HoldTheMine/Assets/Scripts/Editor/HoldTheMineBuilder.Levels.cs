using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;

namespace HoldTheMine.EditorTools
{
    /// <summary>Level I–III models, pixel-art UI icons and the minimap picture.</summary>
    public static partial class HoldTheMineBuilder
    {
        static readonly List<Vector3> RockSpots = new List<Vector3>();
        static readonly List<Vector3> TreeSpots = new List<Vector3>();

        static void BuildLevelMaterials()
        {
            Lit("SoldierOlive", "#6B7F2E"); Lit("SoldierOliveDark", "#3F4A1E"); Lit("SoldierCamo", "#3D5A44"); Lit("SoldierCamoDark", "#1F2E26");
            Lit("HelmetOlive", "#4F5B2A"); Lit("Khaki", "#B59A63"); Lit("SteelDark", "#3A3F46", 0.5f, 0.6f); Lit("Visor", "#1C2E4A", 0.8f, 0.2f);
            Lit("HiVis", "#C9E43A"); Lit("Reflective", "#E6E6E6", 0.7f); Lit("MinerBlue", "#2F5DB8"); Lit("HardHatOrange", "#FF8C1A"); Lit("HardHatWhite", "#F2F2F2");
            Unlit("Lamp", "#FFF6B0"); Lit("ConcreteDark", "#6C7074"); Lit("Hazard", "#F2C230"); Lit("RoofRed", "#A8342A"); Lit("SteelRed", "#B23A2E", 0.4f, 0.4f);
            Lit("Glass", "#7FB8D8", 0.9f); Unlit("UpgradeGlow", "#FFD84A"); Lit("GoldDark", "#C99A12", 0.6f, 0.7f);
        }

        static void Parts(List<GameObject> list, params GameObject[] gos) { list.AddRange(gos); }

        // =====================================================================
        // Soldier: cap → helmet + vest → heavy helmet, visor, steel armour. Rifle → long rifle → heavy scoped rifle.
        // =====================================================================
        static GameObject BuildSoldier(GameConfig c)
        {
            var go = BuildCharacter("Soldier", new CharLook { body = "SoldierGreen", legs = "SoldierDark", gun = false }, out _, out _);
            var model = go.transform.Find("Model");
            var l1 = new List<GameObject>(); var l2 = new List<GameObject>(); var l3 = new List<GameObject>();

            // Level I
            Parts(l1, Cube(model, "Cap", new Vector3(0, 2.2f, 0), new Vector3(0.56f, 0.16f, 0.56f), "SoldierDark"),
                      Cube(model, "CapBrim", new Vector3(0, 2.14f, 0.32f), new Vector3(0.4f, 0.05f, 0.22f), "SoldierDark"));
            var gun1 = Empty(model, "Rifle1", new Vector3(0.18f, 1.3f, 0.45f));
            Cube(gun1, "Barrel", new Vector3(0, 0, 0.25f), new Vector3(0.12f, 0.14f, 0.95f), "Black");
            Cube(gun1, "Stock", new Vector3(0, -0.1f, -0.1f), new Vector3(0.1f, 0.22f, 0.3f), "WoodDark");
            var m1 = Empty(gun1, "Muzzle", new Vector3(0, 0.02f, 0.78f));
            l1.Add(gun1.gameObject);

            // Level II
            Parts(l2, Cube(model, "Helmet", new Vector3(0, 2.2f, 0), new Vector3(0.64f, 0.24f, 0.64f), "HelmetOlive"),
                      Cube(model, "HelmetRim", new Vector3(0, 2.1f, 0), new Vector3(0.7f, 0.06f, 0.7f), "HelmetOlive"),
                      Cube(model, "Vest", new Vector3(0, 1.22f, 0), new Vector3(0.86f, 0.62f, 0.52f), "Khaki"),
                      Cube(model, "PouchL", new Vector3(-0.2f, 1.05f, 0.29f), new Vector3(0.2f, 0.2f, 0.1f), "SoldierOliveDark"),
                      Cube(model, "PouchR", new Vector3(0.2f, 1.05f, 0.29f), new Vector3(0.2f, 0.2f, 0.1f), "SoldierOliveDark"));
            var gun2 = Empty(model, "Rifle2", new Vector3(0.18f, 1.3f, 0.45f));
            Cube(gun2, "Barrel", new Vector3(0, 0, 0.35f), new Vector3(0.12f, 0.15f, 1.2f), "Metal");
            Cube(gun2, "Mag", new Vector3(0, -0.17f, 0.25f), new Vector3(0.08f, 0.25f, 0.14f), "Black");
            Cube(gun2, "Stock", new Vector3(0, -0.08f, -0.15f), new Vector3(0.1f, 0.22f, 0.35f), "Black");
            var m2 = Empty(gun2, "Muzzle", new Vector3(0, 0.02f, 0.98f));
            l2.Add(gun2.gameObject);

            // Level III
            Parts(l3, Cube(model, "HeavyHelmet", new Vector3(0, 2.22f, -0.02f), new Vector3(0.68f, 0.3f, 0.68f), "SteelDark"),
                      Cube(model, "Visor", new Vector3(0, 1.94f, 0.28f), new Vector3(0.54f, 0.16f, 0.05f), "Visor"),
                      Cube(model, "ChestPlate", new Vector3(0, 1.2f, 0), new Vector3(0.9f, 0.72f, 0.56f), "SteelDark"),
                      Cube(model, "PlateStripe", new Vector3(0, 1.35f, 0.285f), new Vector3(0.6f, 0.08f, 0.02f), "Hazard"),
                      Cube(model, "ShoulderL", new Vector3(-0.54f, 1.6f, 0), new Vector3(0.36f, 0.18f, 0.52f), "SteelDark"),
                      Cube(model, "ShoulderR", new Vector3(0.54f, 1.6f, 0), new Vector3(0.36f, 0.18f, 0.52f), "SteelDark"),
                      Cube(model, "Belt", new Vector3(0, 0.78f, 0), new Vector3(0.84f, 0.12f, 0.5f), "Black"));
            var gun3 = Empty(model, "Rifle3", new Vector3(0.18f, 1.3f, 0.45f));
            Cube(gun3, "Barrel", new Vector3(0, 0, 0.4f), new Vector3(0.16f, 0.2f, 1.35f), "SteelDark");
            Cube(gun3, "Scope", new Vector3(0, 0.17f, 0.25f), new Vector3(0.09f, 0.1f, 0.45f), "Black");
            Cube(gun3, "Drum", new Vector3(0, -0.2f, 0.25f), new Vector3(0.14f, 0.26f, 0.26f), "Black");
            Cube(gun3, "Stock", new Vector3(0, -0.06f, -0.2f), new Vector3(0.12f, 0.24f, 0.4f), "SteelDark");
            var m3 = Empty(gun3, "Muzzle", new Vector3(0, 0.02f, 1.1f));
            l3.Add(gun3.gameObject);

            AddCapsule(go);
            AddHealth(go, Team.Friendly, UnitKind.Soldier, c.soldier.health[0]);
            AddAgent(go, c.soldierSpeed, 0.4f);
            var ai = go.AddComponent<SoldierAI>(); ai.muzzle = m1;
            var lv = go.AddComponent<LevelVisuals>();
            lv.onlyL1 = l1.ToArray(); lv.onlyL2 = l2.ToArray(); lv.onlyL3 = l3.ToArray();
            lv.tintA = new[] { R(model, "Body"), R(model, "ArmL/Arm"), R(model, "ArmR/Arm") };
            lv.tintAMats = new[] { M("SoldierGreen"), M("SoldierOlive"), M("SoldierCamo") };
            lv.tintB = new[] { R(model, "LegL/Leg"), R(model, "LegR/Leg") };
            lv.tintBMats = new[] { M("SoldierDark"), M("SoldierOliveDark"), M("SoldierCamoDark") };
            lv.muzzles = new[] { m1, m2, m3 };
            lv.SetLevel(1);
            go.AddComponent<Upgradeable>();
            return SavePrefab(go, "Soldier");
        }

        static Renderer R(Transform root, string path) => root.Find(path).GetComponent<Renderer>();

        // =====================================================================
        // Miner: orange shirt + yellow hard hat → hi-vis vest + headlamp → blue overalls, white hat, gold pickaxe, ore pack.
        // =====================================================================
        static GameObject BuildMiner(GameConfig c)
        {
            var go = BuildCharacter("Miner", new CharLook { body = "MinerShirt", legs = "MinerJeans", gun = false, armsHoldGun = false }, out _, out _);
            var model = go.transform.Find("Model");
            var armR = model.Find("ArmR");
            var l1 = new List<GameObject>(); var l2 = new List<GameObject>(); var l3 = new List<GameObject>(); var f2 = new List<GameObject>();

            Cube(armR, "Handle", new Vector3(0, -0.8f, 0.3f), new Vector3(0.07f, 0.07f, 0.8f), "Wood");
            Parts(l1, Cube(model, "HardHat", new Vector3(0, 2.2f, 0), new Vector3(0.58f, 0.2f, 0.58f), "HardHat"),
                      Cube(model, "Brim", new Vector3(0, 2.12f, 0.1f), new Vector3(0.66f, 0.05f, 0.72f), "HardHat"),
                      Cube(armR, "PickHead", new Vector3(0, -0.8f, 0.68f), new Vector3(0.08f, 0.5f, 0.08f), "Metal"));
            Parts(l2, Cube(model, "HardHat", new Vector3(0, 2.2f, 0), new Vector3(0.6f, 0.22f, 0.6f), "HardHatOrange"),
                      Cube(model, "Brim", new Vector3(0, 2.12f, 0.1f), new Vector3(0.68f, 0.05f, 0.74f), "HardHatOrange"),
                      Cube(armR, "PickHead", new Vector3(0, -0.8f, 0.68f), new Vector3(0.1f, 0.6f, 0.1f), "Metal"));
            Parts(f2, Cube(model, "Lamp", new Vector3(0, 2.2f, 0.31f), new Vector3(0.16f, 0.12f, 0.06f), "Lamp"),
                      Cube(model, "StripeTop", new Vector3(0, 1.35f, 0), new Vector3(0.82f, 0.07f, 0.47f), "Reflective"),
                      Cube(model, "StripeLow", new Vector3(0, 1.0f, 0), new Vector3(0.82f, 0.07f, 0.47f), "Reflective"));
            Parts(l3, Cube(model, "HardHat", new Vector3(0, 2.22f, 0), new Vector3(0.62f, 0.24f, 0.62f), "HardHatWhite"),
                      Cube(model, "Brim", new Vector3(0, 2.13f, 0.1f), new Vector3(0.7f, 0.05f, 0.76f), "HardHatWhite"),
                      Cube(model, "GoldBelt", new Vector3(0, 0.8f, 0), new Vector3(0.84f, 0.12f, 0.5f), "Gold"),
                      Cube(model, "OrePack", new Vector3(0, 1.3f, -0.36f), new Vector3(0.6f, 0.7f, 0.3f), "Crate"),
                      Cube(armR, "PickHead", new Vector3(0, -0.8f, 0.7f), new Vector3(0.12f, 0.72f, 0.12f), "Gold"));

            AddCapsule(go);
            AddHealth(go, Team.Friendly, UnitKind.Miner, c.miner.health[0]);
            AddAgent(go, c.miner.walkSpeed[0], 0.3f);
            var carry = Cube(model, "GoldSack", new Vector3(0, 1.3f, -0.45f), new Vector3(0.55f, 0.55f, 0.35f), "Gold");
            var ai = go.AddComponent<MinerAI>(); ai.carryVisual = carry;
            var lv = go.AddComponent<LevelVisuals>();
            lv.onlyL1 = l1.ToArray(); lv.onlyL2 = l2.ToArray(); lv.onlyL3 = l3.ToArray(); lv.fromL2 = f2.ToArray();
            lv.tintA = new[] { R(model, "Body"), R(model, "ArmL/Arm"), R(model, "ArmR/Arm") };
            lv.tintAMats = new[] { M("MinerShirt"), M("HiVis"), M("MinerBlue") };
            lv.tintB = new[] { R(model, "LegL/Leg"), R(model, "LegR/Leg") };
            lv.tintBMats = new[] { M("MinerJeans"), M("MinerJeans"), M("SoldierCamoDark") };
            lv.SetLevel(1);
            go.AddComponent<Upgradeable>();
            return SavePrefab(go, "Miner");
        }

        // =====================================================================
        // Barricade: sandbags & planks → concrete barrier → riveted steel wall with hazard stripes
        // =====================================================================
        static GameObject BuildBarricade(GameConfig c)
        {
            var go = new GameObject("Barricade");
            var t = go.transform;

            var v1 = Empty(t, "Sandbags_L1", Vector3.zero);
            for (int i = 0; i < 3; i++) Cube(v1, "Sandbags" + i, new Vector3(-1.05f + i * 1.05f, 0.3f, 0.1f), new Vector3(1f, 0.6f, 0.6f), "Sandbag");
            for (int i = 0; i < 2; i++) Cube(v1, "Sandbags2" + i, new Vector3(-0.52f + i * 1.05f, 0.85f, 0.1f), new Vector3(1f, 0.5f, 0.55f), "Sandbag");
            Cube(v1, "Plank", new Vector3(0, 0.95f, -0.25f), new Vector3(3.2f, 0.25f, 0.12f), "Wood", false, new Vector3(0, 0, 4));
            Cube(v1, "PostL", new Vector3(-1.5f, 0.75f, -0.25f), new Vector3(0.18f, 1.5f, 0.18f), "WoodDark");
            Cube(v1, "PostR", new Vector3(1.5f, 0.75f, -0.25f), new Vector3(0.18f, 1.5f, 0.18f), "WoodDark");

            var v2 = Empty(t, "Concrete_L2", Vector3.zero);
            Cube(v2, "Base", new Vector3(0, 0.22f, 0), new Vector3(3.2f, 0.45f, 0.95f), "Concrete");
            Cube(v2, "Wall", new Vector3(0, 0.87f, 0), new Vector3(3.0f, 0.85f, 0.5f), "Concrete");
            Cube(v2, "Cap", new Vector3(0, 1.33f, 0), new Vector3(3.04f, 0.08f, 0.54f), "ConcreteDark");
            for (int i = -1; i <= 1; i += 2) Cube(v2, "Seam", new Vector3(i * 0.8f, 0.65f, 0), new Vector3(0.05f, 1.32f, 0.98f), "ConcreteDark");
            for (int i = 0; i < 3; i++) Cube(v2, "Rebar", new Vector3(-1f + i, 1.45f, 0), new Vector3(0.05f, 0.2f, 0.05f), "Metal");

            var v3 = Empty(t, "Steel_L3", Vector3.zero);
            Cube(v3, "Plate", new Vector3(0, 0.85f, 0), new Vector3(3.2f, 1.7f, 0.24f), "SteelDark");
            for (int r = 0; r < 2; r++)
                for (int i = 0; i < 6; i++) Cube(v3, "Rivet", new Vector3(-1.35f + i * 0.54f, 0.35f + r * 0.75f, -0.13f), new Vector3(0.08f, 0.08f, 0.04f), "Metal");
            for (int i = 0; i < 8; i++) Cube(v3, "Hazard" + i, new Vector3(-1.4f + i * 0.4f, 1.48f, -0.01f), new Vector3(0.4f, 0.18f, 0.27f), i % 2 == 0 ? "Hazard" : "Black");
            for (int i = -1; i <= 1; i += 2) Cube(v3, "Brace", new Vector3(i * 1.2f, 0.65f, 0.45f), new Vector3(0.15f, 1.4f, 0.15f), "Metal", false, new Vector3(-35, 0, 0));
            for (int i = 0; i < 6; i++) Cube(v3, "Spike", new Vector3(-1.4f + i * 0.56f, 1.78f, 0), new Vector3(0.14f, 0.14f, 0.14f), "Metal", false, new Vector3(45, 0, 45));

            var bc = go.AddComponent<BoxCollider>(); bc.center = new Vector3(0, 0.75f, 0); bc.size = new Vector3(3.2f, 1.5f, 0.8f);
            AddHealth(go, Team.Friendly, UnitKind.Barricade, c.barricade.health[0]);
            var ob = go.AddComponent<NavMeshObstacle>(); ob.shape = NavMeshObstacleShape.Box; ob.center = bc.center; ob.size = bc.size; ob.carving = true;
            var lv = go.AddComponent<LevelVisuals>();
            lv.onlyL1 = new[] { v1.gameObject }; lv.onlyL2 = new[] { v2.gameObject }; lv.onlyL3 = new[] { v3.gameObject };
            lv.boxCenters = new[] { new Vector3(0, 0.75f, 0), new Vector3(0, 0.7f, 0), new Vector3(0, 0.9f, 0) };
            lv.boxSizes = new[] { new Vector3(3.2f, 1.5f, 0.8f), new Vector3(3.2f, 1.4f, 0.95f), new Vector3(3.2f, 1.8f, 0.8f) };
            lv.SetLevel(1);
            go.AddComponent<Upgradeable>();
            return SavePrefab(go, "Barricade");
        }

        // =====================================================================
        // Tower: wooden watchtower → taller concrete tower with ladder → steel tower with a roof, twin guns and searchlight
        // =====================================================================
        static GameObject BuildTower(GameConfig c)
        {
            var go = new GameObject("Tower");
            var t = go.transform;
            var pivots = new Transform[3];
            var muzzles = new Transform[3];

            // Level I
            var v1 = Empty(t, "Tower_L1", Vector3.zero);
            foreach (var p in Corners(1f)) Cube(v1, "Post", new Vector3(p.x, 1.5f, p.y), new Vector3(0.3f, 3f, 0.3f), "WoodDark");
            Cube(v1, "BraceA", new Vector3(0, 1.4f, -1f), new Vector3(2.6f, 0.15f, 0.15f), "Wood", false, new Vector3(0, 0, 40));
            Cube(v1, "BraceB", new Vector3(0, 1.4f, 1f), new Vector3(2.6f, 0.15f, 0.15f), "Wood", false, new Vector3(0, 0, -40));
            Cube(v1, "Platform", new Vector3(0, 3.05f, 0), new Vector3(2.7f, 0.2f, 2.7f), "Wood");
            Rails(v1, 1.25f, 3.45f, 2.7f, 0.6f, 0.2f, "Sandbag");
            Gunner(v1, 3.15f, "SoldierGreen", "SoldierDark", 1.3f, "Black", false, out pivots[0], out muzzles[0]);

            // Level II
            var v2 = Empty(t, "Tower_L2", Vector3.zero);
            foreach (var p in Corners(1.15f)) Cube(v2, "Leg", new Vector3(p.x, 2.15f, p.y), new Vector3(0.45f, 4.3f, 0.45f), "Concrete");
            foreach (var z in new[] { -1.15f, 1.15f })
            {
                Cube(v2, "X1", new Vector3(0, 2.1f, z), new Vector3(3.0f, 0.14f, 0.14f), "Metal", false, new Vector3(0, 0, 50));
                Cube(v2, "X2", new Vector3(0, 2.1f, z), new Vector3(3.0f, 0.14f, 0.14f), "Metal", false, new Vector3(0, 0, -50));
            }
            Cube(v2, "Platform", new Vector3(0, 4.38f, 0), new Vector3(3.0f, 0.25f, 3.0f), "Wood");
            Rails(v2, 1.4f, 4.85f, 3.0f, 0.7f, 0.15f, "WoodDark");
            foreach (var p in Corners(1.3f)) Cube(v2, "Sandbag", new Vector3(p.x, 4.7f, p.y), new Vector3(0.5f, 0.4f, 0.5f), "Sandbag");
            for (int i = -1; i <= 1; i += 2) Cube(v2, "LadderRail", new Vector3(i * 0.3f, 2.15f, -1.65f), new Vector3(0.08f, 4.3f, 0.08f), "Metal");
            for (int i = 0; i < 8; i++) Cube(v2, "Rung", new Vector3(0, 0.4f + i * 0.5f, -1.65f), new Vector3(0.6f, 0.06f, 0.06f), "Metal");
            Gunner(v2, 4.5f, "SoldierOlive", "HelmetOlive", 1.5f, "Metal", false, out pivots[1], out muzzles[1]);

            // Level III
            var v3 = Empty(t, "Tower_L3", Vector3.zero);
            Cube(v3, "Base", new Vector3(0, 0.5f, 0), new Vector3(3.4f, 1f, 3.4f), "ConcreteDark");
            foreach (var p in Corners(1.3f)) Cube(v3, "Leg", new Vector3(p.x, 3.4f, p.y), new Vector3(0.4f, 4.8f, 0.4f), "SteelDark");
            foreach (var y in new[] { 2.6f, 4.3f })
            {
                Cube(v3, "GirderN", new Vector3(0, y, 1.3f), new Vector3(2.6f, 0.2f, 0.2f), "SteelRed");
                Cube(v3, "GirderS", new Vector3(0, y, -1.3f), new Vector3(2.6f, 0.2f, 0.2f), "SteelRed");
                Cube(v3, "GirderE", new Vector3(1.3f, y, 0), new Vector3(0.2f, 0.2f, 2.6f), "SteelRed");
                Cube(v3, "GirderW", new Vector3(-1.3f, y, 0), new Vector3(0.2f, 0.2f, 2.6f), "SteelRed");
            }
            Cube(v3, "Platform", new Vector3(0, 5.85f, 0), new Vector3(3.4f, 0.3f, 3.4f), "SteelDark");
            Rails(v3, 1.6f, 6.4f, 3.4f, 0.8f, 0.15f, "Metal");
            Rails(v3, 1.61f, 6.75f, 3.42f, 0.15f, 0.17f, "Hazard");
            foreach (var p in Corners(1.5f)) Cube(v3, "RoofPost", new Vector3(p.x, 7.3f, p.y), new Vector3(0.15f, 2.2f, 0.15f), "SteelDark");
            Cube(v3, "Roof", new Vector3(0, 8.45f, 0), new Vector3(3.8f, 0.25f, 3.8f), "RoofRed");
            Cube(v3, "RoofTop", new Vector3(0, 8.8f, 0), new Vector3(2.4f, 0.5f, 2.4f), "RoofRed", false, new Vector3(0, 45, 0));
            Cube(v3, "FlagPole", new Vector3(0, 9.6f, 0), new Vector3(0.08f, 1.4f, 0.08f), "Black");
            Cube(v3, "Flag", new Vector3(0.35f, 10f, 0), new Vector3(0.65f, 0.4f, 0.04f), "PlayerBlue");
            Cube(v3, "Searchlight", new Vector3(1.5f, 6.95f, 1.5f), new Vector3(0.4f, 0.3f, 0.4f), "Lamp");
            Gunner(v3, 5.95f, "SoldierCamo", "SteelDark", 1.6f, "SteelDark", true, out pivots[2], out muzzles[2]);

            var bc = go.AddComponent<BoxCollider>(); bc.center = new Vector3(0, 2.2f, 0); bc.size = new Vector3(2.7f, 4.4f, 2.7f);
            AddHealth(go, Team.Friendly, UnitKind.Tower, c.tower.health[0]);
            var ob = go.AddComponent<NavMeshObstacle>(); ob.shape = NavMeshObstacleShape.Box; ob.center = bc.center; ob.size = bc.size; ob.carving = true;
            var ai = go.AddComponent<SoldierAI>(); ai.isTower = true; ai.muzzle = muzzles[0]; ai.turretPivot = pivots[0];
            var lv = go.AddComponent<LevelVisuals>();
            lv.onlyL1 = new[] { v1.gameObject }; lv.onlyL2 = new[] { v2.gameObject }; lv.onlyL3 = new[] { v3.gameObject };
            lv.pivots = pivots; lv.muzzles = muzzles;
            lv.boxCenters = new[] { new Vector3(0, 2.2f, 0), new Vector3(0, 2.9f, 0), new Vector3(0, 4.4f, 0) };
            lv.boxSizes = new[] { new Vector3(2.7f, 4.4f, 2.7f), new Vector3(3.0f, 5.8f, 3.0f), new Vector3(3.4f, 8.8f, 3.4f) };
            lv.SetLevel(1);
            go.AddComponent<Upgradeable>();
            return SavePrefab(go, "Tower");
        }

        static Vector2[] Corners(float d) => new[] { new Vector2(-d, -d), new Vector2(d, -d), new Vector2(-d, d), new Vector2(d, d) };

        static void Rails(Transform parent, float offset, float y, float length, float height, float thick, string mat)
        {
            Cube(parent, "RailN", new Vector3(0, y, offset), new Vector3(length, height, thick), mat);
            Cube(parent, "RailS", new Vector3(0, y, -offset), new Vector3(length, height, thick), mat);
            Cube(parent, "RailE", new Vector3(offset, y, 0), new Vector3(thick, height, length), mat);
            Cube(parent, "RailW", new Vector3(-offset, y, 0), new Vector3(thick, height, length), mat);
        }

        static void Gunner(Transform parent, float y, string body, string helmet, float gunLength, string gunMat, bool twin, out Transform pivot, out Transform muzzle)
        {
            pivot = Empty(parent, "Turret", new Vector3(0, y, 0));
            Cube(pivot, "GunnerBody", new Vector3(0, 0.75f, 0), new Vector3(0.7f, 0.8f, 0.45f), body);
            Cube(pivot, "GunnerHead", new Vector3(0, 1.4f, 0), new Vector3(0.48f, 0.48f, 0.48f), "Skin");
            Cube(pivot, "GunnerHelmet", new Vector3(0, 1.68f, 0), new Vector3(0.58f, 0.2f, 0.58f), helmet);
            if (twin)
            {
                Cube(pivot, "MG_L", new Vector3(-0.18f, 0.95f, 0.15f + gunLength / 2f), new Vector3(0.16f, 0.2f, gunLength), gunMat);
                Cube(pivot, "MG_R", new Vector3(0.18f, 0.95f, 0.15f + gunLength / 2f), new Vector3(0.16f, 0.2f, gunLength), gunMat);
                Cube(pivot, "AmmoBox", new Vector3(0.45f, 0.8f, 0.4f), new Vector3(0.3f, 0.3f, 0.4f), "SoldierOliveDark");
            }
            else Cube(pivot, "MG", new Vector3(0, 0.95f, 0.1f + gunLength / 2f), new Vector3(0.18f, 0.2f, gunLength), gunMat);
            muzzle = Empty(pivot, "Muzzle", new Vector3(0, 0.95f, 0.2f + gunLength));
        }

        // =====================================================================
        // Gold mine: small headframe → big wooden headframe + shed + lamps → steel headframe + processing plant + silo
        // =====================================================================
        static void BuildMineLevels(GameObject mineGo)
        {
            var mt = mineGo.transform;
            var l1 = new List<GameObject>(); var l2 = new List<GameObject>(); var l3 = new List<GameObject>();
            var f2 = new List<GameObject>(); var f3 = new List<GameObject>();
            var hfPos = new Vector3(2.8f, 5f, 0.2f);

            // L1 headframe
            var h1 = Empty(mt, "Headframe_L1", hfPos);
            for (int i = -1; i <= 1; i += 2) Cube(h1, "Post", new Vector3(i * 0.8f, 1.5f, 0), new Vector3(0.25f, 3f, 0.25f), "Wood");
            Cube(h1, "Cross", new Vector3(0, 3f, 0), new Vector3(2f, 0.25f, 0.3f), "Wood");
            Cube(h1, "Wheel", new Vector3(0, 3.4f, 0), new Vector3(0.12f, 1f, 1f), "Metal", false, new Vector3(45, 0, 0));
            l1.Add(h1.gameObject);

            // L2 headframe (wood, bigger)
            var h2 = Empty(mt, "Headframe_L2", hfPos);
            foreach (var p in Corners(1f)) Cube(h2, "Leg", new Vector3(p.x, 3f, p.y * 0.8f), new Vector3(0.3f, 6f, 0.3f), "Wood");
            for (int k = 1; k <= 2; k++) Cube(h2, "Girt", new Vector3(0, k * 2f, -0.8f), new Vector3(2.2f, 0.18f, 0.18f), "WoodDark");
            Cube(h2, "Deck", new Vector3(0, 6.1f, 0), new Vector3(2.8f, 0.3f, 2.2f), "WoodDark");
            Cube(h2, "WheelA", new Vector3(0, 7.1f, 0), new Vector3(0.18f, 1.9f, 1.9f), "Metal", false, new Vector3(45, 0, 0));
            Cube(h2, "WheelB", new Vector3(0, 7.1f, 0), new Vector3(0.18f, 1.9f, 1.9f), "Metal");
            Cube(h2, "Backstay", new Vector3(0, 3f, 2f), new Vector3(0.25f, 6.5f, 0.25f), "Wood", false, new Vector3(-28, 0, 0));
            l2.Add(h2.gameObject);

            // L3 headframe (steel, tall, twin wheels)
            var h3 = Empty(mt, "Headframe_L3", hfPos);
            foreach (var p in Corners(1.2f)) Cube(h3, "Leg", new Vector3(p.x, 4.5f, p.y * 0.85f), new Vector3(0.35f, 9f, 0.35f), "SteelRed");
            for (int k = 1; k <= 3; k++)
            {
                Cube(h3, "GirtF", new Vector3(0, k * 2.4f, -1f), new Vector3(2.6f, 0.2f, 0.2f), "SteelDark");
                Cube(h3, "GirtB", new Vector3(0, k * 2.4f, 1f), new Vector3(2.6f, 0.2f, 0.2f), "SteelDark");
            }
            Cube(h3, "Deck", new Vector3(0, 9.1f, 0), new Vector3(3.4f, 0.35f, 2.8f), "SteelDark");
            for (int i = -1; i <= 1; i += 2)
            {
                Cube(h3, "WheelA", new Vector3(i * 0.7f, 10.4f, 0), new Vector3(0.2f, 2.4f, 2.4f), "SteelDark", false, new Vector3(45, 0, 0));
                Cube(h3, "WheelB", new Vector3(i * 0.7f, 10.4f, 0), new Vector3(0.2f, 2.4f, 2.4f), "SteelDark");
                Cube(h3, "Hub", new Vector3(i * 0.7f, 10.4f, 0), new Vector3(0.3f, 0.5f, 0.5f), "Hazard");
            }
            Cube(h3, "Backstay", new Vector3(0, 4.5f, 2.6f), new Vector3(0.3f, 9.6f, 0.3f), "SteelRed", false, new Vector3(-26, 0, 0));
            Cube(h3, "Beacon", new Vector3(0, 12f, 0), new Vector3(0.35f, 0.35f, 0.35f), "Lamp");
            l3.Add(h3.gameObject);

            // L2+: shed, lamp posts, bigger sign
            var shed = Building(mt, "Shed", new Vector3(-9.5f, 0, 3.5f), new Vector3(4f, 3f, 3.5f), "Wood");
            Cube(shed, "Roof", new Vector3(0, 3.15f, 0), new Vector3(4.4f, 0.3f, 3.9f), "RoofRed");
            Cube(shed, "Gable", new Vector3(0, 3.55f, 0), new Vector3(4.4f, 0.6f, 2.4f), "RoofRed");
            Cube(shed, "Door", new Vector3(0.8f, 0.9f, -1.76f), new Vector3(1f, 1.8f, 0.05f), "WoodDark");
            Cube(shed, "Window", new Vector3(-0.9f, 1.8f, -1.76f), new Vector3(0.8f, 0.6f, 0.05f), "Glass");
            f2.Add(shed.gameObject);
            for (int i = -1; i <= 1; i += 2)
            {
                var lamp = Empty(mt, "LampPost", new Vector3(i * 3.4f, 0, -4.2f));
                Cube(lamp, "Pole", new Vector3(0, 1.5f, 0), new Vector3(0.15f, 3f, 0.15f), "Black");
                Cube(lamp, "Light", new Vector3(0, 3.1f, 0), new Vector3(0.35f, 0.25f, 0.35f), "Lamp");
                f2.Add(lamp.gameObject);
            }

            // L3: processing plant, silo, conveyor, gold sign, extra rock
            var plant = Building(mt, "ProcessingPlant", new Vector3(10f, 0, 3.5f), new Vector3(5f, 4.5f, 4.5f), "Concrete");
            Cube(plant, "Roof", new Vector3(0, 4.7f, 0), new Vector3(5.4f, 0.4f, 4.9f), "SteelDark");
            Cube(plant, "Chimney", new Vector3(1.5f, 6f, 1f), new Vector3(0.8f, 3f, 0.8f), "ConcreteDark");
            Cube(plant, "ChimneyStripe", new Vector3(1.5f, 7.2f, 1f), new Vector3(0.84f, 0.3f, 0.84f), "SteelRed");
            Cube(plant, "Windows", new Vector3(0, 3f, -2.26f), new Vector3(4.2f, 0.6f, 0.05f), "Glass");
            Cube(plant, "Door", new Vector3(-1.4f, 1.2f, -2.26f), new Vector3(1.6f, 2.4f, 0.05f), "SteelDark");
            Cube(plant, "HazardBand", new Vector3(0, 0.25f, -2.27f), new Vector3(5f, 0.2f, 0.05f), "Hazard");
            f3.Add(plant.gameObject);
            var silo = Building(mt, "Silo", new Vector3(-5f, 0, 8.5f), new Vector3(2.4f, 6f, 2.4f), "Metal");
            Cube(silo, "SiloTop", new Vector3(0, 6.3f, 0), new Vector3(1.8f, 0.8f, 1.8f), "Metal", false, new Vector3(0, 45, 0));
            Cube(silo, "Band", new Vector3(0, 3f, 0), new Vector3(2.46f, 0.25f, 2.46f), "SteelRed");
            f3.Add(silo.gameObject);
            Parts(f3, Cube(mt, "Conveyor", new Vector3(-2.8f, 6.4f, 6f), new Vector3(0.5f, 0.3f, 5f), "SteelDark", false, new Vector3(-12, 35, 0)),
                      Cube(mt, "BigSign", new Vector3(0, 4.75f, -3.25f), new Vector3(3.4f, 0.55f, 0.08f), "GoldDark"),
                      Cube(mt, "BigSignGold", new Vector3(0, 4.75f, -3.3f), new Vector3(3f, 0.3f, 0.05f), "Gold"),
                      Cube(mt, "MoundExtra", new Vector3(-6.5f, 2.6f, 3f), new Vector3(3.5f, 5.2f, 4f), "MineRock", false, new Vector3(0, 12, 0)));

            var lv = mineGo.AddComponent<LevelVisuals>();
            lv.onlyL1 = l1.ToArray(); lv.onlyL2 = l2.ToArray(); lv.onlyL3 = l3.ToArray();
            lv.fromL2 = f2.ToArray(); lv.fromL3 = f3.ToArray();
            lv.SetLevel(1);
        }

        /// <summary>A box building that carves the NavMesh only while it is active.</summary>
        static Transform Building(Transform parent, string name, Vector3 pos, Vector3 size, string mat)
        {
            var root = Empty(parent, name, pos);
            Cube(root, "Walls", new Vector3(0, size.y / 2f, 0), size, mat);
            var ob = root.gameObject.AddComponent<NavMeshObstacle>();
            ob.shape = NavMeshObstacleShape.Box; ob.center = new Vector3(0, size.y / 2f, 0); ob.size = size; ob.carving = true;
            return root;
        }

        // =====================================================================
        // Pixel-art icons (12x12, upscaled 4x, point filtered)
        // =====================================================================
        static readonly Dictionary<char, Color> Palette = new Dictionary<char, Color>
        {
            { 'K', new Color(0.08f, 0.07f, 0.06f) }, { 'Y', new Color(1f, 0.84f, 0.2f) }, { 'O', new Color(0.8f, 0.55f, 0.1f) },
            { 'W', Color.white }, { 'R', new Color(0.9f, 0.22f, 0.18f) }, { 'G', new Color(0.35f, 0.75f, 0.3f) },
            { 'B', new Color(0.3f, 0.55f, 1f) }, { 'N', new Color(0.62f, 0.4f, 0.2f) }, { 'T', new Color(0.85f, 0.75f, 0.5f) },
            { 'S', new Color(0.65f, 0.67f, 0.7f) }, { 'D', new Color(0.35f, 0.36f, 0.4f) }, { 'P', new Color(0.95f, 0.78f, 0.6f) },
            { 'L', new Color(0.55f, 0.85f, 1f) },
        };

        static readonly Dictionary<string, string[]> IconArt = new Dictionary<string, string[]>
        {
            { "coin", new[] { "...KKKKKK...", "..KYYYYYYK..", ".KYYOOOOYYK.", "KYYOYYYYOYYK", "KYOYYOOYYOYK", "KYOYOYYYYOYK", "KYOYYOOYYOYK", "KYOYYYYOYOYK", "KYYOYOOYOYYK", ".KYYOOOOYYK.", "..KYYYYYYK..", "...KKKKKK..." } },
            { "gold", new[] { "............", "............", "....KKKKKK..", "...KWYYYOK..", "..KYYYYYOK..", "..KKKKKKKKK.", ".KWYYYOKYYOK", "KYYYYOKYYYOK", "KYYYYOKYYYOK", "KOOOOOKOOOOK", "KKKKKKKKKKKK", "............" } },
            { "food", new[] { "......KKKK..", ".....KNNNNK.", "....KNNTNNNK", "....KNNNNNNK", "....KNNNNNK.", "...KNNNNNK..", "..KWKKKKK...", ".KWWK.......", "KWWK........", "KWK.........", ".K..........", "............" } },
            { "heart", new[] { "............", ".KKK...KKK..", "KRRRK.KRRRK.", "KRWRRKRRRRK.", "KRRRRRRRRRK.", "KRRRRRRRRRK.", ".KRRRRRRRK..", "..KRRRRRK...", "...KRRRK....", "....KRK.....", ".....K......", "............" } },
            { "ammo", new[] { "............", "..Y...Y...Y.", ".YYY.YYY.YYY", ".YYY.YYY.YYY", ".OOO.OOO.OOO", ".OOO.OOO.OOO", ".OOO.OOO.OOO", ".OOO.OOO.OOO", ".OOO.OOO.OOO", ".KKK.KKK.KKK", "............", "............" } },
            { "skull", new[] { "...KKKKKK...", "..KWWWWWWK..", ".KWWWWWWWWK.", ".KWKKWWKKWK.", ".KWKKWWKKWK.", ".KWWWKKWWWK.", "..KWWWWWWK..", "...KWKWKWK..", "...KWKWKWK..", "....KKKKK...", "............", "............" } },
            { "clock", new[] { "...KKKKKK...", "..KWWWWWWK..", ".KWWWKWWWWK.", "KWWWWKWWWWWK", "KWWWWKWWWWWK", "KWWWWKKKKWWK", "KWWWWWWWWWWK", "KWWWWWWWWWWK", ".KWWWWWWWWK.", "..KWWWWWWK..", "...KKKKKK...", "............" } },
            { "flag", new[] { "..KK........", "..KKGGGGGGG.", "..KKGGGGGGGG", "..KKGGWGGGG.", "..KKGGGGGGGG", "..KKGGGGGGG.", "..KK........", "..KK........", "..KK........", "..KK........", ".KKKK.......", "............" } },
            { "pickaxe", new[] { "............", "..SSSSSSSS..", ".SSDDDDDDSS.", "SS...NN...SS", "S....NN....S", ".....NN.....", ".....NN.....", ".....NN.....", ".....NN.....", ".....NN.....", ".....NN.....", "............" } },
            { "barricade", new[] { "............", "............", "............", "...KKKKKK...", "..KTTTTTTK..", "..KTTTTTTK..", ".KKKKKKKKKK.", "KTTTTKTTTTTK", "KTTTTKTTTTTK", "KKKKKKKKKKKK", "............", "............" } },
            { "soldier", new[] { "............", "....GGGG....", "...GGGGGG...", "..GGGGGGGG..", "...PPPPPP...", "...PKPPKP...", "...PPPPPP...", "..GGGGGGGG..", ".GGGGGGGGGG.", ".GGGGGGGGGG.", ".GGGGGGGGGG.", "............" } },
            { "tower", new[] { "............", ".NNNNNNNNNN.", ".NTTTTTTTTN.", ".NNNNNNNNNN.", "..N......N..", "..NN....NN..", "..N.N..N.N..", "..N..NN..N..", "..N.N..N.N..", "..NN....NN..", "..N......N..", "............" } },
            { "miner", new[] { "............", "....YYYY....", "...YYYYYY...", "..YYYYYYYY..", ".YYYYYYYYYY.", "...PPPPPP...", "...PKPPKP...", "...PPPPPP...", "..OOOOOOOO..", ".OOOOOOOOOO.", ".OOOOOOOOOO.", "............" } },
            { "star", new[] { ".....YY.....", ".....YY.....", "....YYYY....", "YYYYYYYYYYYY", ".YYYYYYYYYY.", "..YYYYYYYY..", "...YYYYYY...", "...YYYYYY...", "..YYY..YYY..", "..YY....YY..", ".YY......YY.", "............" } },
            { "target", new[] { "............", "...WWWWWW...", "..W......W..", ".W..WWWW..W.", ".W.W....W.W.", ".W.W.RR.W.W.", ".W.W.RR.W.W.", ".W.W....W.W.", ".W..WWWW..W.", "..W......W..", "...WWWWWW...", "............" } },
            { "crate", new[] { "............", "..NNNNNNNN..", ".NTTTTTTTTN.", ".NTNNNNNNTN.", ".NTTTTTTTTN.", ".NTNNNNNNTN.", ".NTTTTTTTTN.", ".NTNNNNNNTN.", ".NTTTTTTTTN.", "..NNNNNNNN..", "............", "............" } },
            { "up", new[] { ".....GG.....", "....GGGG....", "...GGGGGG...", "..GGGGGGGG..", ".GGGGGGGGGG.", "....GGGG....", "....GGGG....", "....GGGG....", "....GGGG....", "....GGGG....", "............", "............" } },
            { "arrow", new[] { ".....WW.....", "....WWWW....", "...WWWWWW...", "..WWWWWWWW..", ".WWWWWWWWWW.", "WWWWWWWWWWWW", "....WWWW....", "....WWWW....", "....WWWW....", "....WWWW....", "....WWWW....", "............" } },
            { "alert", new[] { ".....RR.....", "....RRRR....", "....RWWR....", "...RRWWRR...", "...RRWWRR...", "..RRRWWRRR..", "..RRRWWRRR..", ".RRRRRRRRRR.", ".RRRRWWRRRR.", "RRRRRWWRRRRR", "RRRRRRRRRRRR", "............" } },
            { "truck", new[] { "............", "............", "TTTTTTT.....", "TTTTTTTSSS..", "TTTTTTTSLLS.", "TTTTTTTSSSSS", "TTTTTTTSSSSS", "KKKKKKKKKKKK", ".KDK....KDK.", "..K......K..", "............", "............" } },
        };

        static UIManager.Icons BuildIcons()
        {
            Directory.CreateDirectory("Assets/UI/Icons");
            var s = new Dictionary<string, Sprite>();
            foreach (var kv in IconArt) s[kv.Key] = MakeIcon(kv.Key, kv.Value);
            return new UIManager.Icons
            {
                coin = s["coin"], gold = s["gold"], food = s["food"], heart = s["heart"], ammo = s["ammo"], skull = s["skull"], clock = s["clock"],
                flag = s["flag"], pickaxe = s["pickaxe"], barricade = s["barricade"], soldier = s["soldier"], tower = s["tower"], miner = s["miner"],
                star = s["star"], target = s["target"], crate = s["crate"], up = s["up"], arrow = s["arrow"], alert = s["alert"], truck = s["truck"],
            };
        }

        static Sprite MakeIcon(string name, string[] rows)
        {
            const int N = 12, Scale = 4;
            var tex = new Texture2D(N * Scale, N * Scale, TextureFormat.RGBA32, false);
            var clear = new Color(0, 0, 0, 0);
            for (int y = 0; y < N; y++)
            {
                string row = y < rows.Length ? rows[y].PadRight(N, '.') : new string('.', N);
                for (int x = 0; x < N; x++)
                {
                    Color c = Palette.TryGetValue(row[x], out var pc) ? pc : clear;
                    for (int dy = 0; dy < Scale; dy++)
                        for (int dx = 0; dx < Scale; dx++)
                            tex.SetPixel(x * Scale + dx, (N - 1 - y) * Scale + dy, c);
                }
            }
            tex.Apply();
            return SaveSprite($"Assets/UI/Icons/{name}.png", tex, FilterMode.Point);
        }

        static Sprite SaveSprite(string path, Texture2D tex, FilterMode filter)
        {
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            var ti = (TextureImporter)AssetImporter.GetAtPath(path);
            ti.textureType = TextureImporterType.Sprite;
            ti.spriteImportMode = SpriteImportMode.Single;
            ti.filterMode = filter;
            ti.mipmapEnabled = false;
            ti.alphaIsTransparency = true;
            ti.textureCompression = TextureImporterCompression.Uncompressed;
            ti.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        // =====================================================================
        // Minimap picture (painted from the generated map data)
        // =====================================================================
        static Sprite BuildMinimapSprite()
        {
            const int S = 256;
            const float World = 140f;
            var tex = new Texture2D(S, S, TextureFormat.RGBA32, false);
            Color dirt = new Color(0.5f, 0.41f, 0.31f), sand = new Color(0.8f, 0.7f, 0.48f), forest = new Color(0.22f, 0.33f, 0.17f);
            for (int y = 0; y < S; y++)
                for (int x = 0; x < S; x++)
                {
                    float wx = x / (float)S * World - World / 2f;
                    tex.SetPixel(x, y, wx < -14f ? sand : wx > 14f ? forest : dirt);
                }
            int P(float w) => Mathf.RoundToInt((w + World / 2f) / World * S);
            void Fill(float wx, float wz, int r, Color c)
            {
                int cx = P(wx), cy = P(wz);
                for (int y = cy - r; y <= cy + r; y++)
                    for (int x = cx - r; x <= cx + r; x++)
                        if (x >= 0 && y >= 0 && x < S && y < S) tex.SetPixel(x, y, c);
            }
            foreach (var r in RockSpots) Fill(r.x, r.z, 1, new Color(0.45f, 0.38f, 0.3f));
            foreach (var t in TreeSpots) Fill(t.x, t.z, 1, new Color(0.1f, 0.2f, 0.08f));
            for (int i = 0; i < RoadPts.Length - 1; i++)
                for (float k = 0; k <= 1f; k += 0.005f)
                {
                    var p = Vector3.Lerp(RoadPts[i], RoadPts[i + 1], k);
                    Fill(p.x, p.z, 3, new Color(0.3f, 0.27f, 0.25f));
                }
            for (float x = -6f; x <= 6f; x += 0.5f)
                for (float z = 4f; z <= 12f; z += 0.5f) Fill(x, z, 0, new Color(0.36f, 0.32f, 0.29f));
            foreach (var o in new[] { new Vector2(-17, 9), new Vector2(16, 11), new Vector2(-2, 23) }) Fill(o.x, o.y, 2, new Color(1f, 0.8f, 0.2f));
            for (int i = 0; i < S; i++)
            {
                tex.SetPixel(i, 0, Color.black); tex.SetPixel(i, S - 1, Color.black);
                tex.SetPixel(0, i, Color.black); tex.SetPixel(S - 1, i, Color.black);
            }
            tex.Apply();
            Directory.CreateDirectory("Assets/UI");
            return SaveSprite("Assets/UI/Minimap.png", tex, FilterMode.Bilinear);
        }
    }
}
