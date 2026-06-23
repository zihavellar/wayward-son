using System;
using System.Text;

namespace WaywardSon.Attributes
{
    /// <summary>
    /// Struct que representa todos os 12 atributos de um personagem.
    /// Fornece métodos para consultar totais por categoria e gerar resumos formatados.
    /// Os nomes dos atributos seguem a especificação do design document.
    /// </summary>
    public struct CharacterAttributes
    {
        // ── Social ──────────────────────────────────────────────────────
        public int Charisma;
        public int Manipulation;
        public int Composure;

        // ── Physical ────────────────────────────────────────────────────
        public int Strength;
        public int Dexterity;
        public int Resistance;

        // ── Mental ──────────────────────────────────────────────────────
        public int Intelligence;
        public int Wits;
        public int Resolution;

        // ── Supernatural ────────────────────────────────────────────────
        public int Sensibility;
        public int Understanding;
        public int Protection;

        // ── Consulta por Categoria ──────────────────────────────────────

        /// <summary>
        /// Retorna a soma dos 3 atributos de uma categoria específica.
        /// </summary>
        public int GetCategoryTotal(AttributeCategory category)
        {
            switch (category)
            {
                case AttributeCategory.Social:
                    return Charisma + Manipulation + Composure;
                case AttributeCategory.Physical:
                    return Strength + Dexterity + Resistance;
                case AttributeCategory.Mental:
                    return Intelligence + Wits + Resolution;
                case AttributeCategory.Supernatural:
                    return Sensibility + Understanding + Protection;
                default:
                    return 0;
            }
        }

        /// <summary>
        /// Retorna um valor individual de atributo pelo seu índice (0-11).
        /// Ordem: Charisma(0), Manipulation(1), Composure(2),
        ///        Strength(3), Dexterity(4), Resistance(5),
        ///        Intelligence(6), Wits(7), Resolution(8),
        ///        Sensibility(9), Understanding(10), Protection(11).
        /// </summary>
        public int GetAttribute(int index)
        {
            switch (index)
            {
                case 0:  return Charisma;
                case 1:  return Manipulation;
                case 2:  return Composure;
                case 3:  return Strength;
                case 4:  return Dexterity;
                case 5:  return Resistance;
                case 6:  return Intelligence;
                case 7:  return Wits;
                case 8:  return Resolution;
                case 9:  return Sensibility;
                case 10: return Understanding;
                case 11: return Protection;
                default: throw new ArgumentOutOfRangeException("index");
            }
        }

        /// <summary>
        /// Retorna um resumo formatado de todos os atributos agrupados por categoria.
        /// Exemplo de saída:
        ///   Social       : Cha=3  Man=2  Com=5  [Total: 10]
        ///   Physical     : Str=4  Dex=2  Res=2  [Total: 8]
        ///   Mental       : Int=3  Wit=1  Res=2  [Total: 6]
        ///   Supernatural : Sen=2  Und=3  Pro=3  [Total: 8]
        ///   ========================================
        ///   Total Geral: 32 / 32
        /// </summary>
        public string GetSummary()
        {
            var sb = new StringBuilder(512);

            AppendCategory(sb, "Social",       Charisma, Manipulation, Composure,
                           GetCategoryTotal(AttributeCategory.Social));
            AppendCategory(sb, "Physical",     Strength, Dexterity, Resistance,
                           GetCategoryTotal(AttributeCategory.Physical));
            AppendCategory(sb, "Mental",       Intelligence, Wits, Resolution,
                           GetCategoryTotal(AttributeCategory.Mental));
            AppendCategory(sb, "Supernatural", Sensibility, Understanding, Protection,
                           GetCategoryTotal(AttributeCategory.Supernatural));

            sb.AppendLine();
            int total = GetCategoryTotal(AttributeCategory.Social)
                      + GetCategoryTotal(AttributeCategory.Physical)
                      + GetCategoryTotal(AttributeCategory.Mental)
                      + GetCategoryTotal(AttributeCategory.Supernatural);
            sb.AppendFormat("Total Geral: {0} / 32", total);

            return sb.ToString();
        }

        // ── Helpers Privados ────────────────────────────────────────────

        private static void AppendCategory(StringBuilder sb, string catName,
            int a1, int a2, int a3, int total)
        {
            // Obtém os nomes dos atributos dessa categoria
            string n1, n2, n3;
            GetAttributeNames(catName, out n1, out n2, out n3);

            sb.AppendFormat("  {0,-13}: {1}={2,-2} {3}={4,-2} {5}={6,-2}  [Total: {7}]",
                catName, n1, a1, n2, a2, n3, a3, total);
            sb.AppendLine();
        }

