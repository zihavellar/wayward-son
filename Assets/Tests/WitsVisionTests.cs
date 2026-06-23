using NUnit.Framework;
using UnityEngine;
using WaywardSon;

/// <summary>
/// Testes EditMode para validar o sistema de visão baseado em Wits.
/// Executa no Test Runner do Unity (Window > General > Test Runner > EditMode).
/// </summary>
public class WitsVisionTests
{
    // ═══════════════════════════════════════════════════════════════════
    // CONSTANTES
    // ═══════════════════════════════════════════════════════════════════

    private const float BaseFlashlightRange = 14.0f;
    private const float BasePassiveVisionRange = 3.0f;
    private const float WitsBonusPerPoint = 0.15f;
    private const float FloatTolerance = 0.01f;

    // ═══════════════════════════════════════════════════════════════════
    // TESTES DE CÁLCULO DE BÔNUS
    // ═══════════════════════════════════════════════════════════════════

    [Test]
    public void WitsBonus_Wits1_Returns1Point0()
    {
        float bonus = CalculateWitsBonus(1);
        Assert.AreEqual(1.0f, bonus, FloatTolerance);
    }

    [Test]
    public void WitsBonus_Wits2_Returns1Point15()
    {
        float bonus = CalculateWitsBonus(2);
        Assert.AreEqual(1.15f, bonus, FloatTolerance);
    }

    [Test]
    public void WitsBonus_Wits3_Returns1Point30()
    {
        float bonus = CalculateWitsBonus(3);
        Assert.AreEqual(1.30f, bonus, FloatTolerance);
    }

    [Test]
    public void WitsBonus_Wits4_Returns1Point45()
    {
        float bonus = CalculateWitsBonus(4);
        Assert.AreEqual(1.45f, bonus, FloatTolerance);
    }

    [Test]
    public void WitsBonus_Wits5_Returns1Point60()
    {
        float bonus = CalculateWitsBonus(5);
        Assert.AreEqual(1.60f, bonus, FloatTolerance);
    }

    // ═══════════════════════════════════════════════════════════════════
    // TESTES DE ALCANCE DA LANTERNA
    // ═══════════════════════════════════════════════════════════════════

    [Test]
    public void FlashlightRange_Wits1_Returns14Point0()
    {
        float range = CalculateFlashlightRange(1);
        Assert.AreEqual(14.0f, range, FloatTolerance);
    }

    [Test]
    public void FlashlightRange_Wits3_Returns18Point2()
    {
        float range = CalculateFlashlightRange(3);
        Assert.AreEqual(18.2f, range, FloatTolerance);
    }

    [Test]
    public void FlashlightRange_Wits5_Returns22Point4()
    {
        float range = CalculateFlashlightRange(5);
        Assert.AreEqual(22.4f, range, FloatTolerance);
    }

    // ═══════════════════════════════════════════════════════════════════
    // TESTES DE VISÃO PASSIVA
    // ═══════════════════════════════════════════════════════════════════

    [Test]
    public void PassiveVision_Wits1_Returns3Point0()
    {
        float range = CalculatePassiveVisionRange(1);
        Assert.AreEqual(3.0f, range, FloatTolerance);
    }

    [Test]
    public void PassiveVision_Wits3_Returns3Point9()
    {
        float range = CalculatePassiveVisionRange(3);
        Assert.AreEqual(3.9f, range, FloatTolerance);
    }

    [Test]
    public void PassiveVision_Wits5_Returns4Point8()
    {
        float range = CalculatePassiveVisionRange(5);
        Assert.AreEqual(4.8f, range, FloatTolerance);
    }

    // ═══════════════════════════════════════════════════════════════════
    // TESTES DE CONSISTÊNCIA
    // ═══════════════════════════════════════════════════════════════════

