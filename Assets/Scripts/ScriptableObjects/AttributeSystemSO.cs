using System.Text;
using UnityEngine;

namespace WaywardSon.Attributes
{
    // =====================================================================
    // ENUMS
    // =====================================================================

    /// <summary>
    /// Categorias de atributos do personagem.
    /// </summary>
    public enum AttributeCategory
    {
        Social,
        Physical,
        Mental,
        Supernatural
    }

    /// <summary>
    /// Todos os atributos individuais, agrupados por categoria.
    /// </summary>
    public enum AttributeType
    {
        // ── Social ──
        Charisma,
        Manipulation,
        Composure,

        // ── Physical ──
        Strength,
        Dexterity,
        Resistance,

        // ── Mental ──
        Intelligence,
        Wits,
        Resolution,

        // ── Supernatural ──
        Sensibility,
        Understanding,
        Protection
    }

    // =====================================================================
    // DATA STRUCTURES
    // =====================================================================

    /// <summary>
    /// Estrutura serializável que representa um único atributo com seu valor.
    /// </summary>
    [System.Serializable]
    public struct AttributeEntry
    {
        public AttributeType type;
        public int value;

        public AttributeEntry(AttributeType type, int value)
        {
            this.type = type;
            this.value = value;
        }

        public override string ToString() => $"{type}: {value}";
    }

    /// <summary>
    /// Estrutura serializável que agrupa 3 atributos de uma mesma categoria.
    /// </summary>
    [System.Serializable]
    public struct CategoryData
    {
        public AttributeCategory category;
        public AttributeEntry[] attributes;

        public CategoryData(AttributeCategory category, AttributeEntry[] attributes)
        {
            this.category = category;
            this.attributes = attributes;
        }

        public int Total
        {
            get
            {
                int sum = 0;
                if (attributes != null)
                {
                    for (int i = 0; i < attributes.Length; i++)
                        sum += attributes[i].value;
                }
                return sum;
            }
        }
    }

    // =====================================================================
    // SCRIPTABLE OBJECT
    // =====================================================================

    /// <summary>
    /// Sistema de atributos 100% aleatório para personagens do Wayward Son.
    /// <para>
    /// Regras de geração:<br/>
    /// • Todos os atributos começam em 1.<br/>
    /// • 20 pontos são distribuídos entre 4 categorias (7, 5, 5, 3).<br/>
    /// • Dentro de cada categoria, os pontos são sorteados entre os 3 atributos.<br/>
    /// • Máximo 5, mínimo 1 em qualquer atributo.
    /// </para>
    /// </summary>
    [CreateAssetMenu(
        fileName = "AttributeSystem",
        menuName = "Wayward Son/Attributes/Attribute System",
        order = 0)]
    public class AttributeSystemSO : ScriptableObject
    {
        // =================================================================
        // CONSTANTS
        // =================================================================

        private const int MaxValue = 5;
        private const int TotalPoints = 20;

        // =================================================================
        // INSPECTOR FIELDS
        // =================================================================

        [Header("Category Data")]
        [SerializeField] private CategoryData[] _categories = new CategoryData[4];

        // =================================================================
        // PUBLIC PROPERTIES
        // =================================================================

        /// <summary>
        /// Dados brutos das 4 categorias (leitura externa segura).
        /// </summary>
        public CategoryData[] Categories => _categories;

        // =================================================================
        // GENERATION
        // =====================================================================

        /// <summary>
        /// Gera todos os atributos do personagem usando o <see cref="AttributeGenerator"/>.
        /// </summary>
        public void GenerateRandomAttributes()
        {
            CharacterAttributes attrs = AttributeGenerator.Generate();
            ApplyAttributes(attrs);
        }

        /// <summary>
        /// Converte uma struct <see cref="CharacterAttributes"/> para o array interno de categorias.
        /// </summary>
        /// <param name="attrs">Atributos gerados pelo <see cref="AttributeGenerator"/>.</param>
        private void ApplyAttributes(CharacterAttributes attrs)
        {
            // Social
            _categories[0] = new CategoryData(AttributeCategory.Social, new AttributeEntry[]
            {
                new AttributeEntry(AttributeType.Charisma, attrs.Charisma),
                new AttributeEntry(AttributeType.Manipulation, attrs.Manipulation),
                new AttributeEntry(AttributeType.Composure, attrs.Composure)
            });

            // Physical
            _categories[1] = new CategoryData(AttributeCategory.Physical, new AttributeEntry[]
            {
                new AttributeEntry(AttributeType.Strength, attrs.Strength),
                new AttributeEntry(AttributeType.Dexterity, attrs.Dexterity),
                new AttributeEntry(AttributeType.Resistance, attrs.Resistance)
            });

            // Mental
            _categories[2] = new CategoryData(AttributeCategory.Mental, new AttributeEntry[]
            {
                new AttributeEntry(AttributeType.Intelligence, attrs.Intelligence),
                new AttributeEntry(AttributeType.Wits, attrs.Wits),
                new AttributeEntry(AttributeType.Resolution, attrs.Resolution)
            });

            // Supernatural
            _categories[3] = new CategoryData(AttributeCategory.Supernatural, new AttributeEntry[]
            {
                new AttributeEntry(AttributeType.Sensibility, attrs.Sensibility),
                new AttributeEntry(AttributeType.Understanding, attrs.Understanding),
                new AttributeEntry(AttributeType.Protection, attrs.Protection)
            });
        }

