using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Novel.Runtime;
using VContainer.Unity;

/// <summary>
/// 起動時に指定シナリオの再生を開始する
/// </summary>
public class NovelKitStarter : IStartable, IDisposable
{
    private readonly INovelScenarioRunner _runner;
    private readonly string _scenarioKey;
    private readonly CancellationTokenSource _cts = new();

    public NovelKitStarter(INovelScenarioRunner runner, string scenarioKey)
    {
        _runner = runner;
        _scenarioKey = scenarioKey;
    }

    public void Start()
    {
        _runner.PlayAsync(_scenarioKey, _cts.Token).Forget();
    }

    public void Dispose()
    {
        _cts.Cancel();
        _cts.Dispose();
    }
}
