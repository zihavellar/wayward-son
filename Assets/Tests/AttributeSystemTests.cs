using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using WaywardSon.Attributes;

namespace WaywardSon.Tests
{
    /// <summary>
    /// Suite de testes automatizados para o sistema de atributos de Wayward Son.
    ///
    /// Cobertura:
    ///   1. Geração de atributos (ranges, totais, categorias)
    ///   2. Distribuição de pontos entre categorias
    ///   3. Consistência com seeds
    ///   4. ScriptableObject (AttributeSystemSO)
    /// </summary>
    [TestFixture]
    public class AttributeSystemTests
    {
        // ── Constantes auxiliares ────────────────────────────────────────

        /// <summary>Número de iterações em testes estatísticos.</summary>
        private const int Iterations = 200;

        /// <summary>Total esperado de pontos: 12 base + 20 extras = 32.</summary>
        private const int ExpectedTotalPoints = 32;

        /// <summary>Valor mínimo permitido para qualquer atributo.</summary>
        private const int MinAttributeValue = 1;

        /// <summary>Valor máximo permitido para qualquer atributo.</summary>
        private const int MaxAttributeValue = 5;

        /// <summary>Quantidade total de atributos.</summary>
        private const int TotalAttributes = 12;

        /// <summary>Quantidade de categorias.</summary>
        private const int CategoryCount = 4;

        /// <summary>Atributos por categoria.</summary>
        private const int AttrsPerCategory = 3;

        // ══════════════════════════════════════════════════════════════════
        // 1. TESTES DE GERAÇÃO
        // ══════════════════════════════════════════════════════════════════

        /// <summary>
        /// Valida que TODOS os 12 atributos gerados estão dentro do range válido [1, 5].
        /// Executa múltiplas iterações com seeds diferentes para cobrir aleatoriedade.
        /// </summary>
        [Test]
        public void Generate_AttributesAreWithinValidRange_Tests()
        {
            for (int i = 0; i < Iterations; i++)
            {
                CharacterAttributes attrs = AttributeGenerator.Generate(i);

                for (int attrIndex = 0; attrIndex < TotalAttributes; attrIndex++)
                {
                    int value = attrs.GetAttribute(attrIndex);
                    Assert.IsTrue(
                        value >= MinAttributeValue && value <= MaxAttributeValue,
                        $"Atributo índice {attrIndex} = {value} está fora do range [1, 5]. " +
                        $"(seed={i})"
                    );
                }
            }
        }

        /// <summary>
        /// Valida que a soma de todos os 12 atributos é sempre exatamente 32.
        /// Regra: 12 base (1×12) + 20 extras (7+5+5+3) = 32.
        /// </summary>
        [Test]
        public void Generate_TotalPointsEquals32_Tests()
        {
            for (int i = 0; i < Iterations; i++)
            {
                CharacterAttributes attrs = AttributeGenerator.Generate(i);

                int total = attrs.GetCategoryTotal(AttributeCategory.Social)
                          + attrs.GetCategoryTotal(AttributeCategory.Physical)
                          + attrs.GetCategoryTotal(AttributeCategory.Mental)
                          + attrs.GetCategoryTotal(AttributeCategory.Supernatural);

                Assert.AreEqual(
                    ExpectedTotalPoints, total,
                    $"Total de pontos deveria ser {ExpectedTotalPoints}, mas foi {total}. (seed={i})"
                );
            }
        }

        /// <summary>
        /// Valida que cada categoria possui pelo menos 3 pontos (o valor base: 1×3).
        /// Na prática, com bônus, o mínimo é 6, mas garantimos o piso absoluto.
        /// </summary>
        [Test]
        public void Generate_AllCategoriesHavePoints_Tests()
        {
            AttributeCategory[] categories = {
                AttributeCategory.Social,
                AttributeCategory.Physical,
                AttributeCategory.Mental,
                AttributeCategory.Supernatural
            };

            for (int i = 0; i < Iterations; i++)
            {
                CharacterAttributes attrs = AttributeGenerator.Generate(i);

                foreach (var category in categories)
                {
                    int total = attrs.GetCategoryTotal(category);
                    Assert.IsTrue(
                        total >= AttrsPerCategory,
                        $"Categoria {category} tem apenas {total} pontos (mínimo esperado: {AttrsPerCategory}). " +
                        $"(seed={i})"
                    );
                }
            }
        }

        // ══════════════════════════════════════════════════════════════════
        // 2. TESTES DE DISTRIBUIÇÃO
        // ══════════════════════════════════════════════════════════════════

