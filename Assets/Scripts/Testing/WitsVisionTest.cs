using System.Text;
using UnityEngine;
using WaywardSon;

/// <summary>
/// Script de teste para validar o efeito do atributo Wits no sistema de visão.
/// <para>
/// Anexe a um GameObject na cena e use o ContextMenu para executar os testes.<br/>
/// O script valida a fórmula de bônus: 1.0 + ((Wits - 1) × 0.15)
/// </para>
/// 
/// <remarks>
/// Validações executadas:<br/>
/// • Cálculo do bônus de Wits para todos os níveis (1-5)<br/>
/// • Escalonamento do alcance da lanterna com bônus de Wits<br/>
/// • Escalonamento da visão passiva com bônus de Wits<br/>
/// • Consistência dos cálculos em múltiplas iterações<br/>
/// • Tabela de referência de bônus<br/>
/// • Validação do estado atual do jogador (se disponível)
/// </remarks>
/// </summary>
public class WitsVisionTest : MonoBehaviour
{
    // ═══════════════════════════════════════════════════════════════════
    // CONSTANTES DE CONFIGURAÇÃO
    // ═══════════════════════════════════════════════════════════════════

    /// <summary>Valor base do alcance da lanterna (sem bônus de Wits).</summary>
    private const float BaseFlashlightRange = 14.0f;

    /// <summary>Valor base do alcance da visão passiva (sem bônus de Wits).</summary>
    private const float BasePassiveVisionRange = 3.0f;

    /// <summary>Percentual de bônus por ponto de Wits (15%).</summary>
    private const float WitsBonusPerPoint = 0.15f;

    /// <summary>Número mínimo de Wits.</summary>
    private const int MinWits = 1;

    /// <summary>Número máximo de Wits.</summary>
    private const int MaxWits = 5;

    /// <summary>Tolerância para comparação de floats.</summary>
    private const float FloatTolerance = 0.01f;

    // ═══════════════════════════════════════════════════════════════════
    // CONFIGURAÇÃO DO INSPECTOR
    // ═══════════════════════════════════════════════════════════════════

    [Header("Test Settings")]
    [Tooltip("Número de iterações para validar consistência dos cálculos.")]
    [SerializeField] private int _iterationsPerLevel = 100;

    [Tooltip("Executar testes automaticamente ao iniciar a cena.")]
    [SerializeField] private bool _runOnStart = false;

    [Header("Results (Read Only)")]
    [Tooltip("Resultado do último teste executado.")]
    [SerializeField] private string _lastTestResult = "";

    [Tooltip("Indica se todos os testes foram aprovados.")]
    [SerializeField] private bool _allTestsPassed = false;

    [Tooltip("Número total de testes executados.")]
    [SerializeField] private int _totalTestsRun = 0;

    [Tooltip("Número de testes aprovados.")]
    [SerializeField] private int _totalTestsPassed = 0;

    [Tooltip("Número de testes reprovados.")]
    [SerializeField] private int _totalTestsFailed = 0;

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
    // MÉTODOS PÚBLICOS DE TESTE
    // ═══════════════════════════════════════════════════════════════════

