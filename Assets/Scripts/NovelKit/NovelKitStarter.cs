using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Novel.Runtime;
using VContainer.Unity;

/// <summary>
/// 起動時に保存済みフラグを復元してシナリオ再生を開始し、完了時に状態を保存する
/// </summary>
public class NovelKitStarter : IStartable, IDisposable
{
    private readonly INovelScenarioRunner _runner;
    private readonly GameProgressService _gameProgressService;
    private readonly string _scenarioKey;
    private readonly CancellationTokenSource _cts = new();

    public NovelKitStarter(INovelScenarioRunner runner, GameProgressService gameProgressService, string scenarioKey)
    {
        _runner = runner;
        _gameProgressService = gameProgressService;
        _scenarioKey = scenarioKey;
    }

    private async UniTaskVoid PlayAsync(CancellationToken ct)
    {
        // 分岐リプレイを決定的にするため、再生前にフラグを復元しておく
        if (NovelSaveSerializer.TryDeserialize(_gameProgressService.GetNovelKitState(), out var snapshot))
            _runner.RestoreState(snapshot);

        await _runner.PlayAsync(_scenarioKey, ct);
        _gameProgressService.SaveNovelKitState(NovelSaveSerializer.Serialize(_runner.CaptureState()));
    }

    public void Start()
    {
        PlayAsync(_cts.Token).Forget();
    }

    public void Dispose()
    {
        _cts.Cancel();
        _cts.Dispose();
    }
}
