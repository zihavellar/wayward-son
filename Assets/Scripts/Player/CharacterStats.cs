using UnityEngine;
using WaywardSon.Attributes;

namespace WaywardSon
{
    /// <summary>
    /// Componente que gerencia os atributos do personagem.
    /// Serve como ponte entre o <see cref="AttributeSystemSO"/> e os componentes de gameplay
    /// como <see cref="CharacterStamina"/>.
    /// <para>
    /// Anexe este componente ao mesmo GameObject que possui o <see cref="CharacterStamina"/>.<br/>
    /// Os atributos são gerados aleatoriamente ao iniciar ou podem ser definidos externamente.
    /// </para>
    /// </summary>
    public class CharacterStats : MonoBehaviour
    {
        // ═══════════════════════════════════════════════════════════════════
        // INSPECTOR FIELDS
        // ═══════════════════════════════════════════════════════════════════

        [Header("Attribute Configuration")]
        [Tooltip("Referência ao ScriptableObject que armazena os dados de atributos do personagem.")]
        [SerializeField] private AttributeSystemSO _attributeSystem;

        [Header("Debug (Read Only)")]
        [SerializeField] private string _attributeSummary = "";

        // ═══════════════════════════════════════════════════════════════════
        // PUBLIC PROPERTIES
        // ═══════════════════════════════════════════════════════════════════

        /// <summary>Atributos atuais do personagem.</summary>
        public CharacterAttributes Attributes { get; private set; }

        /// <summary>Referência ao sistema de atributos (ScriptableObject).</summary>
        public AttributeSystemSO AttributeSystem => _attributeSystem;

        // ═══════════════════════════════════════════════════════════════════
        // LIFECYCLE
        // ═══════════════════════════════════════════════════════════════════

        private void Awake()
        {
            if (_attributeSystem != null)
            {
                _attributeSystem.GenerateRandomAttributes();
                Attributes = _attributeSystem.GetCharacterAttributes();
                _attributeSummary = Attributes.GetSummary();
            }
            else
            {
                Debug.LogWarning(
                    "[CharacterStats] Nenhum AttributeSystemSO atribuído. " +
                    "Usando atributos padrão (todos em 1).");
                Attributes = default;
            }
        }

        // ═══════════════════════════════════════════════════════════════════
        // PUBLIC METHODS
        // ═══════════════════════════════════════════════════════════════════

        /// <summary>
        /// Define um novo sistema de atributos, gera novos valores aleatórios
        /// e atualiza todos os componentes dependentes.
        /// </summary>
        /// <param name="system">Novo ScriptableObject de atributos.</param>
        public void SetAttributeSystem(AttributeSystemSO system)
        {
            _attributeSystem = system;

            if (_attributeSystem != null)
            {
                _attributeSystem.GenerateRandomAttributes();
                Attributes = _attributeSystem.GetCharacterAttributes();
                _attributeSummary = Attributes.GetSummary();
            }
        }

        /// <summary>
        /// Define atributos diretamente (útil para testes ou serialização).
        /// </summary>
        /// <param name="attributes">Atributos a serem aplicados.</param>
        public void SetAttributes(CharacterAttributes attributes)
        {
            Attributes = attributes;
            _attributeSummary = Attributes.GetSummary();
        }
    }
}
