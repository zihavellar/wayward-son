using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using WaywardSon.SaveSystem;

namespace WaywardSon
{
    /// <summary>
    /// Controla a lanterna do jogador: Spot Light, toggle, bateria limitável,
    /// drenagem com uso e recarga via item Consumable (Battery).
    /// Também controla a "visão passiva" quando a lanterna está desligada.
    /// </summary>
    public class FlashlightController : MonoBehaviour, ISaveable
    {
        [Header("Flashlight Settings")]
        public Light spotLight;
        public float maxBattery = 100f;
        public float drainRate = 0.8f;        // bateria por segundo (~125s autonomia com 100 de bateria)
        public float resumeDrainDelay = 0.5f; // delay antes de voltar a drenar após recarga

        [Header("Passive Vision (No Flashlight)")]
        [Tooltip("Raixo de visão passiva (sem lanterna) — menor que a lanterna")]
        public float passiveVisionRange = 3f;
        [Tooltip("Redução de velocidade quando sem lanterna (ex: 0.8 = 80% da velocidade normal)")]
        public float passiveSpeedMultiplier = 0.8f;
        [Tooltip("Redução de dano quando sem lanterna")]
        public float passiveDamageMultiplier = 0.7f;

        [Header("References")]
        public Inventory inventory;

        // State
        private float currentBattery;
        private bool isOn = false;
        private bool isDraining = false;
        private float lastDrainTime;
        private PlayerController playerController;

        // Flashlight color (warm yellow)
        private readonly Color flashlightColor = new Color(1f, 0.92f, 0.7f);

        // Passive vision state
        public bool IsFlashlightOn => isOn;
        public float CurrentBattery => currentBattery;
        public float BatteryPercentage => currentBattery / maxBattery;

        // ─── ISaveable ──────────────────────────────────────────
        public string SaveID => "Flashlight";

        public void CollectData(Dictionary<string, object> data)
        {
            data["isOn"] = isOn;
            data["currentBattery"] = currentBattery;
            data["maxBattery"] = maxBattery;
        }

        public void ApplyData(Dictionary<string, object> data)
        {
            if (data.TryGetValue("isOn", out var on))
            {
                bool shouldBeOn = (bool)on;
                if (shouldBeOn != isOn)
                    SetFlashlight(shouldBeOn);
            }
            if (data.TryGetValue("currentBattery", out var bat))
                currentBattery = System.Convert.ToSingle(bat);
            if (data.TryGetValue("maxBattery", out var max))
                maxBattery = System.Convert.ToSingle(max);
        }

        private void Start()
        {
            currentBattery = maxBattery;
            playerController = GetComponent<PlayerController>();

            if (spotLight == null)
            {
                // Create a child Spot Light
                GameObject lightObj = new GameObject("FlashlightSpot");
                lightObj.transform.SetParent(transform);
                lightObj.transform.localPosition = new Vector3(0.3f, 0.8f, 0.5f);
                lightObj.transform.localRotation = Quaternion.Euler(15f, 0f, 0f);

                spotLight = lightObj.AddComponent<Light>();
                spotLight.type = LightType.Spot;
                spotLight.color = flashlightColor;
                spotLight.intensity = 7f;       // intensidade aumentada para melhor visibilidade
                spotLight.range = 14f;          // alcance limitado
                spotLight.spotAngle = 55f;      // cone mais estreito
                spotLight.innerSpotAngle = 35f; // cone interno estreito
            }

            // Start with flashlight off
            SetFlashlight(false);

            if (inventory == null)
                inventory = GetComponent<Inventory>();
        }

        private void Update()
        {
            // Toggle input
            HandleToggleInput();

            // Drain battery while on
            if (isOn && currentBattery > 0f)
            {
                currentBattery -= drainRate * Time.deltaTime;
                if (currentBattery <= 0f)
                {
                    currentBattery = 0f;
                    SetFlashlight(false);
                    Debug.Log("[Flashlight] Bateria esgotada!");
                }
            }
        }

        private void HandleToggleInput()
        {
            bool toggleInput = false;

            // Keyboard: F
            if (Keyboard.current != null)
                toggleInput = Keyboard.current.fKey.wasPressedThisFrame;

            // Gamepad: LB / Left Shoulder
            if (!toggleInput && Gamepad.current != null)
                toggleInput = Gamepad.current.leftShoulder.wasPressedThisFrame;

            if (toggleInput)
            {
                if (isOn)
                {
                    SetFlashlight(false);
                }
                else if (currentBattery > 0f)
                {
                    SetFlashlight(true);
                }
                else
                {
                    Debug.Log("[Flashlight] Sem bateria!");
                }
            }
        }

        private void SetFlashlight(bool state)
        {
            isOn = state;
            if (spotLight != null)
                spotLight.enabled = state;

            Debug.Log($"[Flashlight] {(state ? "LIGADA" : "DESLIGADA")} | Bateria: {currentBattery:F0}/{maxBattery}");
        }

        /// <summary>
        /// Recarrega a lanterna com uma bateria do inventário.
        /// Chamado pelo Inventory quando um item tipo Battery é usado.
        /// </summary>
        /// <param name="amount">Quantidade de bateria a adicionar</param>
        /// <returns>True se recarregou</returns>
        public bool RechargeBattery(float amount)
        {
            if (amount <= 0f) return false;

            float oldBattery = currentBattery;
            currentBattery = Mathf.Min(currentBattery + amount, maxBattery);
            float gained = currentBattery - oldBattery;

            if (gained > 0f)
            {
                lastDrainTime = Time.time;
                Debug.Log($"[Flashlight] +{gained:F0} bateria → {currentBattery:F0}/{maxBattery}");
                return true;
            }

            Debug.Log("[Flashlight] Bateria já está no máximo!");
            return false;
        }
    }
}
