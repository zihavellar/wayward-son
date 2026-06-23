using System.Text;
using UnityEngine;
using WaywardSon.Attributes;

namespace WaywardSon.Testing
{
    /// <summary>
    /// Script de teste manual para validar o sistema de geração de atributos.
    /// <para>
    /// Anexe a um GameObject na cena e use o botão "Run Tests" no Inspector
    /// ou clique com botão direito → "Run Tests" no contexto do componente.
    /// </para>
    /// 
    /// <remarks>
    /// Validações executadas:<br/>
    /// • Todos os 12 atributos estão no intervalo [1, 5]<br/>
    /// • Total de pontos é sempre 32<br/>
    /// • Distribuição de categorias segue o padrão {10, 8, 8, 6}<br/>
    /// • Gerações com a mesma seed produzem resultados idênticos<br/>
    /// • Gerações aleatórias produzem variação suficiente
    /// </remarks>
    /// </summary>
    public class AttributeTestRunner : MonoBehaviour
    {
        // ═══════════════════════════════════════════════════════════════════
        // CONFIGURAÇÃO
        // ═══════════════════════════════════════════════════════════════════

        [Header("Test Settings")]
        [Tooltip("Número de gerações aleatórias para validar estatisticamente.")]
        [SerializeField] private int _numberOfTests = 100;

        [Tooltip("Executar testes automaticamente ao iniciar a cena.")]
        [SerializeField] private bool _runOnStart = false;

        [Tooltip("Seed específica para testes de reprodutibilidade (0 = ignorar).")]
        [SerializeField] private int _reproducibilitySeed = 42;

        [Tooltip("Número de repetições para testes de reprodutibilidade.")]
        [SerializeField] private int _reproducibilityRepeats = 10;

        // ═══════════════════════════════════════════════════════════════════
        // RESULTADOS (somente leitura no Inspector)
        // ═══════════════════════════════════════════════════════════════════

        [Header("Results (Read Only)")]
        [SerializeField] private string _lastSummary = "";
        [SerializeField] private bool _allTestsPassed = false;
        [SerializeField] private int _totalTestsRun = 0;
        [SerializeField] private int _totalTestsPassed = 0;
        [SerializeField] private int _totalTestsFailed = 0;

        // ═══════════════════════════════════════════════════════════════════
        // CONSTANTES DE VALIDAÇÃO
        // ═══════════════════════════════════════════════════════════════════

        private const int MinAttributeValue = 1;
        private const int MaxAttributeValue = 5;
        private const int ExpectedTotalPoints = 32;
        private const int AttributeCount = 12;

        /// <summary>
        /// Distribuição esperada de pontos por categoria (ordenada crescente).
        /// Social=10, Physical=8, Mental=8, Supernatural=6 → ordenado: {6, 8, 8, 10}
        /// </summary>
        private static readonly int[] ExpectedCategoryDistribution = { 6, 8, 8, 10 };

        // ═══════════════════════════════════════════════════════════════════
        // CICLO DE VIDA
        // ═══════════════════════════════════════════════════════════════════

        private void Start()
        {
            if (_runOnStart)
            {
                RunAllTests();
            }
        }

        // ═══════════════════════════════════════════════════════════════════
        // MÉTODOS DE TESTE PÚBLICOS
        // ═══════════════════════════════════════════════════════════════════

