using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Novel.Runtime;
using UnityEngine;
using VContainer.Unity;

/// <summary>
/// 現在のストーリーノードのシナリオを再生し、完了後に進行を記録して次のシーンへ遷移する
/// </summary>
public class NovelKitStarter : IStartable, IDisposable
{
    private readonly INovelScenarioRunner _runner;
    private readonly GameProgressService _gameProgressService;
    private readonly SceneTransitionManager _sceneTransitionManager;
    private readonly string _scenarioKeyOverride;
    private readonly CancellationTokenSource _cts = new();

    public NovelKitStarter(INovelScenarioRunner runner, GameProgressService gameProgressService,
        SceneTransitionManager sceneTransitionManager, string scenarioKeyOverride)
    {
        _runner = runner;
        _gameProgressService = gameProgressService;
        _sceneTransitionManager = sceneTransitionManager;
        _scenarioKeyOverride = scenarioKeyOverride;
    }

    private async UniTask PlayAsync(CancellationToken ct)
    {
        // 分岐リプレイを決定的にするため、再生前にフラグを復元しておく
        if (NovelSaveSerializer.TryDeserialize(_gameProgressService.GetNovelKitState(), out var snapshot))
            _runner.RestoreState(snapshot);

        // 進行を記録すると次ノードへ進んでしまうため、遷移先の判定に使うノードは先に確保する
        var node = _gameProgressService.GetCurrentNode();
        var scenarioKey = string.IsNullOrEmpty(_scenarioKeyOverride) ? node.NodeId : _scenarioKeyOverride;

        var result = await _runner.PlayAsync(scenarioKey, ct);
        _gameProgressService.SaveNovelKitState(NovelSaveSerializer.Serialize(_runner.CaptureState()));

        // 中断・失敗時はシーンに留まる (遷移すると進行だけ進んで復帰できなくなる)
        if (result != NovelResult.Completed)
        {
            Debug.LogWarning($"[NovelKitStarter] シナリオが完了しなかった: {scenarioKey} ({result})");
            return;
        }

        // .rb が無いと novel-kit は何も再生せず Completed を返すため、空振りで進行だけ進むのを防ぐ (SayNumber は再生ごとに 0 起点)
        if (_runner.CurrentSayNumber == 0)
        {
            Debug.LogError($"[NovelKitStarter] シナリオが再生されなかった: {scenarioKey} (.rb が存在するか確認)");
            return;
        }

        // シナリオ単体を再生する検証用シーンでは進行を進めない
        if (!string.IsNullOrEmpty(_scenarioKeyOverride)) return;

        _gameProgressService.RecordNovelResultAndSave();
        var next = node.ReturnToHome ? SceneType.Home : _gameProgressService.GetNextSceneType();
        await _sceneTransitionManager.TransitionToSceneWithFade(next);
    }

    public void Start()
    {
        PlayAsync(_cts.Token).Forget(Debug.LogException);
    }

    public void Dispose()
    {
        _cts.Cancel();
        _cts.Dispose();
    }
}
