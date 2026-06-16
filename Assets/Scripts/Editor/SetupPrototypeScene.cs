using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.AI;
using WaywardSon.SaveSystem;

namespace WaywardSon.Editor
{
    public class SetupPrototypeScene
    {
        [MenuItem("Wayward Son/Setup Prototype Scene")]
        public static void Setup()
        {
            // Create folders if they do not exist
            if (!AssetDatabase.IsValidFolder("Assets/Weapons"))
            {
                AssetDatabase.CreateFolder("Assets", "Weapons");
            }
            if (!AssetDatabase.IsValidFolder("Assets/Prefabs"))
            {
                AssetDatabase.CreateFolder("Assets", "Prefabs");
            }

            // Create a new empty scene
            var activeScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            
            // 1. Create Ground
            GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ground.name = "Ground";
            ground.transform.position = new Vector3(0, -0.5f, 0);
            ground.transform.localScale = new Vector3(50, 1, 50);
            
            var renderer = ground.GetComponent<Renderer>();
            if (renderer != null)
            {
                Material darkMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                if (darkMaterial == null) darkMaterial = new Material(Shader.Find("Standard"));
                darkMaterial.color = new Color(0.15f, 0.15f, 0.15f);
                renderer.material = darkMaterial;
            }

            // 2. Create Projectile Prefab for the Magic Spell
            string projectilePrefabPath = "Assets/Prefabs/MagicProjectile.prefab";
            GameObject projObj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            projObj.name = "MagicProjectile";
            projObj.transform.localScale = new Vector3(0.25f, 0.25f, 0.25f);
            
            var sphereCollider = projObj.GetComponent<SphereCollider>();
            if (sphereCollider != null) Object.DestroyImmediate(sphereCollider);
            var triggerCollider = projObj.AddComponent<SphereCollider>();
            triggerCollider.isTrigger = true;
            
            var rb = projObj.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            
            var projectileScript = projObj.AddComponent<Projectile>();
            projectileScript.speed = 15f;
            projectileScript.damage = 35;
            
            var projRenderer = projObj.GetComponent<Renderer>();
            if (projRenderer != null)
            {
                Material purpleMat = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
                if (purpleMat == null) purpleMat = new Material(Shader.Find("Sprites/Default"));
                purpleMat.color = new Color(0.6f, 0.2f, 0.8f);
                projRenderer.material = purpleMat;
            }

            GameObject prefabAsset = PrefabUtility.SaveAsPrefabAsset(projObj, projectilePrefabPath);
            Object.DestroyImmediate(projObj);

            // 3. Create Weapon Assets
            string pistolPath = "Assets/Weapons/PistolData.asset";
            WeaponData pistolData = AssetDatabase.LoadAssetAtPath<WeaponData>(pistolPath);
            if (pistolData == null)
            {
                pistolData = ScriptableObject.CreateInstance<WeaponData>();
                pistolData.weaponName = "9mm Pistol (Hitscan)";
                pistolData.isHitscan = true;
                pistolData.damage = 20;
                pistolData.fireRate = 0.35f;
                pistolData.range = 50f;
                pistolData.ammoCapacity = 8;
                pistolData.spread = 0.015f;
                pistolData.aimSpeedMultiplier = 0.5f;
                AssetDatabase.CreateAsset(pistolData, pistolPath);
            }

            string spellPath = "Assets/Weapons/MagicSpellData.asset";
            WeaponData spellData = AssetDatabase.LoadAssetAtPath<WeaponData>(spellPath);
            if (spellData == null)
            {
                spellData = ScriptableObject.CreateInstance<WeaponData>();
                spellData.weaponName = "Ember Spell (Projectile)";
                spellData.isHitscan = false;
                spellData.projectilePrefab = prefabAsset;
                spellData.damage = 35;
                spellData.fireRate = 0.75f;
                spellData.range = 30f;
                spellData.ammoCapacity = 5;
                spellData.spread = 0.04f;
                spellData.aimSpeedMultiplier = 0.3f;
                AssetDatabase.CreateAsset(spellData, spellPath);
            }
            
            AssetDatabase.SaveAssets();

            // 4. Create Player (Capsule)
            GameObject player = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            player.name = "Player";
            player.tag = "Player";
            player.transform.position = new Vector3(-5f, 1f, -5f); // Spawn on bottom-left quadrant
            
            var playerRenderer = player.GetComponent<Renderer>();
            if (playerRenderer != null)
            {
                Material playerMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                if (playerMaterial == null) playerMaterial = new Material(Shader.Find("Standard"));
                playerMaterial.color = new Color(0.2f, 0.5f, 0.8f);
                playerRenderer.material = playerMaterial;
            }
            
            var capsuleCollider = player.GetComponent<CapsuleCollider>();
            if (capsuleCollider != null)
            {
                Object.DestroyImmediate(capsuleCollider);
            }
            
            var charController = player.AddComponent<CharacterController>();
            charController.center = new Vector3(0, 0, 0);
            charController.height = 2f;
            charController.radius = 0.5f;

            // Add Player Health and Inventory
            player.AddComponent<PlayerHealth>();
            player.AddComponent<Inventory>();

            // Add FlashlightController
            player.AddComponent<FlashlightController>();

            // Add PlayerController
            var playerController = player.AddComponent<PlayerController>();
            playerController.autoAimRange = 15.0f;

            // Add WeaponHandler and link pistolData
            var weaponHandler = player.AddComponent<WeaponHandler>();
            weaponHandler.activeWeapon = pistolData;

            // Gun nozzle nozzle
            GameObject nozzle = new GameObject("GunNozzle");
            nozzle.transform.SetParent(player.transform);
            nozzle.transform.localPosition = new Vector3(0.3f, 0.5f, 0.8f);
            weaponHandler.firePoint = nozzle.transform;

            // Add Save Commands (auto/manual save)
            player.AddComponent<SaveCommands>();

            // Add HUD Manager (unified HUD)
            var hudManager = player.AddComponent<HUDManager>();
            hudManager.playerController = playerController;
            hudManager.playerHealth = player.GetComponent<PlayerHealth>();
            hudManager.weaponHandler = weaponHandler;
            hudManager.flashlight = player.GetComponent<FlashlightController>();

            // 5. Setup Camera
            GameObject cameraObj = GameObject.FindWithTag("MainCamera");
            if (cameraObj == null)
            {
                cameraObj = new GameObject("Main Camera");
                cameraObj.tag = "MainCamera";
                cameraObj.AddComponent<Camera>();
            }
            
            if (cameraObj.GetComponent<AudioListener>() == null)
            {
                cameraObj.AddComponent<AudioListener>();
            }

            var isoCam = cameraObj.GetComponent<IsometricCamera>();
            if (isoCam == null)
            {
                isoCam = cameraObj.AddComponent<IsometricCamera>();
            }
            isoCam.target = player.transform;
            isoCam.offset = new Vector3(-8f, 10f, -8f);

            // 6. Create Directional Light (dim for horror atmosphere)
            GameObject lightObj = new GameObject("Directional Light");
            var lightComponent = lightObj.AddComponent<Light>();
            lightComponent.type = LightType.Directional;
            lightComponent.color = new Color(0.4f, 0.4f, 0.5f);  // Cold, dim blue
            lightComponent.intensity = 0.15f;                     // Very low for darkness
            lightObj.transform.position = new Vector3(0, 10, 0);
            lightObj.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

            // 6b. Setup Fog for horror atmosphere
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = new Color(0.02f, 0.02f, 0.04f); // Near-black fog
            RenderSettings.fogDensity = 0.08f; // Dense enough to limit vision

            // 6c. Set ambient light to near-zero for darkness
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.03f, 0.03f, 0.05f); // Almost black