        /// <summary>
        /// Executa todos os suites de teste e exibe um relatório consolidado.
        /// Pode ser chamado pelo ContextMenu ou programaticamente.
        /// </summary>
        [ContextMenu("Run All Tests")]
        public void RunAllTests()
        {
            var report = new StringBuilder(2048);
            int totalPassed = 0;
            int totalFailed = 0;

            report.AppendLine("╔══════════════════════════════════════════════════╗");
            report.AppendLine("║     ATTRIBUTE SYSTEM - TEST REPORT              ║");
            report.AppendLine("╚══════════════════════════════════════════════════╝");
            report.AppendLine();

            // ── Suite 1: Validação de Intervalo ──────────────────────
            report.AppendLine("── Suite 1: Attribute Range Validation ──");
            int rangePassed, rangeFailed;
            RunRangeValidationTests(report, out rangePassed, out rangeFailed);
            totalPassed += rangePassed;
            totalFailed += rangeFailed;
            report.AppendLine();

            // ── Suite 2: Validação de Total ─────────────────────────
            report.AppendLine("── Suite 2: Total Points Validation ──");
            int totalPassed2, totalFailed2;
            RunTotalPointsTests(report, out totalPassed2, out totalFailed2);
            totalPassed += totalPassed2;
            totalFailed += totalFailed2;
            report.AppendLine();

            // ── Suite 3: Validação de Distribuição ──────────────────
            report.AppendLine("── Suite 3: Category Distribution Validation ──");
            int distPassed, distFailed;
            RunDistributionTests(report, out distPassed, out distFailed);
            totalPassed += distPassed;
            totalFailed += distFailed;
            report.AppendLine();

            // ── Suite 4: Testes de Reprodutibilidade ────────────────
            if (_reproducibilitySeed > 0)
            {
                report.AppendLine("── Suite 4: Reproducibility Tests ──");
                int reproPassed, reproFailed;
                RunReproducibilityTests(report, out reproPassed, out reproFailed);
                totalPassed += reproPassed;
                totalFailed += reproFailed;
                report.AppendLine();
            }

            // ── Suite 5: Variação Aleatória ─────────────────────────
            report.AppendLine("── Suite 5: Randomness Variation Tests ──");
            int varyPassed, varyFailed;
            RunVariationTests(report, out varyPassed, out varyFailed);
            totalPassed += varyPassed;
            totalFailed += varyFailed;
            report.AppendLine();

            // ── Suite 6: Testes de Edge Cases ───────────────────────
            report.AppendLine("── Suite 6: Edge Case Validation ──");
            int edgePassed, edgeFailed;
            RunEdgeCaseTests(report, out edgePassed, out edgeFailed);
            totalPassed += edgePassed;
            totalFailed += edgeFailed;
            report.AppendLine();

            // ── Resumo Final ────────────────────────────────────────
            int grandTotal = totalPassed + totalFailed;
            _totalTestsRun = grandTotal;
            _totalTestsPassed = totalPassed;
            _totalTestsFailed = totalFailed;
            _allTestsPassed = totalFailed == 0;

            report.AppendLine("══════════════════════════════════════════════════");
            report.AppendLine($"  GRAND TOTAL: {totalPassed}/{grandTotal} passed");
            report.AppendLine($"  Result: {(_allTestsPassed ? "ALL TESTS PASSED ✓" : "SOME TESTS FAILED ✗")}");
            report.AppendLine("══════════════════════════════════════════════════");

            _lastSummary = report.ToString();
            Debug.Log(_lastSummary);

            if (!_allTestsPassed)
            {
                Debug.LogWarning("[AttributeTestRunner] Some tests failed! Check the log above for details.");
            }
        }

        /// <summary>
        /// Executa apenas os testes de validação de intervalo (1-5).
        /// </summary>
        [ContextMenu("Run Range Tests Only")]
        public void RunRangeTestsOnly()
        {
            var report = new StringBuilder(1024);
            int passed, failed;

            report.AppendLine("── Range Validation Tests ──");
            RunRangeValidationTests(report, out passed, out failed);
            Debug.Log(report.ToString());
        }

        /// <summary>
        /// Executa apenas os testes de total de pontos (32).
        /// </summary>
        [ContextMenu("Run Total Points Tests Only")]
        public void RunTotalPointsTestsOnly()
        {
            var report = new StringBuilder(1024);
            int passed, failed;

            report.AppendLine("── Total Points Validation Tests ──");
            RunTotalPointsTests(report, out passed, out failed);
            Debug.Log(report.ToString());
        }

        /// <summary>
        /// Executa apenas os testes de distribuição de categorias.
        /// </summary>
        [ContextMenu("Run Distribution Tests Only")]
        public void RunDistributionTestsOnly()
        {
            var report = new StringBuilder(1024);
            int passed, failed;

            report.AppendLine("── Category Distribution Validation Tests ──");
            RunDistributionTests(report, out passed, out failed);
            Debug.Log(report.ToString());
        }

        /// <summary>
        /// Gera e exibe um único conjunto de atributos no Console para inspeção visual.
        /// </summary>
        [ContextMenu("Generate One Sample")]
        public void GenerateOneSample()
        {
            CharacterAttributes attrs = AttributeGenerator.Generate();
            Debug.Log($"Sample Character Attributes:\n{attrs.GetSummary()}");
        }

