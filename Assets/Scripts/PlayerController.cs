using UnityEngine;
using UnityEngine.InputSystem;

namespace WaywardSon
{
    [RequireComponent(typeof(CharacterController))]
    public class PlayerController : MonoBehaviour
    {
        [Header("Movement")]
        public float speed = 5.0f;
        public float gravity = -9.81f;
        
        [Header("Aim Settings")]
        public bool isAiming = false;
        public float autoAimRange = 12.0f;

        private CharacterController controller;
        private WeaponHandler weaponHandler;
        private PlayerHealth playerHealth;
        private Camera mainCamera;
        private Vector2 moveInput;
        private Vector3 velocity;

        private void Start()
        {
            controller = GetComponent<CharacterController>();
            weaponHandler = GetComponent<WeaponHandler>();
            playerHealth = GetComponent<PlayerHealth>();
            mainCamera = Camera.main;
        }

        private void Update()
        {
            ReadInput();
            HandleRotationAndAiming();
            HandleMovement();
        }

        private void ReadInput()
        {
            float moveX = 0f;
            float moveZ = 0f;

            // 1. Read Keyboard input
            if (Keyboard.current != null)
            {
                if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed) moveZ = 1f;
                if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed) moveZ = -1f;
                if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed) moveX = -1f;
                if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed) moveX = 1f;
            }

            Vector2 keyboardInput = new Vector2(moveX, moveZ);

            // 2. Read Gamepad input (override if keyboard is inactive)
            if (keyboardInput.sqrMagnitude > 0.001f)
            {
                moveInput = keyboardInput.normalized;
            }
            else if (Gamepad.current != null)
            {
                Vector2 stickInput = Gamepad.current.leftStick.ReadValue();
                if (stickInput.sqrMagnitude > 0.04f) // deadzone
                {
                    moveInput = stickInput;
                }
                else
                {
                    moveInput = Vector2.zero;
                }
            }
            else
            {
                moveInput = Vector2.zero;
            }

            // 3. Check Aim Stance (RMB on Mouse or LT/L-Shoulder on Gamepad)
            bool mouseAim = Mouse.current != null && Mouse.current.rightButton.isPressed;
            bool gamepadAim = Gamepad.current != null && (Gamepad.current.leftTrigger.isPressed || Gamepad.current.leftShoulder.isPressed);
            isAiming = mouseAim || gamepadAim;
        }

        private void HandleRotationAndAiming()
        {
            bool usingAutoAim = false;

            // 1. Auto-Aim Check (closest visible enemy in range)
            if (isAiming)
            {
                Transform target = GetAutoAimTarget();
                if (target != null)
                {
                    usingAutoAim = true;
                    Vector3 lookDir = target.position - transform.position;
                    lookDir.y = 0f; // Stay horizontal

                    if (lookDir.sqrMagnitude > 0.001f)
                    {
                        Quaternion targetRotation = Quaternion.LookRotation(lookDir);
                        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 15f);
                    }
                }
            }

            // 2. Rotate using Gamepad Right Stick (if not auto-aiming)
            bool usingGamepadAim = false;
            if (isAiming && !usingAutoAim && Gamepad.current != null)
            {
                Vector2 rightStick = Gamepad.current.rightStick.ReadValue();
                if (rightStick.sqrMagnitude > 0.04f)
                {
                    usingGamepadAim = true;
                    Vector3 lookDir = GetCameraRelativeDirection(rightStick);
                    if (lookDir.sqrMagnitude > 0.001f)
                    {
                        Quaternion targetRotation = Quaternion.LookRotation(lookDir);
                        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 15f);
                    }
                }
            }

            // 3. Rotate using Mouse Cursor position (if not auto-aiming or gamepad aiming)
            if (isAiming && !usingAutoAim && !usingGamepadAim && mainCamera != null && Mouse.current != null)
            {
                Ray ray = mainCamera.ScreenPointToRay(Mouse.current.position.ReadValue());
                Plane groundPlane = new Plane(Vector3.up, transform.position);
                
                if (groundPlane.Raycast(ray, out float rayDistance))
                {
                    Vector3 targetPoint = ray.GetPoint(rayDistance);
                    Vector3 lookDirection = targetPoint - transform.position;
                    lookDirection.y = 0f;

                    if (lookDirection.sqrMagnitude > 0.001f)
                    {
                        Quaternion targetRotation = Quaternion.LookRotation(lookDirection);
                        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 15f);
                    }
                }
            }
            // 4. Normal Movement Stance (Face moving direction)
            else if (!isAiming)
            {
                if (moveInput.sqrMagnitude > 0.001f)
                {
                    Vector3 moveDir = GetCameraRelativeDirection(moveInput);
                    if (moveDir.sqrMagnitude > 0.001f)
                    {
                        Quaternion targetRotation = Quaternion.LookRotation(moveDir);
                        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 10f);
                    }
                }
            }
        }

        private void HandleMovement()
        {
            if (controller.isGrounded && velocity.y < 0)
            {
                velocity.y = -2f;
            }

            Vector3 moveDirection = Vector3.zero;

            if (moveInput.sqrMagnitude > 0.001f)
            {
                float currentSpeed = speed;

                // Apply Slow aiming movement
                if (isAiming)
                {
                    float mult = (weaponHandler != null && weaponHandler.activeWeapon != null) 
                        ? weaponHandler.activeWeapon.aimSpeedMultiplier 
                        : 0.3f;
                    currentSpeed *= mult;
                }

                // Apply Player Health ECG Speed Penalty (Fine: 1.0x, Caution: 0.65x, Danger: 0.35x)
                if (playerHealth != null)
                {
                    currentSpeed *= playerHealth.SpeedMultiplier;
                }

                moveDirection = GetCameraRelativeDirection(moveInput) * currentSpeed;
            }

            controller.Move(moveDirection * Time.deltaTime);

            // Apply gravity
            velocity.y += gravity * Time.deltaTime;
            controller.Move(velocity * Time.deltaTime);
        }

        private Transform GetAutoAimTarget()
        {
            EnemyHealth[] enemies = FindObjectsOfType<EnemyHealth>();
            Transform bestTarget = null;
            float closestDistance = autoAimRange;

            foreach (EnemyHealth enemy in enemies)
            {
                float distance = Vector3.Distance(transform.position, enemy.transform.position);
                if (distance < closestDistance)
                {
                    // Check Line of Sight (LoS)
                    Vector3 directionToEnemy = (enemy.transform.position - transform.position).normalized;
                    Vector3 rayStart = transform.position + Vector3.up * 0.5f; // eye level
                    Vector3 targetPos = enemy.transform.position + Vector3.up * 0.5f;
                    float rayDist = Vector3.Distance(rayStart, targetPos);

                    RaycastHit hit;
                    if (Physics.Raycast(rayStart, directionToEnemy, out hit, rayDist))
                    {
                        // Check if we hit the enemy directly
                        if (hit.transform == enemy.transform || hit.transform.GetComponentInParent<EnemyHealth>() != null)
                        {
                            bestTarget = enemy.transform;
                            closestDistance = distance;
                        }
                    }
                }
            }

            return bestTarget;
        }

        private Vector3 GetCameraRelativeDirection(Vector2 input)
        {
            if (mainCamera == null)
            {
                return new Vector3(input.x, 0f, input.y).normalized;
            }

            Vector3 camForward = mainCamera.transform.forward;
            Vector3 camRight = mainCamera.transform.right;

            camForward.y = 0f;
            camRight.y = 0f;

            camForward.Normalize();
            camRight.Normalize();

            return (camForward * input.y + camRight * input.x).normalized;
        }

        private void OnGUI()
        {
            // Draw Player Health ECG Status (OnGUI legacy HUD helper)
            if (playerHealth == null) return;

            GUIStyle style = new GUIStyle();
            style.fontSize = 20;
            style.fontStyle = FontStyle.Bold;

            Color hpColor = Color.green;
            switch (playerHealth.CurrentState)
            {
                case PlayerHealth.HealthState.Fine:
                    hpColor = Color.green;
                    break;
                case PlayerHealth.HealthState.Caution:
                    hpColor = Color.yellow;
                    break;
                case PlayerHealth.HealthState.Danger:
                    hpColor = Color.red;
                    break;
            }
            style.normal.textColor = hpColor;

            GUI.Label(new Rect(20, Screen.height - 50, 400, 30), $"ECG State: {playerHealth.CurrentState.ToString().ToUpper()} ({playerHealth.currentHealth} / {playerHealth.maxHealth} HP)", style);
        }
    }
}