            // 7. Create Arena Walls (for Auto-Aim Line of Sight and Raycast Collision testing)
            // Wall A: Center dividing wall with a gap in the center
            GameObject wallA1 = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wallA1.name = "Wall_Center_Left";
            wallA1.transform.position = new Vector3(-5f, 2f, 0f);
            wallA1.transform.localScale = new Vector3(15f, 4f, 2f);
            SetWallMaterial(wallA1);

            GameObject wallA2 = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wallA2.name = "Wall_Center_Right";
            wallA2.transform.position = new Vector3(8f, 2f, 0f);
            wallA2.transform.localScale = new Vector3(12f, 4f, 2f);
            SetWallMaterial(wallA2);

            // Wall B: Pillars/Columns in quadrants
            GameObject pillar1 = GameObject.CreatePrimitive(PrimitiveType.Cube);
            pillar1.name = "Pillar_1";
            pillar1.transform.position = new Vector3(5f, 2f, -8f);
            pillar1.transform.localScale = new Vector3(3f, 4f, 3f);
            SetWallMaterial(pillar1);

            GameObject pillar2 = GameObject.CreatePrimitive(PrimitiveType.Cube);
            pillar2.name = "Pillar_2";
            pillar2.transform.position = new Vector3(-8f, 2f, 5f);
            pillar2.transform.localScale = new Vector3(3f, 4f, 3f);
            SetWallMaterial(pillar2);