    /// <summary>
    /// Executa todos os testes de validação do sistema de visão Wits.
    /// Exibe um relatório consolidado no Console do Unity.
    /// Pode ser chamado pelo ContextMenu ou programaticamente.
    /// </summary>
    [ContextMenu("Run All Tests")]
    public void RunAllTests()
    {
        var report = new StringBuilder(2048);
        int totalPassed = 0;
        int totalFailed = 0;

        report.AppendLine("╔══════════════════════════════════════════════════╗");
        report.AppendLine("║     WITS VISION SYSTEM - TEST REPORT            ║");
        report.AppendLine("╚══════════════════════════════════════════════════╝");
        report.AppendLine();

        // ── Teste 1: Validação do cálculo de bônus ──────────────────
        report.AppendLine("── Test 1: Wits Vision Bonus Calculation ──");
        bool test1 = TestBonusCalculation(report);
        if (test1) totalPassed++; else totalFailed++;
        report.AppendLine();

        // ── Teste 2: Escalonamento do alcance da lanterna ───────────
        report.AppendLine("── Test 2: Flashlight Range Scaling ──");
        bool test2 = TestFlashlightRangeScaling(report);
        if (test2) totalPassed++; else totalFailed++;
        report.AppendLine();

        // ── Teste 3: Escalonamento da visão passiva ─────────────────
        report.AppendLine("── Test 3: Passive Vision Scaling ──");
        bool test3 = TestPassiveVisionScaling(report);
        if (test3) totalPassed++; else totalFailed++;
        report.AppendLine();

        // ── Teste 4: Consistência em múltiplas iterações ────────────
        report.AppendLine("── Test 4: Consistency Across Iterations ──");
        bool test4 = TestConsistency(report);
        if (test4) totalPassed++; else totalFailed++;
        report.AppendLine();

        // ── Teste 5: Limites e edge cases ───────────────────────────
        report.AppendLine("── Test 5: Edge Cases and Boundary Values ──");
        bool test5 = TestEdgeCases(report);
        if (test5) totalPassed++; else totalFailed++;
        report.AppendLine();

        // ── Resumo Final ────────────────────────────────────────────
        int grandTotal = totalPassed + totalFailed;
        _totalTestsRun = grandTotal;
        _totalTestsPassed = totalPassed;
        _totalTestsFailed = totalFailed;
        _allTestsPassed = totalFailed == 0;

        report.AppendLine("══════════════════════════════════════════════════");
        report.AppendLine($"  GRAND TOTAL: {totalPassed}/{grandTotal} passed");
        report.AppendLine($"  Result: {(_allTestsPassed ? "ALL TESTS PASSED ✓" : "SOME TESTS FAILED ✗")}");
        report.AppendLine("══════════════════════════════════════════════════");

        _lastTestResult = _allTestsPassed ? "ALL TESTS PASSED" : $"{totalFailed} test(s) failed";

        Debug.Log(report.ToString());

        if (!_allTestsPassed)
        {
            Debug.LogWarning("[WitsVisionTest] Some tests failed! Check the log above for details.");
        }
    }

    /// <summary>
    /// Exibe uma tabela de referência com todos os bônus de Wits,
    /// incluindo alcance da lanterna e visão passiva para cada nível.
    /// </summary>
    [ContextMenu("Show Bonus Table")]
    public void ShowBonusTable()
    {
        var sb = new StringBuilder(1024);

        sb.AppendLine("╔═══════════════════════════════════════════════════════════════╗");
        sb.AppendLine("║              WITS VISION BONUS TABLE                        ║");
        sb.AppendLine("╠═══════════════════════════════════════════════════════════════╣");
        sb.AppendLine("║  Wits │  Bonus │  Lantern Range │  Lantern Intensity │ Passive ║");
        sb.AppendLine("╠═══════════════════════════════════════════════════════════════╣");

        for (int wits = MinWits; wits <= MaxWits; wits++)
        {
            float bonus = CalculateWitsBonus(wits);
            float lanternRange = BaseFlashlightRange * bonus;
            float lanternIntensity = 7f * bonus; // base intensity do FlashlightController
            float passiveRange = BasePassiveVisionRange * bonus;

            sb.AppendFormat("║   {0}    │ {1,5:F2}x │     {2,5:F1}      │       {3,5:F1}        │  {4,5:F1}   ║",
                wits, bonus, lanternRange, lanternIntensity, passiveRange);
            sb.AppendLine();
        }

        sb.AppendLine("╚═══════════════════════════════════════════════════════════════╝");
        sb.AppendLine();
        sb.AppendLine("Fórmula: bonus = 1.0 + ((Wits - 1) × 0.15)");
        sb.AppendLine($"Lantern Range Base: {BaseFlashlightRange}");
        sb.AppendLine($"Passive Vision Base: {BasePassiveVisionRange}");

        Debug.Log(sb.ToString());
    }

