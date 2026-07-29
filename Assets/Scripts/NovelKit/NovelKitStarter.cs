using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Novel.Assets;
using Novel.Runtime;
using VContainer.Unity;

/// <summary>
/// 起動時に指定シナリオの再生を開始する
/// </summary>
public class NovelKitStarter : IStartable, IDisposable
{
    private readonly INovelScenarioRunner _runner;
    private readonly ISpriteLoader _spriteLoader;
    private readonly string _scenarioKey;
    private readonly CancellationTokenSource _cts = new();

    public NovelKitStarter(INovelScenarioRunner runner, ISpriteLoader spriteLoader, string scenarioKey)
    {
        _runner = runner;
        _spriteLoader = spriteLoader;
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
        // 表示中は参照が生きている必要があるため、シナリオの区切りでまとめて解放する
        _spriteLoader.ReleaseAll();
    }
}