            // 8. Create Enemies
            // Enemy 1: Stationary Target (on the other side of Wall A2)
            GameObject dummy = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            dummy.name = "Dummy_Stationary";
            dummy.transform.position = new Vector3(8f, 1f, 5f);
            
            var dummyRenderer = dummy.GetComponent<Renderer>();
            if (dummyRenderer != null)
            {
                Material dummyMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                if (dummyMaterial == null) dummyMaterial = new Material(Shader.Find("Standard"));
                dummyMaterial.color = new Color(0.7f, 0.15f, 0.15f); // Red
                dummyRenderer.material = dummyMaterial;
            }
            var dummyEH = dummy.AddComponent<EnemyHealth>();
            dummyEH.maxHealth = 100;
            dummyEH.isAggressive = false;

            // Enemy 2: Aggressive Zombie (on the top-left quadrant)
            GameObject zombie = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            zombie.name = "Zombie_Aggressive";
            zombie.transform.position = new Vector3(-8f, 1f, 8f);
            
            var zombieRenderer = zombie.GetComponent<Renderer>();
            if (zombieRenderer != null)
            {
                Material zombieMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                if (zombieMaterial == null) zombieMaterial = new Material(Shader.Find("Standard"));
                zombieMaterial.color = new Color(0.15f, 0.6f, 0.15f); // Green
                zombieRenderer.material = zombieMaterial;
            }
            var zombieEH = zombie.AddComponent<EnemyHealth>();
            zombieEH.maxHealth = 60;
            zombieEH.isAggressive = true;
            zombieEH.attackDamage = 15;
            zombieEH.attackCooldown = 1.0f;
            zombieEH.attackRange = 1.6f;
            
            // Add NavMeshAgent for movement
            zombie.AddComponent<NavMeshAgent>();

            // 9. Create SaveManager (persistent across scenes)
            GameObject saveMgr = new GameObject("SaveManager");
            saveMgr.AddComponent<SaveManager>();
            Object.DontDestroyOnLoad(saveMgr);

            // 10. Spawn Collectible WorldItems around the arena
            SpawnWorldItems();

            // Save the scene
            string scenePath = "Assets/Scenes/SampleScene.unity";
            EditorSceneManager.SaveScene(activeScene, scenePath);
            Debug.Log("Prototype Scene set up successfully and saved to " + scenePath);
        }

        // ─── World Item Spawner ────────────────────────────────────────────────────