        /// <summary>
        /// Gera e exibe um conjunto de atributos com seed específica.
        /// </summary>
        [ContextMenu("Generate Sample with Configured Seed")]
        public void GenerateSampleWithSeed()
        {
            if (_reproducibilitySeed <= 0)
            {
                Debug.LogWarning("[AttributeTestRunner] Seed must be > 0. Current: " + _reproducibilitySeed);
                return;
            }

            CharacterAttributes attrs = AttributeGenerator.Generate(_reproducibilitySeed);
            Debug.Log($"Sample (seed={_reproducibilitySeed}):\n{attrs.GetSummary()}");
        }

        /// <summary>
        /// Gera múltiplas amostras e exibe estatísticas de distribuição.
        /// </summary>
        [ContextMenu("Show Statistics (1000 samples)")]
        public void ShowStatistics()
        {
            const int sampleCount = 1000;
            var stats = new int[AttributeCount]; // soma de cada atributo
            var categoryTotals = new int[4];     // soma de cada categoria
            var attrDistributions = new int[AttributeCount][]; // histograma [attrIndex][value]

            for (int a = 0; a < AttributeCount; a++)
            {
                attrDistributions[a] = new int[MaxAttributeValue + 1]; // indices 0-5, usamos 1-5
            }

            for (int i = 0; i < sampleCount; i++)
            {
                CharacterAttributes attrs = AttributeGenerator.Generate();

                // Coletar valores individuais
                for (int a = 0; a < AttributeCount; a++)
                {
                    int val = attrs.GetAttribute(a);
                    stats[a] += val;
                    if (val >= 1 && val <= 5)
                        attrDistributions[a][val]++;
                }

                // Coletar totais de categoria
                categoryTotals[0] += attrs.GetCategoryTotal(AttributeCategory.Social);
                categoryTotals[1] += attrs.GetCategoryTotal(AttributeCategory.Physical);
                categoryTotals[2] += attrs.GetCategoryTotal(AttributeCategory.Mental);
                categoryTotals[3] += attrs.GetCategoryTotal(AttributeCategory.Supernatural);
            }

            var sb = new StringBuilder(2048);
            sb.AppendLine($"══ STATISTICS ({sampleCount} samples) ══");
            sb.AppendLine();
            sb.AppendLine("── Attribute Averages ──");

            string[] attrNames = {
                "Charisma", "Manipulation", "Composure",
                "Strength", "Dexterity", "Resistance",
                "Intelligence", "Wits", "Resolution",
                "Sensibility", "Understanding", "Protection"
            };

            for (int a = 0; a < AttributeCount; a++)
            {
                double avg = (double)stats[a] / sampleCount;
                sb.AppendLine($"  {attrNames[a],-15}: avg={avg:F2}");
            }

            sb.AppendLine();
            sb.AppendLine("── Category Averages ──");
            string[] catNames = { "Social", "Physical", "Mental", "Supernatural" };
            for (int c = 0; c < 4; c++)
            {
                double avg = (double)categoryTotals[c] / sampleCount;
                sb.AppendLine($"  {catNames[c],-15}: avg={avg:F2}");
            }

            sb.AppendLine();
            sb.AppendLine("── Value Distribution (per attribute) ──");
            for (int a = 0; a < AttributeCount; a++)
            {
                sb.Append($"  {attrNames[a],-15}: ");
                for (int v = 1; v <= MaxAttributeValue; v++)
                {
                    double pct = (double)attrDistributions[a][v] / sampleCount * 100;
                    sb.Append($"[{v}]={pct,5:F1}%  ");
                }
                sb.AppendLine();
            }

            Debug.Log(sb.ToString());
        }

        // ═══════════════════════════════════════════════════════════════════
        // SUITES DE TESTE PRIVADOS
        // ═══════════════════════════════════════════════════════════════════

