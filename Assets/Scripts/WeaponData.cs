using UnityEngine;

namespace WaywardSon
{
    [CreateAssetMenu(fileName = "NewWeaponData", menuName = "Wayward Son/Weapon Data")]
    public class WeaponData : ScriptableObject
    {
        [Header("General Info")]
        public string weaponName = "Pistol";
        
        [Header("Firing Mode")]
        public bool isHitscan = true;
        public GameObject projectilePrefab;

        [Header("Stats")]
        public int damage = 25;
        public float fireRate = 0.5f; // Time between shots in seconds
        public float range = 50.0f;
        public int ammoCapacity = 7;
        
        [Header("Handling")]
        public float spread = 0.05f;
        [Range(0f, 1f)]
        public float aimSpeedMultiplier = 0.3f; // Move at 30% speed when aiming

        [Header("Swarm Firing")]
        [Tooltip("Number of pellets/projectiles fired simultaneously")]
        public int pelletsPerShot = 1;
    }
}