        private static void GetAttributeNames(string category,
            out string name1, out string name2, out string name3)
        {
            switch (category)
            {
                case "Social":
                    name1 = "Cha"; name2 = "Man"; name3 = "Com";
                    break;
                case "Physical":
                    name1 = "Str"; name2 = "Dex"; name3 = "Res";
                    break;
                case "Mental":
                    name1 = "Int"; name2 = "Wit"; name3 = "Res";
                    break;
                case "Supernatural":
                    name1 = "Sen"; name2 = "Und"; name3 = "Pro";
                    break;
                default:
                    name1 = "?"; name2 = "?"; name3 = "?";
                    break;
            }
        }
    }

    /// <summary>
    /// Gerador de atributos aleatórios para personagens de Wayward Son.
    ///
    /// ALGORITMO:
    /// 1. Todos os 12 atributos começam com valor 1 (base).
    /// 2. Uma categoria recebe +7 pontos bônus (total 10 para dividir entre 3 atributos).
    /// 3. Duas categorias recebem +5 pontos bônus cada (total 8 para dividir).
    /// 4. A categoria restante recebe +3 pontos bônus (total 6 para dividir).
    /// 5. Dentro de cada categoria, os pontos são distribuídos aleatoriamente
    ///    entre os 3 atributos, respeitando: 1 ≤ atributo ≤ 5.
    ///
    /// TOTAL DE PONTOS: 12 (base) + 7 + 5 + 5 + 3 = 32
    /// </summary>
    public static class AttributeGenerator
    {
        // ── Constantes ──────────────────────────────────────────────────

        private const int NumCategories    = 4;
        private const int AttrsPerCategory = 3;
        private const int BaseValue        = 1;
        private const int MaxAttributeValue = 5;
        private const int MaxRetries       = 100;

        // ── Métodos Públicos ────────────────────────────────────────────

        /// <summary>
        /// Gera um conjunto aleatório de 12 atributos para um personagem.
        /// </summary>
        public static CharacterAttributes Generate()
        {
            return Generate(new System.Random());
        }

        /// <summary>
        /// Gera um conjunto aleatório de 12 atributos usando uma seed específica.
        /// Útil para testes e reprodução de resultados.
        /// </summary>
        public static CharacterAttributes Generate(int seed)
        {
            return Generate(new System.Random(seed));
        }

        /// <summary>
        /// Gera um conjunto aleatório de 12 atributos usando uma instância de Random.
        /// </summary>
        public static CharacterAttributes Generate(System.Random rng)
        {
            if (rng == null) throw new ArgumentNullException("rng");

            // ── Passo 1: Atribuir bônus de categoria ────────────────────
            // Cada categoria recebe um dos bônus: 7, 5, 5, 3
            // Total de pontos extras: 7 + 5 + 5 + 3 = 20
            int[] categoryBonuses = AssignCategoryBonuses(rng);

            // ── Passo 2: Distribuir pontos dentro de cada categoria ─────
            // Cada categoria tem 3 atributos que começam em 1
            // Total por categoria: 3 (base) + bônus
            int[][] categoryAttributes = new int[NumCategories][];

            for (int cat = 0; cat < NumCategories; cat++)
            {
                int totalPoints = AttrsPerCategory * BaseValue + categoryBonuses[cat];
                categoryAttributes[cat] = new int[AttrsPerCategory];
                DistributePointsAmongThree(categoryAttributes[cat], totalPoints, rng);
            }

            // ── Passo 3: Montar a struct de resultado ───────────────────
            CharacterAttributes result;
            result.Charisma      = categoryAttributes[0][0];
            result.Manipulation  = categoryAttributes[0][1];
            result.Composure     = categoryAttributes[0][2];
            result.Strength      = categoryAttributes[1][0];
            result.Dexterity     = categoryAttributes[1][1];
            result.Resistance    = categoryAttributes[1][2];
            result.Intelligence  = categoryAttributes[2][0];
            result.Wits          = categoryAttributes[2][1];
            result.Resolution    = categoryAttributes[2][2];
            result.Sensibility   = categoryAttributes[3][0];
            result.Understanding = categoryAttributes[3][1];
            result.Protection    = categoryAttributes[3][2];
            return result;
        }

        // ── Métodos Privados ────────────────────────────────────────────