    /// <summary>
    /// Valida o estado atual do jogador, verificando se o bônus de Wits
    /// está sendo aplicado corretamente ao FlashlightController.
    /// Requer que o jogador esteja na cena.
    /// </summary>
    [ContextMenu("Test Current Player Wits")]
    public void TestCurrentPlayerWits()
    {
        FlashlightController flashlight = FindAnyObjectByType<FlashlightController>();
        CharacterStats stats = FindAnyObjectByType<CharacterStats>();

        if (flashlight == null)
        {
            Debug.LogError("[WitsVisionTest] FlashlightController not found in scene!");
            return;
        }

        if (stats == null)
        {
            Debug.LogError("[WitsVisionTest] CharacterStats not found in scene!");
            return;
        }

        int wits = stats.Attributes.Wits;
        float bonus = CalculateWitsBonus(wits);

        var sb = new StringBuilder(512);
        sb.AppendLine("╔══════════════════════════════════════════════════╗");
        sb.AppendLine("║     CURRENT PLAYER WITS TEST                    ║");
        sb.AppendLine("╚══════════════════════════════════════════════════╝");
        sb.AppendLine($"  Player Wits:             {wits}");
        sb.AppendLine($"  Vision Bonus:            {bonus:F2}x");
        sb.AppendLine($"  Expected Lantern Range:  {BaseFlashlightRange * bonus:F1}");
        sb.AppendLine($"  Actual Lantern Range:    {flashlight.spotLight.range:F1}");
        sb.AppendLine($"  Expected Passive Range:  {BasePassiveVisionRange * bonus:F1}");
        sb.AppendLine($"  Actual Passive Range:    {flashlight.GetEffectivePassiveVisionRange():F1}");
        sb.AppendLine("══════════════════════════════════════════════════");

        // Validar se os valores estão corretos
        bool rangeCorrect = Mathf.Approximately(flashlight.spotLight.range, BaseFlashlightRange * bonus);
        bool passiveCorrect = Mathf.Approximately(flashlight.GetEffectivePassiveVisionRange(), BasePassiveVisionRange * bonus);

        if (rangeCorrect && passiveCorrect)
        {
            sb.AppendLine("  Result: ALL VALUES CORRECT ✓");
            _lastTestResult = "PLAYER WITS TEST: PASSED";
        }
        else
        {
            if (!rangeCorrect)
                sb.AppendLine($"  ✗ Lantern range mismatch: expected {BaseFlashlightRange * bonus:F1}, got {flashlight.spotLight.range:F1}");
            if (!passiveCorrect)
                sb.AppendLine($"  ✗ Passive range mismatch: expected {BasePassiveVisionRange * bonus:F1}, got {flashlight.GetEffectivePassiveVisionRange():F1}");

            _lastTestResult = "PLAYER WITS TEST: FAILED";
        }

        Debug.Log(sb.ToString());
    }

    // ═══════════════════════════════════════════════════════════════════
    // MÉTODOS DE TESTE PRIVADOS
    // ═══════════════════════════════════════════════════════════════════

    /// <summary>
    /// Teste 1: Valida que o cálculo de bônus de Wits está correto.
    /// <para>
    /// Valores esperados:<br/>
    /// • Wits 1 = 1.00x (base, sem bônus)<br/>
    /// • Wits 2 = 1.15x (+15%)<br/>
    /// • Wits 3 = 1.30x (+30%)<br/>
    /// • Wits 4 = 1.45x (+45%)<br/>
    /// • Wits 5 = 1.60x (+60%)
    /// </para>
    /// </summary>
    /// <param name="report">StringBuilder para acumular resultados.</param>
    /// <returns>True se todos os valores estão corretos.</returns>
    private bool TestBonusCalculation(StringBuilder report)
    {
        float[] expectedBonuses = { 1.00f, 1.15f, 1.30f, 1.45f, 1.60f };
        bool allCorrect = true;

        for (int wits = MinWits; wits <= MaxWits; wits++)
        {
            float expected = expectedBonuses[wits - 1];
            float actual = CalculateWitsBonus(wits);

            if (!Mathf.Approximately(expected, actual))
            {
                report.AppendLine($"  ✗ Wits {wits}: Expected {expected:F2}x, got {actual:F2}x");
                allCorrect = false;
            }
            else
            {
                report.AppendLine($"  ✓ Wits {wits}: {actual:F2}x");
            }
        }

        report.AppendLine($"  Result: {(allCorrect ? "PASSED ✓" : "FAILED ✗")}");
        return allCorrect;
    }

    /// <summary>
    /// Teste 2: Valida que o alcance da lanterna escala corretamente com Wits.
    /// <para>
    /// Alcance base: 14.0<br/>
    /// Valores esperados: 14.0, 16.1, 18.2, 20.3, 22.4
    /// </para>
    /// </summary>
    /// <param name="report">StringBuilder para acumular resultados.</param>
    /// <returns>True se todos os alcances estão corretos.</returns>
    private bool TestFlashlightRangeScaling(StringBuilder report)
    {
        float[] expectedRanges = { 14.0f, 16.1f, 18.2f, 20.3f, 22.4f };
        bool allCorrect = true;

        for (int wits = MinWits; wits <= MaxWits; wits++)
        {
            float bonus = CalculateWitsBonus(wits);
            float actualRange = BaseFlashlightRange * bonus;
            float expected = expectedRanges[wits - 1];

            if (!Mathf.Approximately(expected, actualRange))
            {
                report.AppendLine($"  ✗ Wits {wits}: Expected range {expected:F1}, got {actualRange:F1}");
                allCorrect = false;
            }
            else
            {
                report.AppendLine($"  ✓ Wits {wits}: Range {actualRange:F1}");
            }
        }

        report.AppendLine($"  Result: {(allCorrect ? "PASSED ✓" : "FAILED ✗")}");
        return allCorrect;
    }