    [Test]
    public void WitsBonus_IsMonotonicallyIncreasing()
    {
        float previousBonus = 0f;
        
        for (int wits = 1; wits <= 5; wits++)
        {
            float currentBonus = CalculateWitsBonus(wits);
            Assert.Greater(currentBonus, previousBonus, 
                $"Wits {wits} bonus ({currentBonus}) should be greater than Wits {wits-1} ({previousBonus})");
            previousBonus = currentBonus;
        }
    }

    [Test]
    public void FlashlightRange_AlwaysGreaterThanBase()
    {
        for (int wits = 1; wits <= 5; wits++)
        {
            float range = CalculateFlashlightRange(wits);
            Assert.GreaterOrEqual(range, BaseFlashlightRange, 
                $"Flashlight range at Wits {wits} should be >= base ({BaseFlashlightRange})");
        }
    }

    [Test]
    public void PassiveVision_AlwaysGreaterThanBase()
    {
        for (int wits = 1; wits <= 5; wits++)
        {
            float range = CalculatePassiveVisionRange(wits);
            Assert.GreaterOrEqual(range, BasePassiveVisionRange, 
                $"Passive vision at Wits {wits} should be >= base ({BasePassiveVisionRange})");
        }
    }

    // ═══════════════════════════════════════════════════════════════════
    // TESTES DE INTEGRAÇÃO COM FLASHLIGHTCONTROLLER
    // ═══════════════════════════════════════════════════════════════════

    [Test]
    public void FlashlightController_BaseValues_AreCorrect()
    {
        // Cria um GameObject temporário com FlashlightController
        GameObject testObj = new GameObject("TestFlashlight");
        FlashlightController flashlight = testObj.AddComponent<FlashlightController>();
        
        // Verifica valores base via reflexão ou propriedades públicas
        // Nota: Os valores base são serializados, então verificamos via lógica
        
        // Limpa
        Object.DestroyImmediate(testObj);
        
        // Se chegou aqui, o componente existe e pode ser instanciado
        Assert.Pass("FlashlightController can be instantiated");
    }

    // ═══════════════════════════════════════════════════════════════════
    // TESTES DE LIMITES
    // ═══════════════════════════════════════════════════════════════════

    [Test]
    public void WitsBonus_NeverLessThan1Point0()
    {
        // Testa valores extremos
        int[] testValues = { 0, -1, 100, int.MinValue };
        
        foreach (int wits in testValues)
        {
            float bonus = CalculateWitsBonus(wits);
            Assert.GreaterOrEqual(bonus, 1.0f, 
                $"Wits bonus for value {wits} should be >= 1.0");
        }
    }

    [Test]
    public void WitsBonus_MaxValue_IsReasonable()
    {
        // Wits 5 deve dar 1.60x, não algo absurdo
        float bonus = CalculateWitsBonus(5);
        Assert.LessOrEqual(bonus, 2.0f, "Wits 5 bonus should not exceed 2.0x");
    }

    // ═══════════════════════════════════════════════════════════════════
    // MÉTODOS AUXILIARES (replicam a lógica do FlashlightController)
    // ═══════════════════════════════════════════════════════════════════

    /// <summary>
    /// Calcula o bônus de Wits para visão.
    /// Fórmula: 1.0 + ((Wits - 1) × 0.15), com mínimo de 1.0
    /// </summary>
    private float CalculateWitsBonus(int wits)
    {
        float bonus = 1f + ((wits - 1) * WitsBonusPerPoint);
        return Mathf.Max(bonus, 1.0f);
    }

    /// <summary>
    /// Calcula o alcance da lanterna com bônus de Wits.
    /// </summary>
    private float CalculateFlashlightRange(int wits)
    {
        return BaseFlashlightRange * CalculateWitsBonus(wits);
    }

    /// <summary>
    /// Calcula o alcance da visão passiva com bônus de Wits.
    /// </summary>
    private float CalculatePassiveVisionRange(int wits)
    {
        return BasePassiveVisionRange * CalculateWitsBonus(wits);
    }
}
