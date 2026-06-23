using UnityEngine;
using UnityEngine.InputSystem;

namespace WaywardSon
{
    public class WeaponHandler : MonoBehaviour
    {
        [Header("Weapon Config")]
        public WeaponData activeWeapon;
        public Transform firePoint;

        [Header("Tracer Settings (Hitscan)")]
        public Material tracerMaterial;

        private PlayerController playerController;
        private FlashlightController flashlight;
        private float nextFireTime = 0f;
        public int currentAmmo { get; private set; } = 0;

        [Header("Passive Vision")]
        [Tooltip("Multiplicador de dano quando sem lanterna")]
        public float passiveDamageMultiplier = 0.7f;

        private void Start()
        {
            playerController = GetComponent<PlayerController>();
            flashlight = GetComponent<FlashlightController>();
            
            if (activeWeapon != null)
            {
                currentAmmo = activeWeapon.ammoCapacity;
            }

            if (firePoint == null)
            {
                // Create a temporary firepoint if none is assigned
                GameObject fpObj = new GameObject("FirePoint");
                fpObj.transform.SetParent(transform);
                fpObj.transform.localPosition = new Vector3(0f, 0.5f, 0.8f); // slightly forward and up
                firePoint = fpObj.transform;
            }
        }

        private void Update()
        {
            if (activeWeapon == null || playerController == null) return;

            // Firing is only allowed when aiming (Resident Evil style)
            if (playerController.isAiming)
            {
                bool shootInput = false;

                // Keyboard/Mouse
                if (Mouse.current != null)
                {
                    shootInput = Mouse.current.leftButton.wasPressedThisFrame;
                }

                // Gamepad
                if (!shootInput && Gamepad.current != null)
                {
                    shootInput = Gamepad.current.rightTrigger.wasPressedThisFrame || Gamepad.current.buttonWest.wasPressedThisFrame;
                }

                if (shootInput && Time.time >= nextFireTime)
                {
                    nextFireTime = Time.time + activeWeapon.fireRate;
                    FireWeapon();
                }
            }
        }

        private void FireWeapon()
        {
            if (currentAmmo <= 0)
            {
                Debug.Log($"{activeWeapon.weaponName}: Out of ammo! Reloading...");
                currentAmmo = activeWeapon.ammoCapacity; // Auto reload for prototype ease
                return;
            }

            currentAmmo--;
            Debug.Log($"{activeWeapon.weaponName} Fired! Ammo: {currentAmmo}/{activeWeapon.ammoCapacity}");

            if (activeWeapon.isHitscan)
            {
                FireHitscan();
            }
            else
            {
                FireProjectile();
            }
        }

        private void FireHitscan()
        {
            // Calculate effective damage (reduced when flashlight is off)
            int effectiveDamage = activeWeapon.damage;
            if (flashlight != null && !flashlight.IsFlashlightOn)
            {
                effectiveDamage = Mathf.RoundToInt(activeWeapon.damage * passiveDamageMultiplier);
            }

            int count = Mathf.Max(1, activeWeapon.pelletsPerShot);
            for (int i = 0; i < count; i++)
            {
                Vector3 direction = firePoint.forward;
                
                // Apply spread
                direction.x += Random.Range(-activeWeapon.spread, activeWeapon.spread);
                direction.y += Random.Range(-activeWeapon.spread, activeWeapon.spread);
                direction.Normalize();

                RaycastHit hit;
                Vector3 endPoint = firePoint.position + direction * activeWeapon.range;

                if (Physics.Raycast(firePoint.position, direction, out hit, activeWeapon.range))
                {
                    endPoint = hit.point;

                    // Check for EnemyHealth
                    EnemyHealth dummy = hit.collider.GetComponent<EnemyHealth>();
                    if (dummy != null)
                    {
                        dummy.TakeDamage(effectiveDamage);
                    }
                    else
                    {
                        Debug.Log($"Hitscan hit: {hit.collider.name}");
                    }
                }

                CreateTracer(firePoint.position, endPoint);
            }
        }

        private void FireProjectile()
        {
            if (activeWeapon.projectilePrefab == null)
            {
                Debug.LogError($"{activeWeapon.weaponName} has no ProjectilePrefab!");
                return;
            }

            // Calculate effective damage (reduced when flashlight is off)
            int effectiveDamage = activeWeapon.damage;
            if (flashlight != null && !flashlight.IsFlashlightOn)
            {
                effectiveDamage = Mathf.RoundToInt(activeWeapon.damage * passiveDamageMultiplier);
            }

            int count = Mathf.Max(1, activeWeapon.pelletsPerShot);
            for (int i = 0; i < count; i++)
            {
                Vector3 direction = firePoint.forward;
                direction.x += Random.Range(-activeWeapon.spread, activeWeapon.spread);
                direction.y += Random.Range(-activeWeapon.spread, activeWeapon.spread);
                direction.Normalize();

                GameObject projObj = Instantiate(activeWeapon.projectilePrefab, firePoint.position, Quaternion.LookRotation(direction));
                
                Projectile proj = projObj.GetComponent<Projectile>();
                if (proj == null)
                {
                    proj = projObj.AddComponent<Projectile>();
                }
                proj.damage = effectiveDamage;
            }
        }

        private void CreateTracer(Vector3 start, Vector3 end)
        {
            GameObject tracerObj = new GameObject("Tracer");
            LineRenderer lr = tracerObj.AddComponent<LineRenderer>();
            
            lr.startWidth = 0.05f;
            lr.endWidth = 0.02f;
            lr.positionCount = 2;
            lr.SetPosition(0, start);
            lr.SetPosition(1, end);
            
            // Set simple yellow material
            if (tracerMaterial != null)
            {
                lr.material = tracerMaterial;
            }
            else
            {
                lr.material = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
                if (lr.material == null) lr.material = new Material(Shader.Find("Sprites/Default"));
                lr.material.color = Color.yellow;
            }

            Destroy(tracerObj, 0.05f); // vanish tracer after 0.05s
        }

        // ─── Public API for Inventory ───────────────────────────────────────────

        /// <summary>Equip a new weapon from the inventory.</summary>
        public void EquipWeapon(WeaponData data)
        {
            if (data == null) return;
            activeWeapon = data;
            currentAmmo  = data.ammoCapacity;
            Debug.Log($"[WeaponHandler] Equipado: {data.weaponName} ({currentAmmo} balas)");
        }

        /// <summary>Add ammo to the currently equipped weapon.</summary>
        public void AddAmmo(int amount)
        {
            if (activeWeapon == null)
            {
                Debug.LogWarning("[WeaponHandler] Nenhuma arma equipada para adicionar munição.");
                return;
            }
            currentAmmo = Mathf.Min(currentAmmo + amount, activeWeapon.ammoCapacity * 3); // max 3x cap
            Debug.Log($"[WeaponHandler] +{amount} munição → {currentAmmo}");
        }
    }
}