        /// <summary>
        /// Valida que os totais das 4 categorias são {10, 8, 8, 6} em alguma ordem.
        /// Equivale aos bônus {7, 5, 5, 3} somados ao base (3 por categoria).
        /// </summary>
        [Test]
        public void Generate_CategoryDistributionIs7_5_5_3_Tests()
        {
            // Totais esperados: 3+7=10, 3+5=8, 3+5=8, 3+3=6
            int[] expectedTotals = { 10, 8, 8, 6 };

            AttributeCategory[] categories = {
                AttributeCategory.Social,
                AttributeCategory.Physical,
                AttributeCategory.Mental,
                AttributeCategory.Supernatural
            };

            for (int i = 0; i < Iterations; i++)
            {
                CharacterAttributes attrs = AttributeGenerator.Generate(i);

                // Coleta os totais das categorias
                int[] actualTotals = new int[CategoryCount];
                for (int c = 0; c < CategoryCount; c++)
                {
                    actualTotals[c] = attrs.GetCategoryTotal(categories[c]);
                }

                // Ordena ambos para comparação
                System.Array.Sort(actualTotals);
                int[] sortedExpected = (int[])expectedTotals.Clone();
                System.Array.Sort(sortedExpected);

                CollectionAssert.AreEqual(
                    sortedExpected, actualTotals,
                    $"Distribuição de categorias incorreta. " +
                    $"Esperado (ordenado): [{string.Join(", ", sortedExpected)}], " +
                    $"Atual (ordenado): [{string.Join(", ", actualTotals)}]. (seed={i})"
                );
            }
        }

        /// <summary>
        /// Valida que NENHUMA categoria possui mais de 10 pontos.
        /// O máximo é a categoria com bônus 7: 3 (base) + 7 = 10.
        /// </summary>
        [Test]
        public void Generate_NoCategoryExceeds10_Tests()
        {
            AttributeCategory[] categories = {
                AttributeCategory.Social,
                AttributeCategory.Physical,
                AttributeCategory.Mental,
                AttributeCategory.Supernatural
            };

            for (int i = 0; i < Iterations; i++)
            {
                CharacterAttributes attrs = AttributeGenerator.Generate(i);

                foreach (var category in categories)
                {
                    int total = attrs.GetCategoryTotal(category);
                    Assert.IsTrue(
                        total <= 10,
                        $"Categoria {category} tem {total} pontos, excedendo o máximo de 10. " +
                        $"(seed={i})"
                    );
                }
            }
        }

        /// <summary>
        /// Valida que NENHUMA categoria possui menos de 6 pontos.
        /// O mínimo é a categoria com bônus 3: 3 (base) + 3 = 6.
        /// </summary>
        [Test]
        public void Generate_NoCategoryBelow6_Tests()
        {
            AttributeCategory[] categories = {
                AttributeCategory.Social,
                AttributeCategory.Physical,
                AttributeCategory.Mental,
                AttributeCategory.Supernatural
            };

            for (int i = 0; i < Iterations; i++)
            {
                CharacterAttributes attrs = AttributeGenerator.Generate(i);

                foreach (var category in categories)
                {
                    int total = attrs.GetCategoryTotal(category);
                    Assert.IsTrue(
                        total >= 6,
                        $"Categoria {category} tem {total} pontos, abaixo do mínimo de 6. " +
                        $"(seed={i})"
                    );
                }
            }
        }

        // ══════════════════════════════════════════════════════════════════
        // 3. TESTES DE CONSISTÊNCIA
        // ══════════════════════════════════════════════════════════════════