        // =================================================================
        // QUERY METHODS
        // =====================================================================

        /// <summary>
        /// Retorna o valor de um atributo específico dentro de uma categoria.
        /// </summary>
        /// <param name="category">Categoria do atributo.</param>
        /// <param name="type">Tipo do atributo.</param>
        /// <returns>Valor do atributo (1-5) ou -1 se não encontrado.</returns>
        public int GetAttributeValue(AttributeCategory category, AttributeType type)
        {
            CategoryData catData = GetCategoryData(category);
            if (catData.attributes == null) return -1;

            for (int i = 0; i < catData.attributes.Length; i++)
            {
                if (catData.attributes[i].type == type)
                    return catData.attributes[i].value;
            }

            return -1;
        }

        /// <summary>
        /// Retorna o total de pontos de uma categoria (soma dos 3 atributos).
        /// </summary>
        public int GetCategoryTotal(AttributeCategory category)
        {
            return GetCategoryData(category).Total;
        }

        /// <summary>
        /// Retorna os dados completos de uma categoria.
        /// </summary>
        public CategoryData GetCategoryData(AttributeCategory category)
        {
            int index = (int)category;
            if (_categories != null && index >= 0 && index < _categories.Length)
                return _categories[index];

            return default;
        }

        /// <summary>
        /// Retorna um resumo formatado do personagem com todos os atributos.
        /// </summary>
        public string GetCharacterSummary()
        {
            var sb = new StringBuilder();
            sb.AppendLine("═══════════════════════════════════");
            sb.AppendLine("         CHARACTER ATTRIBUTES      ");
            sb.AppendLine("═══════════════════════════════════");

            string[] categoryNames = { "SOCIAL", "PHYSICAL", "MENTAL", "SUPERNATURAL" };

            for (int catIndex = 0; catIndex < _categories.Length; catIndex++)
            {
                CategoryData cat = _categories[catIndex];
                string catName = catIndex < categoryNames.Length
                    ? categoryNames[catIndex]
                    : cat.category.ToString().ToUpper();

                sb.AppendLine();
                sb.AppendLine($"┌─ {catName} (Total: {cat.Total}) ─");

                if (cat.attributes != null)
                {
                    for (int i = 0; i < cat.attributes.Length; i++)
                    {
                        AttributeEntry attr = cat.attributes[i];
                        string bar = GenerateBar(attr.value, MaxValue);
                        sb.AppendLine($"│  {attr.type,-14} [{bar}] {attr.value}");
                    }
                }

                sb.AppendLine("└──────────────────────────────");
            }

            sb.AppendLine();
            sb.AppendLine($"TOTAL POINTS: {GetTotalPoints()}/{TotalPoints}");
            sb.AppendLine("═══════════════════════════════════");

            return sb.ToString();
        }

        /// <summary>
        /// Retorna o total de pontos gastos em todos os atributos.
        /// </summary>
        public int GetTotalPoints()
        {
            int total = 0;
            for (int i = 0; i < _categories.Length; i++)
                total += _categories[i].Total;
            return total;
        }

        /// <summary>
        /// Retorna os atributos como struct <see cref="CharacterAttributes"/>.
        /// Útil para integrar com sistemas de gameplay que dependem de atributos individuais.
        /// </summary>
        /// <returns>Struct preenchida com todos os 12 atributos.</returns>
        public CharacterAttributes GetCharacterAttributes()
        {
            CharacterAttributes attrs = default;

            // Social
            attrs.Charisma     = GetAttributeValue(AttributeCategory.Social,     AttributeType.Charisma);
            attrs.Manipulation = GetAttributeValue(AttributeCategory.Social,     AttributeType.Manipulation);
            attrs.Composure    = GetAttributeValue(AttributeCategory.Social,     AttributeType.Composure);

            // Physical
            attrs.Strength   = GetAttributeValue(AttributeCategory.Physical,   AttributeType.Strength);
            attrs.Dexterity  = GetAttributeValue(AttributeCategory.Physical,   AttributeType.Dexterity);
            attrs.Resistance = GetAttributeValue(AttributeCategory.Physical,   AttributeType.Resistance);

            // Mental
            attrs.Intelligence = GetAttributeValue(AttributeCategory.Mental,   AttributeType.Intelligence);
            attrs.Wits         = GetAttributeValue(AttributeCategory.Mental,   AttributeType.Wits);
            attrs.Resolution   = GetAttributeValue(AttributeCategory.Mental,   AttributeType.Resolution);

            // Supernatural
            attrs.Sensibility   = GetAttributeValue(AttributeCategory.Supernatural, AttributeType.Sensibility);
            attrs.Understanding = GetAttributeValue(AttributeCategory.Supernatural, AttributeType.Understanding);
            attrs.Protection    = GetAttributeValue(AttributeCategory.Supernatural, AttributeType.Protection);

            return attrs;
        }

        // =================================================================
        // PRIVATE HELPERS
        // =====================================================================

        /// <summary>
        /// Gera uma barra visual para exibição no resumo.
        /// Ex: "█████░░░░░" para valor 3 de 5.
        /// </summary>
        private static string GenerateBar(int value, int maxValue)
        {
            const int barLength = 10;
            int filled = Mathf.RoundToInt((float)value / maxValue * barLength);

            var sb = new StringBuilder(barLength);
            for (int i = 0; i < barLength; i++)
            {
                sb.Append(i < filled ? '█' : '░');
            }
            return sb.ToString();
        }
    }
}
