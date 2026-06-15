using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.AI;

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

            // 6. Create Directional Light
            GameObject lightObj = new GameObject("Directional Light");
            var lightComponent = lightObj.AddComponent<Light>();
            lightComponent.type = LightType.Directional;
            lightComponent.color = new Color(0.9f, 0.9f, 0.9f);
            lightComponent.intensity = 0.7f;
            lightObj.transform.position = new Vector3(0, 10, 0);
            lightObj.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

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

            // Save the scene
            string scenePath = "Assets/Scenes/SampleScene.unity";
            EditorSceneManager.SaveScene(activeScene, scenePath);
            Debug.Log("Prototype Scene set up successfully and saved to " + scenePath);
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
