using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine.TestTools;
using Void2610.LiminalPalette.TestSupport;

/// <summary>
/// "Lobby/Scenario/" prefix の LiminalScenario を Unity Test Runner で回す薄いランナー
/// </summary>
public sealed class LobbyScenariosE2ETests
{
    [UnityTest]
    public IEnumerator Run([ValueSource(nameof(Paths))] string scenarioPath) => LiminalPaletteTestRunner.RunScenario(scenarioPath);

    public static IEnumerable<string> Paths => LiminalPaletteTestRunner.GetScenariosWithPrefix("Lobby/Scenario/");
}