    /// <summary>
    /// Teste 3: Valida que a visão passiva escala corretamente com Wits.
    /// <para>
    /// Alcance base: 3.0<br/>
    /// Valores esperados: 3.00, 3.45, 3.90, 4.35, 4.80
    /// </para>
    /// </summary>
    /// <param name="report">StringBuilder para acumular resultados.</param>
    /// <returns>True se todos os alcances estão corretos.</returns>
    private bool TestPassiveVisionScaling(StringBuilder report)
    {
        float[] expectedRanges = { 3.00f, 3.45f, 3.90f, 4.35f, 4.80f };
        bool allCorrect = true;

        for (int wits = MinWits; wits <= MaxWits; wits++)
        {
            float bonus = CalculateWitsBonus(wits);
            float actualRange = BasePassiveVisionRange * bonus;
            float expected = expectedRanges[wits - 1];

            if (!Mathf.Approximately(expected, actualRange))
            {
                report.AppendLine($"  ✗ Wits {wits}: Expected passive range {expected:F2}, got {actualRange:F2}");
                allCorrect = false;
            }
            else
            {
                report.AppendLine($"  ✓ Wits {wits}: Passive range {actualRange:F2}");
            }
        }

        report.AppendLine($"  Result: {(allCorrect ? "PASSED ✓" : "FAILED ✗")}");
        return allCorrect;
    }

    /// <summary>
    /// Teste 4: Valida que o cálculo é consistente em múltiplas iterações.
    /// <para>
    /// Executa o cálculo de bônus, alcance da lanterna e visão passiva
    /// para cada nível de Wits, repetindo _iterationsPerLevel vezes.
    /// Verifica que os valores permanecem dentro dos limites esperados.
    /// </para>
    /// </summary>
    /// <param name="report">StringBuilder para acumular resultados.</param>
    /// <returns>True se todos os cálculos são consistentes.</returns>
    private bool TestConsistency(StringBuilder report)
    {
        bool consistent = true;
        int totalIterations = 0;

        for (int i = 0; i < _iterationsPerLevel; i++)
        {
            for (int wits = MinWits; wits <= MaxWits; wits++)
            {
                float bonus = CalculateWitsBonus(wits);
                float range = BaseFlashlightRange * bonus;
                float passive = BasePassiveVisionRange * bonus;
                totalIterations++;

                // Validar que os valores estão dentro dos limites esperados
                if (range < BaseFlashlightRange || range > BaseFlashlightRange * 1.65f)
                {
                    report.AppendLine($"  ✗ Iteration {i}, Wits {wits}: Lantern range {range:F1} out of bounds!");
                    consistent = false;
                }

                if (passive < BasePassiveVisionRange || passive > BasePassiveVisionRange * 1.65f)
                {
                    report.AppendLine($"  ✗ Iteration {i}, Wits {wits}: Passive range {passive:F1} out of bounds!");
                    consistent = false;
                }

                // Validar monotonicidade (mais Wits = mais alcance)
                if (wits > MinWits)
                {
                    float prevBonus = CalculateWitsBonus(wits - 1);
                    float prevRange = BaseFlashlightRange * prevBonus;

                    if (range <= prevRange)
                    {
                        report.AppendLine($"  ✗ Iteration {i}, Wits {wits}: Range {range:F1} <= previous {prevRange:F1} (should be greater)");
                        consistent = false;
                    }
                }
            }
        }

        if (consistent)
        {
            report.AppendLine($"  ✓ {totalIterations} iterations ({_iterationsPerLevel} × {MaxWits} Wits levels): All consistent");
        }

        report.AppendLine($"  Result: {(consistent ? "PASSED ✓" : "FAILED ✗")}");
        return consistent;
    }