        /// <summary>
        /// Suite 1: Valida que todos os 12 atributos estão no intervalo [1, 5].
        /// </summary>
        private void RunRangeValidationTests(StringBuilder report, out int passed, out int failed)
        {
            passed = 0;
            failed = 0;

            for (int i = 0; i < _numberOfTests; i++)
            {
                CharacterAttributes attrs = AttributeGenerator.Generate();
                bool valid = true;

                // Validar cada atributo individualmente
                for (int a = 0; a < AttributeCount; a++)
                {
                    int value = attrs.GetAttribute(a);
                    if (value < MinAttributeValue || value > MaxAttributeValue)
                    {
                        report.AppendLine($"  ✗ Test {i}: Attribute index {a} = {value} (expected {MinAttributeValue}-{MaxAttributeValue})");
                        valid = false;
                    }
                }

                if (valid)
                    passed++;
                else
                    failed++;
            }

            report.AppendLine($"  Result: {passed}/{_numberOfTests} passed");
        }

        /// <summary>
        /// Suite 2: Valida que o total de pontos é sempre 32.
        /// </summary>
        private void RunTotalPointsTests(StringBuilder report, out int passed, out int failed)
        {
            passed = 0;
            failed = 0;

            for (int i = 0; i < _numberOfTests; i++)
            {
                CharacterAttributes attrs = AttributeGenerator.Generate();

                int total = attrs.GetCategoryTotal(AttributeCategory.Social)
                          + attrs.GetCategoryTotal(AttributeCategory.Physical)
                          + attrs.GetCategoryTotal(AttributeCategory.Mental)
                          + attrs.GetCategoryTotal(AttributeCategory.Supernatural);

                if (total == ExpectedTotalPoints)
                {
                    passed++;
                }
                else
                {
                    report.AppendLine($"  ✗ Test {i}: Total = {total}, expected {ExpectedTotalPoints}");
                    report.AppendLine($"    Detail: {attrs.GetSummary()}");
                    failed++;
                }
            }

            report.AppendLine($"  Result: {passed}/{_numberOfTests} passed");
        }

        /// <summary>
        /// Suite 3: Valida que a distribuição de categorias segue {10, 8, 8, 6}.
        /// </summary>
        private void RunDistributionTests(StringBuilder report, out int passed, out int failed)
        {
            passed = 0;
            failed = 0;

            for (int i = 0; i < _numberOfTests; i++)
            {
                CharacterAttributes attrs = AttributeGenerator.Generate();

                int social = attrs.GetCategoryTotal(AttributeCategory.Social);
                int physical = attrs.GetCategoryTotal(AttributeCategory.Physical);
                int mental = attrs.GetCategoryTotal(AttributeCategory.Mental);
                int supernatural = attrs.GetCategoryTotal(AttributeCategory.Supernatural);

                // Ordenar para comparar com a distribuição esperada
                int[] actual = { social, physical, mental, supernatural };
                System.Array.Sort(actual);

                bool valid = true;
                for (int c = 0; c < 4; c++)
                {
                    if (actual[c] != ExpectedCategoryDistribution[c])
                    {
                        valid = false;
                        break;
                    }
                }

                if (valid)
                {
                    passed++;
                }
                else
                {
                    report.AppendLine($"  ✗ Test {i}: Distribution [{social}, {physical}, {mental}, {supernatural}] " +
                                     $"≠ expected [{string.Join(", ", ExpectedCategoryDistribution)}]");
                    failed++;
                }
            }

            report.AppendLine($"  Result: {passed}/{_numberOfTests} passed");
        }

        /// <summary>
        /// Suite 4: Valida que a mesma seed sempre produz o mesmo resultado.
        /// </summary>
        private void RunReproducibilityTests(StringBuilder report, out int passed, out int failed)
        {
            passed = 0;
            failed = 0;

            for (int seed = 1; seed <= _reproducibilityRepeats; seed++)
            {
                CharacterAttributes first = AttributeGenerator.Generate(seed);
                bool allMatch = true;

                for (int repeat = 0; repeat < 5; repeat++)
                {
                    CharacterAttributes again = AttributeGenerator.Generate(seed);

                    if (!AttributesEqual(first, again))
                    {
                        report.AppendLine($"  ✗ Seed {seed}: Reproducibility failed on repeat {repeat}");
                        report.AppendLine($"    First:  {first.GetSummary()}");
                        report.AppendLine($"    Again:  {again.GetSummary()}");
                        allMatch = false;
                        break;
                    }
                }

                if (allMatch)
                    passed++;
                else
                    failed++;
            }

            report.AppendLine($"  Result: {passed}/{_reproducibilityRepeats} passed");
        }

