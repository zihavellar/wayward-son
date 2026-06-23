using UnityEngine;
using UnityEngine.UI;

namespace WaywardSon
{
    /// <summary>
    /// Controla a UI da barra de stamina.
    /// Exibe a stamina atual do jogador com preenchimento dinâmico,
    /// mudança de cor por faixa e animação de suavização.
    ///
    /// Dependências:
    /// - CharacterStamina (componente no mesmo ou outro GameObject)
    /// - Canvas com Image (Fill) para representar o preenchimento
    ///
    /// Uso:
    /// 1. Crie um Canvas > Panel > Image (background) > Image (fill)
    /// 2. Defina o Image fill como Image.Type.Filled / Horizontal
    /// 3. Anexe este script ao GameObject raiz da barra
    /// 4. Arraste as referências no Inspector
    /// </summary>
    public class StaminaBarUI : MonoBehaviour
    {
        // ═══════════════════════════════════════════════════════════════════
        // INSPECTOR REFERENCES
        // ═══════════════════════════════════════════════════════════════════

        [Header("References")]
        [SerializeField] private Image _fillImage;
        [SerializeField] private Image _backgroundImage;
        [SerializeField] private CharacterStamina _characterStamina;

        [Header("Colors")]
        [SerializeField] private Color _fullColor = new Color(0.2f, 0.8f, 0.2f);
        [SerializeField] private Color _mediumColor = new Color(0.8f, 0.8f, 0.2f);
        [SerializeField] private Color _lowColor = new Color(0.8f, 0.2f, 0.2f);
        [SerializeField] private Color _depletedColor = new Color(0.3f, 0.3f, 0.3f);

        [Header("Animation")]
        [SerializeField] private float _animationSpeed = 5f;
        [SerializeField] private bool _useAnimation = true;

        [Header("Thresholds")]
        [Tooltip("Percentual abaixo do qual a barra fica vermelha (0-1).")]
        [SerializeField, Range(0f, 1f)] private float _lowThreshold = 0.3f;
        [Tooltip("Percentual abaixo do qual a barra fica amarela (0-1).")]
        [SerializeField, Range(0f, 1f)] private float _mediumThreshold = 0.6f;

        [Header("Visibility")]
        [Tooltip("Delay (segundos) antes de esconder a barra após parar de correr.")]
        [SerializeField] private float _hideDelay = 2f;

        // ═══════════════════════════════════════════════════════════════════
        // PRIVATE STATE
        // ═══════════════════════════════════════════════════════════════════

        private float _targetFill = 1f;
        private float _currentFill = 1f;
        private CanvasGroup _canvasGroup;
        private bool _isSubscribed;

        // ═══════════════════════════════════════════════════════════════════
        // LIFECYCLE
        // ═══════════════════════════════════════════════════════════════════

        private void Awake()
        {
            _canvasGroup = GetComponent<CanvasGroup>();
            if (_canvasGroup == null)
                _canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }

        private void Start()
        {
            // Auto-find CharacterStamina if not assigned
            if (_characterStamina == null)
                _characterStamina = FindAnyObjectByType<CharacterStamina>();

            SubscribeEvents();

            // Initialize bar state
            if (_characterStamina != null)
            {
                UpdateStaminaBar(_characterStamina.CurrentStamina, _characterStamina.MaxStamina);

                // Show bar only if already sprinting
                if (_characterStamina.IsSprinting)
                    ShowBar();
                else
                    HideBarImmediate();
            }
        }

        private void OnDestroy()
        {
            UnsubscribeEvents();
        }

        private void Update()
        {
            if (_useAnimation && Mathf.Abs(_currentFill - _targetFill) > 0.001f)
            {
                _currentFill = Mathf.Lerp(_currentFill, _targetFill, Time.deltaTime * _animationSpeed);
                _fillImage.fillAmount = _currentFill;
                UpdateColor(_currentFill);
            }
        }

        // ═══════════════════════════════════════════════════════════════════
        // EVENT SUBSCRIPTION
        // ═══════════════════════════════════════════════════════════════════

        private void SubscribeEvents()
        {
            if (_characterStamina == null || _isSubscribed) return;

            _characterStamina.OnStaminaChanged += UpdateStaminaBar;
            _characterStamina.OnSprintStarted += ShowBar;
            _characterStamina.OnSprintEnded += HideBarDelayed;
            _isSubscribed = true;
        }

