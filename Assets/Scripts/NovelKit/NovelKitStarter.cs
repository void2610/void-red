using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading;
using Cysharp.Threading.Tasks;
using Novel.Assets;
using Novel.Runtime;
using UnityEngine;
using VContainer.Unity;
using Void2610.UnityTemplate;

/// <summary>
/// 現在のストーリーノードのシナリオを再生し、完了後に進行を記録して次のシーンへ遷移する
/// </summary>
public class NovelKitStarter : IStartable, IDisposable
{
    // シナリオ本文からスプライトキーを拾う。拡張子付きの文字列リテラルだけを対象にする
    private static readonly Regex SPRITE_KEY_PATTERN = new(@"'([^']+\.(?:png|jpg|jpeg))'", RegexOptions.Compiled);

    private readonly INovelScenarioRunner _runner;
    private readonly GameProgressService _gameProgressService;
    private readonly SceneTransitionManager _sceneTransitionManager;
    private readonly ISpriteLoader _spriteLoader;
    private readonly string _scenarioKeyOverride;
    private readonly CancellationTokenSource _cts = new();

    public NovelKitStarter(INovelScenarioRunner runner, GameProgressService gameProgressService,
        SceneTransitionManager sceneTransitionManager, ISpriteLoader spriteLoader, string scenarioKeyOverride)
    {
        _runner = runner;
        _gameProgressService = gameProgressService;
        _sceneTransitionManager = sceneTransitionManager;
        _spriteLoader = spriteLoader;
        _scenarioKeyOverride = scenarioKeyOverride;
    }

    // 表示時にロードすると初出のたびに待たされるため、再生前にまとめて温めておく
    private async UniTask PreloadSpritesAsync(string scenarioKey, CancellationToken ct)
    {
        var text = Resources.Load<TextAsset>($"Scenarios/{scenarioKey}");
        if (text == null) return;

        var keys = new HashSet<string>();
        foreach (Match m in SPRITE_KEY_PATTERN.Matches(text.text)) keys.Add(m.Groups[1].Value);
        if (keys.Count == 0) return;

        var tasks = new List<UniTask>(keys.Count);
        foreach (var key in keys) tasks.Add(_spriteLoader.LoadAsync(key, ct).AsUniTask());
        await UniTask.WhenAll(tasks);
    }

    private async UniTask PlayAsync(CancellationToken ct)
    {
        // 分岐リプレイを決定的にするため、再生前にフラグを復元しておく
        if (NovelSaveSerializer.TryDeserialize(_gameProgressService.GetNovelKitState(), out var snapshot))
            _runner.RestoreState(snapshot);

        // 進行を記録すると次ノードへ進んでしまうため、遷移先の判定に使うノードは先に確保する
        var node = _gameProgressService.GetCurrentNode();
        var scenarioKey = string.IsNullOrEmpty(_scenarioKeyOverride) ? node.NodeId : _scenarioKeyOverride;

        await PreloadSpritesAsync(scenarioKey, ct);

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

            // 単体再生の検証中はシーンに留めて原因を追えるようにする
            if (string.IsNullOrEmpty(_scenarioKeyOverride))
                await _sceneTransitionManager.TransitionToSceneWithFade(SceneType.Home);
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
        BgmManager.Instance.PlayBGM("Novel");
        SafeNavigationManager.SelectRootForceSelectable().Forget();
        PlayAsync(_cts.Token).Forget(Debug.LogException);
    }

    public void Dispose()
    {
        _cts.Cancel();
        _cts.Dispose();
    }
}