        private static void SpawnWorldItems()
        {
            // Ensure Items folder exists
            if (!AssetDatabase.IsValidFolder("Assets/Items"))
                AssetDatabase.CreateFolder("Assets", "Items");

            // ── Med Kit ──
            var medDef = LoadOrCreateItem("Assets/Items/MedKit.asset", def =>
            {
                def.itemName    = "Med Kit";
                def.description = "Kit de primeiros socorros.\nRestora 40 HP.";
                def.gridSize    = new Vector2Int(2, 2);
                def.texturePath = "Images/Medkit";
                def.tintColor   = new Color(0.08f, 0.20f, 0.08f);
                def.itemType    = ItemDefinition.ItemType.Consumable;
                def.healAmount  = 40;
            });
            CreateWorldItemObject("Pickup_MedKit",    new Vector3(-3f, 0.3f,  5f), medDef, Color.green);

            // ── Handgun Ammo ──
            var ammoDef = LoadOrCreateItem("Assets/Items/HandgunAmmo.asset", def =>
            {
                def.itemName    = "Handgun Ammo";
                def.description = "Caixa com 15 balas 9mm.";
                def.gridSize    = new Vector2Int(1, 1);
                def.texturePath = "Images/handgunammo";
                def.tintColor   = new Color(0.20f, 0.14f, 0.08f);
                def.itemType    = ItemDefinition.ItemType.Ammo;
                def.ammoAmount  = 15;
            });
            CreateWorldItemObject("Pickup_HandgunAmmo", new Vector3(3f, 0.3f, -6f), ammoDef, Color.yellow);

            // ── Shotgun Shell ──
            var shellDef = LoadOrCreateItem("Assets/Items/ShotgunShell.asset", def =>
            {
                def.itemName    = "Shotgun Shell";
                def.description = "Cartucho calibre 12. Fornece 8 cartuchos.";
                def.gridSize    = new Vector2Int(1, 1);
                def.texturePath = "Images/Shotgunshell";
                def.tintColor   = new Color(0.22f, 0.12f, 0.05f);
                def.itemType    = ItemDefinition.ItemType.Ammo;
                def.ammoAmount  = 8;
            });
            CreateWorldItemObject("Pickup_ShotgunShell", new Vector3(10f, 0.3f, 7f), shellDef, new Color(1f, 0.5f, 0f));

            // ── Battery (Flashlight recharge) ──
            var batteryDef = LoadOrCreateItem("Assets/Items/Battery.asset", def =>
            {
                def.itemName    = "Battery";
                def.description = "Pilha alcalina.\nRecarrega a lanterna em 30 pontos.";
                def.gridSize    = new Vector2Int(1, 1);
                def.texturePath = "Images/battery";
                def.tintColor   = new Color(0.1f, 0.8f, 0.9f);  // Cyan tint
                def.itemType    = ItemDefinition.ItemType.Battery;
                def.batteryAmount = 30f;
            });
            CreateWorldItemObject("Pickup_Battery_1", new Vector3(-6f, 0.3f, -3f), batteryDef, new Color(0.1f, 0.8f, 0.9f));
            CreateWorldItemObject("Pickup_Battery_2", new Vector3(7f, 0.3f, 3f), batteryDef, new Color(0.1f, 0.8f, 0.9f));

            // ── Extra Battery near enemy area ──
            CreateWorldItemObject("Pickup_Battery_3", new Vector3(-8f, 0.3f, 10f), batteryDef, new Color(0.1f, 0.8f, 0.9f));

            AssetDatabase.SaveAssets();
        }

        private static ItemDefinition LoadOrCreateItem(string path, System.Action<ItemDefinition> configure)
        {
            var existing = AssetDatabase.LoadAssetAtPath<ItemDefinition>(path);
            if (existing != null) return existing;

            var def = ScriptableObject.CreateInstance<ItemDefinition>();
            configure(def);
            AssetDatabase.CreateAsset(def, path);
            return def;
        }

        private static void CreateWorldItemObject(string objName, Vector3 pos, ItemDefinition def, Color glowColor)
        {
            // Visual: a small glowing cube
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = objName;
            go.transform.position = pos;
            go.transform.localScale = new Vector3(0.4f, 0.4f, 0.4f);

            // Emissive material
            var rend = go.GetComponent<Renderer>();
            if (rend != null)
            {
                Material m = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                if (m == null) m = new Material(Shader.Find("Standard"));
                m.color = glowColor * 0.7f;
                m.EnableKeyword("_EMISSION");
                m.SetColor("_EmissionColor", glowColor * 1.4f);
                rend.material = m;
            }

            // Trigger collider (enlarged for easy pickup)
            var col = go.GetComponent<BoxCollider>();
            if (col != null) Object.DestroyImmediate(col);
            var trigger = go.AddComponent<BoxCollider>();
            trigger.size      = new Vector3(3f, 3f, 3f);
            trigger.isTrigger = true;

            // WorldItem component
            var wi = go.AddComponent<WorldItem>();
            wi.definition = def;
        }

        private static void SetWallMaterial(GameObject wall)
        {
            var wallRend = wall.GetComponent<Renderer>();
            if (wallRend != null)
            {
                Material wallMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                if (wallMat == null) wallMat = new Material(Shader.Find("Standard"));
                wallMat.color = new Color(0.45f, 0.45f, 0.45f); // Gray
                wallRend.material = wallMat;
            }
        }
    }
}
