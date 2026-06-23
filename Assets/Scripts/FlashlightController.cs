using UnityEngine;
using UnityEngine.InputSystem;

namespace WaywardSon
{
    /// <summary>
    /// Controla a lanterna do jogador: Spot Light, toggle, bateria limitável,
    /// drenagem com uso e recarga via item Consumable (Battery).
    /// Também controla a "visão passiva" quando a lanterna está desligada.
    /// </summary>
    public class FlashlightController : MonoBehaviour
    {
        [Header("Flashlight Settings")]
        public Light spotLight;
        public float maxBattery = 100f;
        public float drainRate = 0.8f;        // bateria por segundo (~125s autonomia com 100 de bateria)
        public float resumeDrainDelay = 0.5f; // delay antes de voltar a drenar após recarga

        [Header("Vision Base Values")]
        [Tooltip("Alcance base da lanterna (sem bônus de Wits)")]
        [SerializeField] private float _baseFlashlightRange = 14f;
        [Tooltip("Intensidade base da lanterna")]
        [SerializeField] private float _baseFlashlightIntensity = 7f;
        [Tooltip("Alcance base da visão passiva (sem lanterna)")]
        [SerializeField] private float _basePassiveVisionRange = 3f;

        [Header("Passive Vision (No Flashlight)")]
        [Tooltip("Raixo de visão passiva (sem lanterna) — menor que a lanterna")]
        public float passiveVisionRange = 3f;
        [Tooltip("Redução de velocidade quando sem lanterna (ex: 0.8 = 80% da velocidade normal)")]
        public float passiveSpeedMultiplier = 0.8f;
        [Tooltip("Redução de dano quando sem lanterna")]
        public float passiveDamageMultiplier = 0.7f;

        [Header("References")]
        public Inventory inventory;
        [Tooltip("Referência ao sistema de atributos do personagem (para ler Wits)")]
        [SerializeField] private CharacterStats _characterStats;

        // State
        private float currentBattery;
        private bool isOn = false;
        private float lastDrainTime;
        private PlayerController playerController;

        // Flashlight color (warm yellow)
        private readonly Color flashlightColor = new Color(1f, 0.92f, 0.7f);

        // Passive vision state
        public bool IsFlashlightOn => isOn;
        public float CurrentBattery => currentBattery;
        public float BatteryPercentage => currentBattery / maxBattery;

        /// <summary>Alcance base da visão passiva (sem bônus de Wits).</summary>
        public float BasePassiveVisionRange => _basePassiveVisionRange;

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
                spotLight.intensity = _baseFlashlightIntensity;
                spotLight.range = _baseFlashlightRange;
                spotLight.spotAngle = 55f;      // cone mais estreito
                spotLight.innerSpotAngle = 35f; // cone interno estreito
            }

            // Start with flashlight off
            SetFlashlight(false);

            if (inventory == null)
                inventory = GetComponent<Inventory>();

            // Busca CharacterStats se não atribuído
            if (_characterStats == null)
                _characterStats = GetComponent<CharacterStats>();

            // Aplica bônus de Wits após inicializar
            ApplyWitsBonus();
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
            {
                spotLight.enabled = state;
                if (state) ApplyWitsBonus(); // Aplica bônus ao ligar
            }

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

        // ═══════════════════════════════════════════════════════════════════
        // WITS VISION BONUS
        // ═══════════════════════════════════════════════════════════════════

        /// <summary>
        /// Calcula o bônus de Wits para visão.
        /// Fórmula: +15% de alcance/intensidade por ponto de Wits (1-5).
        /// Wits 1 = 1.0x (base), Wits 2 = 1.15x, Wits 3 = 1.30x,
        /// Wits 4 = 1.45x, Wits 5 = 1.60x.
        /// </summary>
        /// <returns>Multiplicador de bônus (mínimo 1.0).</returns>
        private float GetWitsVisionBonus()
        {
            if (_characterStats == null) return 1f;

            int wits = _characterStats.Attributes.Wits;
            return 1f + ((wits - 1) * 0.15f);
        }

        /// <summary>
        /// Aplica o bônus de Wits ao alcance e intensidade da lanterna.
        /// Deve ser chamado ao ligar a lanterna e ao inicializar.
        /// </summary>
        public void ApplyWitsBonus()
        {
            if (spotLight == null) return;

            float bonus = GetWitsVisionBonus();
            spotLight.range = _baseFlashlightRange * bonus;
            spotLight.intensity = _baseFlashlightIntensity * bonus;

            Debug.Log($"[Flashlight] Wits bonus aplicado: {bonus:F2}x → Range: {spotLight.range:F1}, Intensity: {spotLight.intensity:F1}");
        }

        /// <summary>
        /// Retorna o alcance da visão passiva com bônus de Wits.
        /// </summary>
        /// <returns>Alcance efetivo da visão passiva.</returns>
        public float GetEffectivePassiveVisionRange()
        {
            float bonus = GetWitsVisionBonus();
            return _basePassiveVisionRange * bonus;
        }
    }
}