        /// <summary>
        /// Valida que a mesma seed sempre gera o mesmo conjunto de atributos.
        /// Garante reprodutibilidade do sistema de geração.
        /// </summary>
        [Test]
        public void Generate_SeedProducesSameResult_Tests()
        {
            int[] seedsToTest = { 0, 1, 42, 123, 9999, int.MaxValue / 2 };

            foreach (int seed in seedsToTest)
            {
                CharacterAttributes first = AttributeGenerator.Generate(seed);
                CharacterAttributes second = AttributeGenerator.Generate(seed);

                Assert.AreEqual(first.Charisma, second.Charisma,
                    $"Charisma difere entre duas gerações com seed={seed}");
                Assert.AreEqual(first.Manipulation, second.Manipulation,
                    $"Manipulation difere entre duas gerações com seed={seed}");
                Assert.AreEqual(first.Composure, second.Composure,
                    $"Composure difere entre duas gerações com seed={seed}");
                Assert.AreEqual(first.Strength, second.Strength,
                    $"Strength difere entre duas gerações com seed={seed}");
                Assert.AreEqual(first.Dexterity, second.Dexterity,
                    $"Dexterity difere entre duas gerações com seed={seed}");
                Assert.AreEqual(first.Resistance, second.Resistance,
                    $"Resistance difere entre duas gerações com seed={seed}");
                Assert.AreEqual(first.Intelligence, second.Intelligence,
                    $"Intelligence difere entre duas gerações com seed={seed}");
                Assert.AreEqual(first.Wits, second.Wits,
                    $"Wits difere entre duas gerações com seed={seed}");
                Assert.AreEqual(first.Resolution, second.Resolution,
                    $"Resolution difere entre duas gerações com seed={seed}");
                Assert.AreEqual(first.Sensibility, second.Sensibility,
                    $"Sensibility difere entre duas gerações com seed={seed}");
                Assert.AreEqual(first.Understanding, second.Understanding,
                    $"Understanding difere entre duas gerações com seed={seed}");
                Assert.AreEqual(first.Protection, second.Protection,
                    $"Protection difere entre duas gerações com seed={seed}");
            }
        }

        /// <summary>
        /// Valida que seeds diferentes geram resultados diferentes na maioria das vezes.
        /// Usa 50 pares de seeds adjacentes e espera que pelo menos 80% produzam
        /// resultados distintos (evita falsos positivos por colisão de hash).
        /// </summary>
        [Test]
        public void Generate_DifferentSeedsProduceDifferentResults_Tests()
        {
            int differentCount = 0;
            int totalPairs = 50;

            for (int i = 0; i < totalPairs; i++)
            {
                int seedA = i;
                int seedB = i + 1000; // offset grande para minimizar colisões

                CharacterAttributes a = AttributeGenerator.Generate(seedA);
                CharacterAttributes b = AttributeGenerator.Generate(seedB);

                bool areDifferent = a.Charisma != b.Charisma
                                 || a.Manipulation != b.Manipulation
                                 || a.Composure != b.Composure
                                 || a.Strength != b.Strength
                                 || a.Dexterity != b.Dexterity
                                 || a.Resistance != b.Resistance
                                 || a.Intelligence != b.Intelligence
                                 || a.Wits != b.Wits
                                 || a.Resolution != b.Resolution
                                 || a.Sensibility != b.Sensibility
                                 || a.Understanding != b.Understanding
                                 || a.Protection != b.Protection;

                if (areDifferent) differentCount++;
            }

            int minExpected = (int)(totalPairs * 0.8); // 80% mínimo
            Assert.IsTrue(
                differentCount >= minExpected,
                $"Apenas {differentCount}/{totalPairs} pares de seeds diferentes " +
                $"produziram resultados distintos (mínimo esperado: {minExpected})."
            );
        }

        // ══════════════════════════════════════════════════════════════════
        // 4. TESTES DO SCRIPTABLE OBJECT
        // ══════════════════════════════════════════════════════════════════

        /// <summary>
        /// Valida que o AttributeSystemSO, após gerar atributos, possui exatamente 4 categorias.
        /// </summary>
        [Test]
        public void AttributeSystemSO_Initialize_HasCorrectCategoryCount_Tests()
        {
            AttributeSystemSO so = ScriptableObject.CreateInstance<AttributeSystemSO>();

            try
            {
                so.GenerateRandomAttributes();

                Assert.IsNotNull(so.Categories,
                    "O array de categorias não deveria ser nulo após geração.");
                Assert.AreEqual(
                    CategoryCount, so.Categories.Length,
                    $"Deveria haver {CategoryCount} categorias, " +
                    $"mas foram encontradas {so.Categories.Length}."
                );
            }
            finally
            {
                Object.DestroyImmediate(so);
            }
        }

        /// <summary>
        /// Valida que o método GetCategoryTotal retorna corretamente a soma dos 3 atributos
        /// de cada categoria, para todas as 4 categorias.
        /// </summary>
        [Test]
        public void AttributeSystemSO_GetCategoryTotal_ReturnsCorrectValue_Tests()
        {
            AttributeSystemSO so = ScriptableObject.CreateInstance<AttributeSystemSO>();

            try
            {
                so.GenerateRandomAttributes();

                AttributeCategory[] categories = {
                    AttributeCategory.Social,
                    AttributeCategory.Physical,
                    AttributeCategory.Mental,
                    AttributeCategory.Supernatural
                };

                foreach (var category in categories)
                {
                    CategoryData catData = so.GetCategoryData(category);

                    Assert.IsNotNull(catData.attributes,
                        $"Categoria {category} não deveria ter attributes nulos.");
                    Assert.AreEqual(
                        AttrsPerCategory, catData.attributes.Length,
                        $"Categoria {category} deveria ter {AttrsPerCategory} atributos, " +
                        $"mas tem {catData.attributes.Length}."
                    );

                    // Calcula a soma manualmente
                    int manualSum = 0;
                    for (int i = 0; i < catData.attributes.Length; i++)
                    {
                        manualSum += catData.attributes[i].value;
                    }

                    // Compara com GetCategoryTotal
                    int reportedTotal = so.GetCategoryTotal(category);
                    Assert.AreEqual(
                        manualSum, reportedTotal,
                        $"GetCategoryTotal({category}) retornou {reportedTotal}, " +
                        $"mas a soma manual é {manualSum}."
                    );
                }
            }
            finally
            {
                Object.DestroyImmediate(so);
            }
        }