        private void UnsubscribeEvents()
        {
            if (_characterStamina == null || !_isSubscribed) return;

            _characterStamina.OnStaminaChanged -= UpdateStaminaBar;
            _characterStamina.OnSprintStarted -= ShowBar;
            _characterStamina.OnSprintEnded -= HideBarDelayed;
            _isSubscribed = false;
        }

        // ═══════════════════════════════════════════════════════════════════
        // STAMINA BAR UPDATE
        // ═══════════════════════════════════════════════════════════════════

        /// <summary>
        /// Atualiza a barra de stamina. Chamado pelo evento OnStaminaChanged.
        /// </summary>
        /// <param name="current">Stamina atual.</param>
        /// <param name="max">Stamina máxima.</param>
        public void UpdateStaminaBar(float current, float max)
        {
            if (_fillImage == null) return;

            _targetFill = max > 0f ? Mathf.Clamp01(current / max) : 0f;

            if (!_useAnimation)
            {
                _currentFill = _targetFill;
                _fillImage.fillAmount = _currentFill;
                UpdateColor(_currentFill);
            }
        }

        /// <summary>
        /// Atualiza a cor da barra com base no percentual atual.
        /// </summary>
        private void UpdateColor(float fill)
        {
            if (_fillImage == null) return;

            Color targetColor;

            if (fill <= _lowThreshold)
                targetColor = _lowColor;
            else if (fill <= _mediumThreshold)
                targetColor = _mediumColor;
            else
                targetColor = _fullColor;

            _fillImage.color = Color.Lerp(_fillImage.color, targetColor, Time.deltaTime * _animationSpeed);
        }

        // ═══════════════════════════════════════════════════════════════════
        // VISIBILITY
        // ═══════════════════════════════════════════════════════════════════

        /// <summary>Torna a barra de stamina visível.</summary>
        public void ShowBar()
        {
            CancelInvoke(nameof(HideBar));
            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = 1f;
                _canvasGroup.interactable = true;
                _canvasGroup.blocksRaycasts = true;
            }
        }

        /// <summary>
        /// Esconde a barra após um delay.
        /// Dá tempo ao jogador de ver a regeneração antes de desaparecer.
        /// </summary>
        public void HideBarDelayed()
        {
            CancelInvoke(nameof(HideBar));
            Invoke(nameof(HideBar), _hideDelay);
        }

        /// <summary>Esconde a barra imediatamente.</summary>
        public void HideBar()
        {
            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = 0f;
                _canvasGroup.interactable = false;
                _canvasGroup.blocksRaycasts = false;
            }
        }

        /// <summary>Esconde a barra sem animação (usado na inicialização).</summary>
        public void HideBarImmediate()
        {
            CancelInvoke(nameof(HideBar));
            HideBar();
        }

        // ═══════════════════════════════════════════════════════════════════
        // PUBLIC API — RUNTIME BINDING
        // ═══════════════════════════════════════════════════════════════════

        /// <summary>
        /// Vincula dinamicamente a um CharacterStamina.
        /// Útil quando o jogador é instanciado em runtime.
        /// </summary>
        /// <param name="stamina">Componente de stamina a vincular.</param>
        public void SetCharacterStamina(CharacterStamina stamina)
        {
            UnsubscribeEvents();

            _characterStamina = stamina;

            SubscribeEvents();

            if (_characterStamina != null)
                UpdateStaminaBar(_characterStamina.CurrentStamina, _characterStamina.MaxStamina);
        }

        /// <summary>
        /// Define a velocidade de animação da barra.
        /// </summary>
        /// <param name="speed">Velocidade de suavização (mais alto = mais rápido).</param>
        public void SetAnimationSpeed(float speed)
        {
            _animationSpeed = Mathf.Max(0.1f, speed);
        }

        /// <summary>
        /// Força a atualização imediata da barra (sem animação).
        /// </summary>
        public void ForceImmediateUpdate()
        {
            if (_characterStamina == null || _fillImage == null) return;

            _currentFill = _characterStamina.StaminaPercentage;
            _targetFill = _currentFill;
            _fillImage.fillAmount = _currentFill;
            UpdateColor(_currentFill);
        }
    }
}
