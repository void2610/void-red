using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine.TestTools;
using Void2610.LiminalPalette.TestSupport;

/// <summary>
/// "Title/Scenario/" prefix の LiminalScenario を Unity Test Runner で回す薄いランナー
/// シナリオを追加すれば自動で Test Runner に出現する
/// </summary>
public sealed class TitleScenariosE2ETests
{
    [UnityTest]
    public IEnumerator Run([ValueSource(nameof(Paths))] string scenarioPath) => LiminalPaletteTestRunner.RunScenario(scenarioPath);

    public static IEnumerable<string> Paths => LiminalPaletteTestRunner.GetScenariosWithPrefix("Title/Scenario/");
}