        /// <summary>
        /// Distribui os bônus entre as 4 categorias.
        /// Resultado: array de 4 inteiros com valores {7, 5, 5, 3} em ordem aleatória.
        ///
        /// Algoritmo: Fisher-Yates shuffle em array fixo.
        /// </summary>
        private static int[] AssignCategoryBonuses(System.Random rng)
        {
            int[] bonuses = new int[] { 7, 5, 5, 3 };

            // Fisher-Yates shuffle
            for (int i = bonuses.Length - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                int temp = bonuses[i];
                bonuses[i] = bonuses[j];
                bonuses[j] = temp;
            }

            return bonuses;
        }

        /// <summary>
        /// Distribui 'totalPoints' entre 3 atributos, onde cada um começa em BaseValue (1)
        /// e nenhum pode exceder MaxAttributeValue (5).
        ///
        /// Algoritmo de distribuição:
        /// - Calcula pontos extras = totalPoints - 3 (pois cada atributo começa em 1)
        /// - Divide os extras entre 3 posições usando separadores aleatórios
        ///   (método "stars and bars" com rejeição)
        /// - Rejeita distribuições onde qualquer atributo exceda 5
        /// - Repete até encontrar uma distribuição válida
        ///
        /// Valores possíveis por categoria:
        ///   +7 → total=10, extras=7, range=[1..5]  → ex: (5,3,2), (4,3,3), (5,4,1)
        ///   +5 → total=8,  extras=5, range=[1..5]  → ex: (4,2,2), (3,3,2), (5,1,2)
        ///   +3 → total=6,  extras=3, range=[1..5]  → ex: (2,2,2), (3,2,1), (1,2,3)
        /// </summary>
        private static void DistributePointsAmongThree(int[] attributes, int totalPoints, System.Random rng)
        {
            int minPerAttr  = BaseValue;
            int maxPerAttr  = MaxAttributeValue;
            int maxExtra    = maxPerAttr - minPerAttr;

            // Total de pontos extras a distribuir entre os 3 atributos
            int extraPoints = totalPoints - AttrsPerCategory * minPerAttr;

            // Se não há pontos extras, todos ficam no valor base
            if (extraPoints <= 0)
            {
                attributes[0] = minPerAttr;
                attributes[1] = minPerAttr;
                attributes[2] = minPerAttr;
                return;
            }

            // Distribuição por separadores aleatórios com rejeição
            // Gera 2 separadores no intervalo [0, extraPoints]
            // As 3 "faixas" entre os separadores determinam os pontos extras de cada atributo
            int attempts = 0;

            while (attempts < MaxRetries)
            {
                attempts++;

                // Gerar 2 separadores aleatórios
                int s1 = rng.Next(extraPoints + 1);
                int s2 = rng.Next(extraPoints + 1);

                // Ordenar: s1 <= s2
                if (s1 > s2)
                {
                    int temp = s1;
                    s1 = s2;
                    s2 = temp;
                }

                // Calcular os pontos extras para cada atributo
                int extra0 = s1;
                int extra1 = s2 - s1;
                int extra2 = extraPoints - s2;

                // Validar: nenhum extra pode exceder o máximo permitido
                if (extra0 <= maxExtra && extra1 <= maxExtra && extra2 <= maxExtra)
                {
                    attributes[0] = minPerAttr + extra0;
                    attributes[1] = minPerAttr + extra1;
                    attributes[2] = minPerAttr + extra2;
                    return;
                }
            }

            // Fallback seguro (não deveria acontecer com os valores do jogo)
            // Distribui uniformemente e ajusta o excesso no último
            int baseDiv = extraPoints / AttrsPerCategory;
            int remainder = extraPoints % AttrsPerCategory;
            attributes[0] = Math.Min(minPerAttr + (remainder > 0 ? 1 : 0) + (remainder > 0 ? baseDiv : 0), maxPerAttr);
            attributes[1] = Math.Min(minPerAttr + (remainder > 1 ? 1 : 0) + baseDiv, maxPerAttr);
            attributes[2] = Math.Min(minPerAttr + baseDiv, maxPerAttr);

            // Garantir que o total está correto
            int currentTotal = attributes[0] + attributes[1] + attributes[2];
            int diff = totalPoints - currentTotal;
            for (int i = 0; i < AttrsPerCategory && diff != 0; i++)
            {
                if (diff > 0 && attributes[i] < maxPerAttr)
                {
                    int add = Math.Min(diff, maxPerAttr - attributes[i]);
                    attributes[i] += add;
                    diff -= add;
                }
                else if (diff < 0 && attributes[i] > minPerAttr)
                {
                    int sub = Math.Min(-diff, attributes[i] - minPerAttr);
                    attributes[i] -= sub;
                    diff += sub;
                }
            }
        }
    }
}
