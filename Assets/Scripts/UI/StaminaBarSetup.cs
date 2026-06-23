using UnityEngine;
using UnityEngine.UI;

namespace WaywardSon
{
    /// <summary>
    /// Helper de Editor para criar a barra de stamina com estrutura de UI completa.
    /// Gera background, fill e o componente StaminaBarUI automaticamente.
    ///
    /// Uso no Editor:
    /// 1. Crie um GameObject vazio (ou selecione um Canvas existente)
    /// 2. Anexe este script
    /// 3. Clique em "Create Stamina Bar" no Inspector (botão checkbox)
    /// 4. Remova este script após a criação (é opcional)
    ///
    /// Uso em Runtime:
    /// Chame CreateStaminaBar() para instanciar a barra programaticamente.
    /// </summary>
    [ExecuteInEditMode]
    public class StaminaBarSetup : MonoBehaviour
    {
        // ═══════════════════════════════════════════════════════════════════
        // INSPECTOR SETTINGS
        // ═══════════════════════════════════════════════════════════════════

        [Header("Setup Trigger")]
        [Tooltip("Marque este checkbox para criar a barra de stamina no Editor.")]
        [SerializeField] private bool _createUI = false;

        [Header("Bar Dimensions")]
        [SerializeField] private Vector2 _barSize = new Vector2(200f, 20f);

        [Header("Colors")]
        [SerializeField] private Color _barColor = new Color(0.2f, 0.8f, 0.2f);
        [SerializeField] private Color _backgroundColor = new Color(0.1f, 0.1f, 0.12f, 0.85f);

        [Header("Layout")]
        [Tooltip("Posição da barra relativa ao pivot do pai (canto inferior esquerdo).")]
        [SerializeField] private Vector2 _anchoredPosition = new Vector2(110f, 50f);
        [SerializeField] private Vector2 _anchorMin = new Vector2(0f, 0f);
        [SerializeField] private Vector2 _anchorMax = new Vector2(0f, 0f);

        // ═══════════════════════════════════════════════════════════════════
        // EDITOR TRIGGER
        // ═══════════════════════════════════════════════════════════════════

        private void Update()
        {
#if UNITY_EDITOR
            if (_createUI)
            {
                _createUI = false;
                CreateStaminaBar();
            }
#endif
        }

        // ═══════════════════════════════════════════════════════════════════
        // PUBLIC API
        // ═══════════════════════════════════════════════════════════════════

        /// <summary>
        /// Cria a estrutura completa da barra de stamina como filho deste GameObject.
        /// Gera: Background > Fill + StaminaBarUI component.
        /// </summary>
        /// <returns>O GameObject raiz da barra criada.</returns>
        public GameObject CreateStaminaBar()
        {
            // ── Root (this) ──
            RectTransform rootRect = GetComponent<RectTransform>();
            if (rootRect == null)
            {
                rootRect = gameObject.AddComponent<RectTransform>();
            }

            // ── Background ──
            GameObject bgObj = new GameObject("StaminaBar_Background");
            bgObj.transform.SetParent(transform, false);

            RectTransform bgRect = bgObj.AddComponent<RectTransform>();
            bgRect.anchorMin = _anchorMin;
            bgRect.anchorMax = _anchorMax;
            bgRect.anchoredPosition = _anchoredPosition;
            bgRect.sizeDelta = _barSize;

            Image bgImage = bgObj.AddComponent<Image>();
            bgImage.color = _backgroundColor;
            bgImage.raycastTarget = false;

            // ── Fill ──
            GameObject fillObj = new GameObject("StaminaBar_Fill");
            fillObj.transform.SetParent(bgObj.transform, false);

            RectTransform fillRect = fillObj.AddComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.offsetMin = Vector2.zero;
            fillRect.offsetMax = Vector2.zero;

            Image fillImage = fillObj.AddComponent<Image>();
            fillImage.color = _barColor;
            fillImage.type = Image.Type.Filled;
            fillImage.fillMethod = Image.FillMethod.Horizontal;
            fillImage.fillAmount = 1f;
            fillImage.raycastTarget = false;

            // ── StaminaBarUI Component ──
            StaminaBarUI barUI = gameObject.GetComponent<StaminaBarUI>();
            if (barUI == null)
                barUI = gameObject.AddComponent<StaminaBarUI>();

            // Use reflection to set private serialized fields (Editor only)
#if UNITY_EDITOR
            SetSerializedField(barUI, "_fillImage", fillImage);
            SetSerializedField(barUI, "_backgroundImage", bgImage);

            // Also try to auto-find CharacterStamina on the same object
            CharacterStamina stamina = GetComponent<CharacterStamina>();
            if (stamina == null)
                stamina = GetComponentInParent<CharacterStamina>();
            if (stamina != null)
                SetSerializedField(barUI, "_characterStamina", stamina);
#endif

            Debug.Log("[StaminaBarSetup] Barra de stamina criada com sucesso!");
            return bgObj;
        }

        // ═══════════════════════════════════════════════════════════════════
        // EDITOR UTILITY
        // ═══════════════════════════════════════════════════════════════════

#if UNITY_EDITOR
        /// <summary>
        /// Define um campo serializado via reflection (necessário para campos [SerializeField] privados).
        /// </summary>
        private void SetSerializedField(object target, string fieldName, object value)
        {
            var field = target.GetType().GetField(
                fieldName,
                System.Reflection.BindingFlags.NonPublic |
                System.Reflection.BindingFlags.Instance
            );

            if (field != null)
            {
                field.SetValue(target, value);
                UnityEditor.EditorUtility.SetDirty(target as Object);
            }
            else
            {
                Debug.LogWarning($"[StaminaBarSetup] Campo '{fieldName}' não encontrado em {target.GetType().Name}");
            }
        }

        /// <summary>
        /// Custom Inspector button. Chame via [ContextMenu] ou Inspector.
        /// </summary>
        [ContextMenu("Create Stamina Bar")]
        private void ContextCreateStaminaBar()
        {
            CreateStaminaBar();
        }
#endif
    }
}