        /// <summary>
        /// Suite 5: Valida que diferentes gerações aleatórias produzem resultados variados.
        /// </summary>
        private void RunVariationTests(StringBuilder report, out int passed, out int failed)
        {
            passed = 0;
            failed = 0;

            // Teste 5a: Pelo menos 2 valores diferentes em cada atributo
            const int variationSampleSize = 50;
            var attributeSamples = new int[AttributeCount][];
            for (int a = 0; a < AttributeCount; a++)
                attributeSamples[a] = new int[variationSampleSize];

            for (int i = 0; i < variationSampleSize; i++)
            {
                CharacterAttributes attrs = AttributeGenerator.Generate();
                for (int a = 0; a < AttributeCount; a++)
                    attributeSamples[a][i] = attrs.GetAttribute(a);
            }

            bool hasVariation = true;
            for (int a = 0; a < AttributeCount; a++)
            {
                int min = attributeSamples[a][0];
                int max = attributeSamples[a][0];
                for (int i = 1; i < variationSampleSize; i++)
                {
                    if (attributeSamples[a][i] < min) min = attributeSamples[a][i];
                    if (attributeSamples[a][i] > max) max = attributeSamples[a][i];
                }

                if (min == max)
                {
                    report.AppendLine($"  ✗ Attribute {a} has no variation (always {min})");
                    hasVariation = false;
                }
            }

            if (hasVariation)
            {
                report.AppendLine("  ✓ All attributes show variation across generations");
                passed++;
            }
            else
            {
                failed++;
            }

            // Teste 5b: Nem todas as gerações são idênticas
            CharacterAttributes gen1 = AttributeGenerator.Generate();
            CharacterAttributes gen2 = AttributeGenerator.Generate();
            bool areDifferent = !AttributesEqual(gen1, gen2);

            if (areDifferent)
            {
                report.AppendLine("  ✓ Consecutive generations produce different results");
                passed++;
            }
            else
            {
                report.AppendLine("  ✗ Two consecutive generations are identical (statistically unlikely)");
                failed++;
            }

            // Teste 5c: Pelo menos 2 distribuições de categorias diferentes em 20 gerações
            var distributions = new System.Collections.Generic.HashSet<string>();
            for (int i = 0; i < 20; i++)
            {
                CharacterAttributes attrs = AttributeGenerator.Generate();
                int[] dist = {
                    attrs.GetCategoryTotal(AttributeCategory.Social),
                    attrs.GetCategoryTotal(AttributeCategory.Physical),
                    attrs.GetCategoryTotal(AttributeCategory.Mental),
                    attrs.GetCategoryTotal(AttributeCategory.Supernatural)
                };
                System.Array.Sort(dist);
                distributions.Add(string.Join(",", dist));
            }

            if (distributions.Count > 1)
            {
                report.AppendLine($"  ✓ Found {distributions.Count} distinct category distributions");
                passed++;
            }
            else
            {
                report.AppendLine("  ✗ Only 1 unique distribution found in 20 generations");
                failed++;
            }
        }

