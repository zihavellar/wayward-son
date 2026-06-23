using UnityEngine;
using WaywardSon.Attributes;

namespace WaywardSon
{
    /// <summary>
    /// Sistema de stamina do personagem.
    /// Controla corrida, regeneração e consumo de stamina.
    /// <para>
    /// StaminaMax = 100 + (Resistance × 10)<br/>
    /// Resistência 1 → 110 stamina | Resistência 5 → 150 stamina
    /// </para>
    /// </summary>
    public class CharacterStamina : MonoBehaviour
    {
        // ═══════════════════════════════════════════════════════════════════
        // INSPECTOR FIELDS
        // ═══════════════════════════════════════════════════════════════════

        [Header("Stamina Settings")]
        [Tooltip("Quantidade de stamina regenerada por segundo quando não está correndo.")]
        [SerializeField] private float _staminaRegenRate = 8f;

        [Tooltip("Quantidade de stamina consumida por segundo ao correr.")]
        [SerializeField] private float _sprintDrainRate = 12f;

        [Tooltip("Tempo de espera (em segundos) antes de começar a regenerar após parar de correr.")]
        [SerializeField] private float _staminaRegenDelay = 0.4f;

        [Header("Sprint")]
        [Tooltip("Multiplicador de velocidade ao correr (2.0 = dobra a velocidade).")]
        [SerializeField] private float _sprintSpeedMultiplier = 2.0f;

        [Header("Debug (Read Only)")]
        [SerializeField] private float _currentStamina;
        [SerializeField] private float _maxStamina = 100f;
        [SerializeField] private bool _isSprinting;

        // ═══════════════════════════════════════════════════════════════════
        // EVENTS
        // ═══════════════════════════════════════════════════════════════════

        /// <summary>Invocado quando a stamina muda. Parâmetros: stamina atual, stamina máxima.</summary>
        public System.Action<float, float> OnStaminaChanged;

        /// <summary>Invocado quando o personagem começa a correr.</summary>
        public System.Action OnSprintStarted;

        /// <summary>Invocado quando o personagem para de correr.</summary>
        public System.Action OnSprintEnded;

        /// <summary>Invocado quando a stamina atinge zero.</summary>
        public System.Action OnStaminaDepleted;

        // ═══════════════════════════════════════════════════════════════════
        // PUBLIC PROPERTIES
        // ═══════════════════════════════════════════════════════════════════

        /// <summary>Quantidade atual de stamina.</summary>
        public float CurrentStamina => _currentStamina;

        /// <summary>Stamina máxima (calculada a partir de Resistance).</summary>
        public float MaxStamina => _maxStamina;

        /// <summary>Stamina normalizada (0 a 1).</summary>
        public float StaminaNormalized => _currentStamina / _maxStamina;

        /// <summary>Percentual de stamina (0 a 1). Alias de <see cref="StaminaNormalized"/>.</summary>
        public float StaminaPercentage => _currentStamina / _maxStamina;

        /// <summary>Indica se o personagem está correndo no momento.</summary>
        public bool IsSprinting => _isSprinting;

        /// <summary>Indica se o personagem pode iniciar uma corrida.</summary>
        public bool CanSprint => _currentStamina > 0f;

        /// <summary>Multiplicador de velocidade ao correr.</summary>
        public float SprintSpeedMultiplier => _sprintSpeedMultiplier;

        // ═══════════════════════════════════════════════════════════════════
        // PRIVATE STATE
        // ═══════════════════════════════════════════════════════════════════

        private float _lastSprintTime;
        private CharacterAttributes _attributes;

        // ═══════════════════════════════════════════════════════════════════
        // LIFECYCLE
        // ═══════════════════════════════════════════════════════════════════

        private void Awake()
        {
            // Tenta obter os atributos do CharacterStats no mesmo GameObject
            CharacterStats stats = GetComponent<CharacterStats>();
            if (stats != null)
            {
                _attributes = stats.Attributes;
            }

            InitializeStamina();
        }

        private void Update()
        {
            if (_isSprinting)
            {
                DrainStamina();
            }
            else
            {
                RegenerateStamina();
            }
        }

        // ═══════════════════════════════════════════════════════════════════
        // PUBLIC METHODS
        // ═══════════════════════════════════════════════════════════════════

        /// <summary>
        /// Inicializa (ou recalcula) a stamina máxima com base no atributo Resistance.
        /// <para>Fórmula: StaminaMax = 100 + (Resistance × 10)</para>
        /// </summary>
        public void InitializeStamina()
        {
            int resistance = _attributes.Resistance;

            // Garante que Resistance está no intervalo válido (1-5)
            resistance = Mathf.Clamp(resistance, 1, 5);

            _maxStamina = 100f + (resistance * 10f);
            _currentStamina = _maxStamina;

            OnStaminaChanged?.Invoke(_currentStamina, _maxStamina);
        }

        /// <summary>
        /// Tenta iniciar a corrida.
        /// </summary>
        /// <returns>True se a corrida foi iniciada com sucesso.</returns>
        public bool TryStartSprint()
        {
            if (!CanSprint) return false;

            _isSprinting = true;
            OnSprintStarted?.Invoke();
            return true;
        }

        /// <summary>
        /// Para a corrida imediatamente.
        /// </summary>
        public void StopSprint()
        {
            if (!_isSprinting) return;

            _isSprinting = false;
            _lastSprintTime = Time.time;
            OnSprintEnded?.Invoke();
        }

        /// <summary>
        /// Atualiza os atributos do personagem e recalcula a stamina máxima.
        /// </summary>
        /// <param name="attributes">Novos atributos do personagem.</param>
        public void SetAttributes(CharacterAttributes attributes)
        {
            _attributes = attributes;
            InitializeStamina();
        }

        /// <summary>
        /// Restaura stamina diretamente (ex: por item consumível).
        /// </summary>
        /// <param name="amount">Quantidade de stamina a restaurar.</param>
        public void RestoreStamina(float amount)
        {
            if (amount <= 0f) return;

            float oldStamina = _currentStamina;
            _currentStamina = Mathf.Min(_currentStamina + amount, _maxStamina);

            if (!Mathf.Approximately(_currentStamina, oldStamina))
            {
                OnStaminaChanged?.Invoke(_currentStamina, _maxStamina);
            }
        }

        // ═══════════════════════════════════════════════════════════════════
        // PRIVATE METHODS
        // ═══════════════════════════════════════════════════════════════════

        /// <summary>
        /// Drena stamina enquanto o personagem está correndo.
        /// Se a stamina chegar a zero, para a corrida automaticamente.
        /// </summary>
        private void DrainStamina()
        {
            _currentStamina -= _sprintDrainRate * Time.deltaTime;
            _currentStamina = Mathf.Max(0f, _currentStamina);

            OnStaminaChanged?.Invoke(_currentStamina, _maxStamina);

            if (_currentStamina <= 0f)
            {
                StopSprint();
                OnStaminaDepleted?.Invoke();
            }
        }

        /// <summary>
        /// Regenera stamina quando o personagem não está correndo,
        /// respeitando o delay após a última vez que correu.
        /// </summary>
        private void RegenerateStamina()
        {
            if (_currentStamina >= _maxStamina) return;

            if (Time.time - _lastSprintTime < _staminaRegenDelay) return;

            _currentStamina += _staminaRegenRate * Time.deltaTime;
            _currentStamina = Mathf.Min(_maxStamina, _currentStamina);

            OnStaminaChanged?.Invoke(_currentStamina, _maxStamina);
        }
    }
}