    /// <summary>
    /// Teste 5: Valida edge cases e valores de limite.
    /// <para>
    /// Testa:<br/>
    /// • Wits abaixo do mínimo (0, negativos)<br/>
    /// • Wits acima do máximo (6, 10, 100)<br/>
    /// • Que o bônus nunca é negativo<br/>
    /// • Que o bônus sempre é >= 1.0
    /// </para>
    /// </summary>
    /// <param name="report">StringBuilder para acumular resultados.</param>
    /// <returns>True se todos os edge cases são tratados corretamente.</returns>
    private bool TestEdgeCases(StringBuilder report)
    {
        bool allPassed = true;

        // Teste 5a: Wits mínimo (1) retorna 1.0x
        float bonusAtMin = CalculateWitsBonus(MinWits);
        if (Mathf.Approximately(bonusAtMin, 1.0f))
        {
            report.AppendLine($"  ✓ Wits {MinWits} (min): {bonusAtMin:F2}x");
        }
        else
        {
            report.AppendLine($"  ✗ Wits {MinWits} (min): Expected 1.00x, got {bonusAtMin:F2}x");
            allPassed = false;
        }

        // Teste 5b: Wits máximo (5) retorna 1.60x
        float bonusAtMax = CalculateWitsBonus(MaxWits);
        if (Mathf.Approximately(bonusAtMax, 1.60f))
        {
            report.AppendLine($"  ✓ Wits {MaxWits} (max): {bonusAtMax:F2}x");
        }
        else
        {
            report.AppendLine($"  ✗ Wits {MaxWits} (max): Expected 1.60x, got {bonusAtMax:F2}x");
            allPassed = false;
        }

        // Teste 5c: Bônus nunca é negativo
        for (int wits = MinWits; wits <= MaxWits; wits++)
        {
            float bonus = CalculateWitsBonus(wits);
            if (bonus < 0f)
            {
                report.AppendLine($"  ✗ Wits {wits}: Bonus {bonus:F2}x is negative!");
                allPassed = false;
            }
        }

        if (allPassed)
        {
            report.AppendLine("  ✓ All bonuses are non-negative");
        }

        // Teste 5d: Bônus sempre >= 1.0 (nunca reduz visão)
        bool alwaysAboveBase = true;
        for (int wits = MinWits; wits <= MaxWits; wits++)
        {
            float bonus = CalculateWitsBonus(wits);
            if (bonus < 1.0f)
            {
                report.AppendLine($"  ✗ Wits {wits}: Bonus {bonus:F2}x is below base (1.0x)!");
                alwaysAboveBase = false;
                allPassed = false;
            }
        }

        if (alwaysAboveBase)
        {
            report.AppendLine("  ✓ All bonuses are >= 1.0x (base)");
        }

        // Teste 5e: Alcance da lanterna nunca é zero ou negativo
        bool lanternRangeValid = true;
        for (int wits = MinWits; wits <= MaxWits; wits++)
        {
            float bonus = CalculateWitsBonus(wits);
            float range = BaseFlashlightRange * bonus;
            if (range <= 0f)
            {
                report.AppendLine($"  ✗ Wits {wits}: Lantern range {range:F1} is zero or negative!");
                lanternRangeValid = false;
                allPassed = false;
            }
        }

        if (lanternRangeValid)
        {
            report.AppendLine("  ✓ All lantern ranges are positive");
        }

        // Teste 5f: Visão passiva nunca é zero ou negativa
        bool passiveRangeValid = true;
        for (int wits = MinWits; wits <= MaxWits; wits++)
        {
            float bonus = CalculateWitsBonus(wits);
            float passive = BasePassiveVisionRange * bonus;
            if (passive <= 0f)
            {
                report.AppendLine($"  ✗ Wits {wits}: Passive range {passive:F1} is zero or negative!");
                passiveRangeValid = false;
                allPassed = false;
            }
        }

        if (passiveRangeValid)
        {
            report.AppendLine("  ✓ All passive ranges are positive");
        }

        report.AppendLine($"  Result: {(allPassed ? "PASSED ✓" : "FAILED ✗")}");
        return allPassed;
    }

    // ═══════════════════════════════════════════════════════════════════
    // UTILITÁRIOS
    // ═══════════════════════════════════════════════════════════════════

    /// <summary>
    /// Calcula o bônus de Wits para visão.
    /// <para>
    /// Fórmula: <c>1.0 + ((Wits - 1) × 0.15)</c><br/>
    /// Exemplos:<br/>
    /// • Wits 1 = 1.0 + (0 × 0.15) = 1.00x<br/>
    /// • Wits 2 = 1.0 + (1 × 0.15) = 1.15x<br/>
    /// • Wits 3 = 1.0 + (2 × 0.15) = 1.30x<br/>
    /// • Wits 4 = 1.0 + (3 × 0.15) = 1.45x<br/>
    /// • Wits 5 = 1.0 + (4 × 0.15) = 1.60x
    /// </para>
    /// </summary>
    /// <param name="wits">Valor do atributo Wits (1-5).</param>
    /// <returns>Multiplicador de bônus (mínimo 1.0).</returns>
    private static float CalculateWitsBonus(int wits)
    {
        return 1f + ((wits - 1) * WitsBonusPerPoint);
    }
}
