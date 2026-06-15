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
        private float nextFireTime = 0f;
        private int currentAmmo = 0;

        private void Start()
        {
            playerController = GetComponent<PlayerController>();
            
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
                    dummy.TakeDamage(activeWeapon.damage);
                }
                else
                {
                    Debug.Log($"Hitscan hit: {hit.collider.name}");
                }
            }

            CreateTracer(firePoint.position, endPoint);
        }

        private void FireProjectile()
        {
            if (activeWeapon.projectilePrefab == null)
            {
                Debug.LogError($"{activeWeapon.weaponName} has no ProjectilePrefab!");
                return;
            }

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
            proj.damage = activeWeapon.damage;
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

        private void OnGUI()
        {
            if (activeWeapon == null) return;

            GUIStyle style = new GUIStyle();
            style.fontSize = 20;
            style.normal.textColor = Color.white;

            string aimState = playerController != null && playerController.isAiming ? " [AIMING]" : "";
            GUI.Label(new Rect(20, 20, 300, 30), $"Weapon: {activeWeapon.weaponName}{aimState}", style);
            GUI.Label(new Rect(20, 50, 300, 30), $"Ammo: {currentAmmo} / {activeWeapon.ammoCapacity}", style);
            GUI.Label(new Rect(20, 80, 600, 30), "Controls: WASD / L-Stick (Move) | Hold RMB / LT (Aim) | LMB / RT (Fire)", style);
        }
    }
}