        // ══════════════════════════════════════════════════════════════════
        // TESTES EXTRAS DE ROBUSTEZ
        // ══════════════════════════════════════════════════════════════════

        /// <summary>
        /// Valida que o AttributeSystemSO gera um total de 32 pontos
        /// (12 base + 20 extras), assim como o AttributeGenerator.
        /// </summary>
        [Test]
        public void AttributeSystemSO_TotalPointsEquals32_Tests()
        {
            AttributeSystemSO so = ScriptableObject.CreateInstance<AttributeSystemSO>();

            try
            {
                so.GenerateRandomAttributes();

                int total = so.GetTotalPoints();
                Assert.AreEqual(
                    ExpectedTotalPoints, total,
                    $"AttributeSystemSO deveria gerar {ExpectedTotalPoints} pontos, " +
                    $"mas gerou {total}."
                );
            }
            finally
            {
                Object.DestroyImmediate(so);
            }
        }

        /// <summary>
        /// Valida que nenhum atributo individual no ScriptableObject excede o range [1, 5].
        /// </summary>
        [Test]
        public void AttributeSystemSO_AllAttributesWithinRange_Tests()
        {
            AttributeSystemSO so = ScriptableObject.CreateInstance<AttributeSystemSO>();

            try
            {
                so.GenerateRandomAttributes();

                foreach (CategoryData cat in so.Categories)
                {
                    Assert.IsNotNull(cat.attributes,
                        $"Categoria {cat.category} não deveria ter attributes nulos.");

                    foreach (AttributeEntry entry in cat.attributes)
                    {
                        Assert.IsTrue(
                            entry.value >= MinAttributeValue && entry.value <= MaxAttributeValue,
                            $"Atributo {entry.type} na categoria {cat.category} " +
                            $"tem valor {entry.value}, fora do range [1, 5]."
                        );
                    }
                }
            }
            finally
            {
                Object.DestroyImmediate(so);
            }
        }

        /// <summary>
        /// Valida que o resumo gerado pelo AttributeGenerator contém informações
        /// essenciais (nomes das categorias e o total geral).
        /// </summary>
        [Test]
        public void Generate_GetSummary_ContainsExpectedContent_Tests()
        {
            CharacterAttributes attrs = AttributeGenerator.Generate(42);
            string summary = attrs.GetSummary();

            Assert.IsTrue(summary.Contains("Social"),
                "Resumo deveria conter a categoria 'Social'.");
            Assert.IsTrue(summary.Contains("Physical"),
                "Resumo deveria conter a categoria 'Physical'.");
            Assert.IsTrue(summary.Contains("Mental"),
                "Resumo deveria conter a categoria 'Mental'.");
            Assert.IsTrue(summary.Contains("Supernatural"),
                "Resumo deveria conter a categoria 'Supernatural'.");
            Assert.IsTrue(summary.Contains("Total Geral: 32"),
                $"Resumo deveria conter 'Total Geral: 32'.\nResumo completo:\n{summary}");
        }

        /// <summary>
        /// Valida que o método Generate(int seed) lança exceção quando receives null.
        /// Nota: Generate(System.Random rng) é o que lança, mas Generate(int) nunca deveria.
        /// Este teste garante que Generate(int) NÃO lança exceção com qualquer seed.
        /// </summary>
        [Test]
        public void Generate_WithVariousSeeds_DoesNotThrow_Tests()
        {
            int[] edgeSeeds = { 0, -1, 1, int.MinValue, int.MaxValue };

            foreach (int seed in edgeSeeds)
            {
                Assert.DoesNotThrow(
                    () => AttributeGenerator.Generate(seed),
                    $"Generate({seed}) não deveria lançar exceção."
                );
            }
        }
    }
}