        /// <summary>
        /// Suite 6: Validações de edge cases e invariantes adicionais.
        /// </summary>
        private void RunEdgeCaseTests(StringBuilder report, out int passed, out int failed)
        {
            passed = 0;
            failed = 0;

            // Teste 6a: Gerar com seed 1 e validar estrutura
            CharacterAttributes seed1 = AttributeGenerator.Generate(1);
            bool seed1Valid = ValidateFullAttributes(seed1, "Seed=1");
            if (seed1Valid)
            {
                report.AppendLine("  ✓ Seed=1 generates valid attributes");
                passed++;
            }
            else
            {
                failed++;
            }

            // Teste 6b: Gerar com seed grande
            CharacterAttributes seedLarge = AttributeGenerator.Generate(99999);
            bool seedLargeValid = ValidateFullAttributes(seedLarge, "Seed=99999");
            if (seedLargeValid)
            {
                report.AppendLine("  ✓ Seed=99999 generates valid attributes");
                passed++;
            }
            else
            {
                failed++;
            }

            // Teste 6c: Gerar sem seed (aleatório) e validar
            CharacterAttributes random = AttributeGenerator.Generate();
            bool randomValid = ValidateFullAttributes(random, "Random");
            if (randomValid)
            {
                report.AppendLine("  ✓ Random generation produces valid attributes");
                passed++;
            }
            else
            {
                failed++;
            }

            // Teste 6d: Validar que cada categoria tem exatamente 3 atributos
            bool categoryStructureValid = true;
            for (int c = 0; c < 4; c++)
            {
                AttributeCategory cat = (AttributeCategory)c;
                int total = seed1.GetCategoryTotal(cat);
                if (total < 3 || total > 15) // 3*1=3 min, 3*5=15 max
                {
                    report.AppendLine($"  ✗ Category {cat} total {total} is out of valid range [3, 15]");
                    categoryStructureValid = false;
                }
            }

            if (categoryStructureValid)
            {
                report.AppendLine("  ✓ All category totals are within valid range [3, 15]");
                passed++;
            }
            else
            {
                failed++;
            }

            // Teste 6e: Validar que o resumo não está vazio
            string summary = seed1.GetSummary();
            if (!string.IsNullOrEmpty(summary) && summary.Length > 50)
            {
                report.AppendLine("  ✓ GetSummary() returns non-empty, meaningful content");
                passed++;
            }
            else
            {
                report.AppendLine("  ✗ GetSummary() returned empty or too short content");
                failed++;
            }

            // Teste 6f: Validar que GetAttribute funciona para todos os índices
            bool getAttrWorks = true;
            for (int a = 0; a < AttributeCount; a++)
            {
                int val = seed1.GetAttribute(a);
                if (val < MinAttributeValue || val > MaxAttributeValue)
                {
                    report.AppendLine($"  ✗ GetAttribute({a}) returned {val}, expected {MinAttributeValue}-{MaxAttributeValue}");
                    getAttrWorks = false;
                }
            }

            if (getAttrWorks)
            {
                report.AppendLine("  ✓ GetAttribute() works correctly for all 12 indices");
                passed++;
            }
            else
            {
                failed++;
            }
        }

        // ═══════════════════════════════════════════════════════════════════
        // HELPERS
        // ═══════════════════════════════════════════════════════════════════

        /// <summary>
        /// Valida completamente um conjunto de atributos (intervalo + total + distribuição).
        /// </summary>
        private bool ValidateFullAttributes(CharacterAttributes attrs, string label)
        {
            // Validar intervalo de cada atributo
            for (int a = 0; a < AttributeCount; a++)
            {
                int value = attrs.GetAttribute(a);
                if (value < MinAttributeValue || value > MaxAttributeValue)
                {
                    Debug.LogError($"[{label}] Attribute {a} = {value}, expected {MinAttributeValue}-{MaxAttributeValue}");
                    return false;
                }
            }

            // Validar total
            int total = attrs.GetCategoryTotal(AttributeCategory.Social)
                      + attrs.GetCategoryTotal(AttributeCategory.Physical)
                      + attrs.GetCategoryTotal(AttributeCategory.Mental)
                      + attrs.GetCategoryTotal(AttributeCategory.Supernatural);

            if (total != ExpectedTotalPoints)
            {
                Debug.LogError($"[{label}] Total = {total}, expected {ExpectedTotalPoints}");
                return false;
            }

            // Validar distribuição
            int[] actual = {
                attrs.GetCategoryTotal(AttributeCategory.Social),
                attrs.GetCategoryTotal(AttributeCategory.Physical),
                attrs.GetCategoryTotal(AttributeCategory.Mental),
                attrs.GetCategoryTotal(AttributeCategory.Supernatural)
            };
            System.Array.Sort(actual);

            for (int c = 0; c < 4; c++)
            {
                if (actual[c] != ExpectedCategoryDistribution[c])
                {
                    Debug.LogError($"[{label}] Distribution mismatch at index {c}: {actual[c]} ≠ {ExpectedCategoryDistribution[c]}");
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Verifica se dois conjuntos de atributos são idênticos.
        /// </summary>
        private bool AttributesEqual(CharacterAttributes a, CharacterAttributes b)
        {
            return a.Charisma == b.Charisma
                && a.Manipulation == b.Manipulation
                && a.Composure == b.Composure
                && a.Strength == b.Strength
                && a.Dexterity == b.Dexterity
                && a.Resistance == b.Resistance
                && a.Intelligence == b.Intelligence
                && a.Wits == b.Wits
                && a.Resolution == b.Resolution
                && a.Sensibility == b.Sensibility
                && a.Understanding == b.Understanding
                && a.Protection == b.Protection;
        }
    }
}
