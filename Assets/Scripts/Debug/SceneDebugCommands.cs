using System;
using Cysharp.Threading.Tasks;
using UnityEngine.SceneManagement;
using Void2610.LiminalPalette;

/// <summary>
/// シーン遷移を LiminalPalette から観測 / 操作するデバッグコマンド群
/// </summary>
public sealed class SceneDebugCommands
{
    private readonly SceneTransitionManager _sceneTransitionManager;

    public SceneDebugCommands(SceneTransitionManager sceneTransitionManager)
    {
        _sceneTransitionManager = sceneTransitionManager;
    }

    [LiminalCommand("Scene/IsFading", Description = "クロスフェード遷移中かを返す")]
    public bool IsFading() => _sceneTransitionManager.IsFading;

    [LiminalCommand("Scene/Transition", Description = "指定シーンへフェード付きで遷移し、完了後のシーン名を返す")]
    public async UniTask<string> Transition(SceneType scene)
    {
        // EnumConverter は未定義の数値も通すため、空シーン名のロードで黒画面のまま止まる遷移を弾く
        if (!Enum.IsDefined(typeof(SceneType), scene)) throw new ArgumentOutOfRangeException(nameof(scene), $"未定義の SceneType: {(int)scene}");
        // TransitionToSceneWithFade はフェード中だと無言で no-op するため、成功と誤認させず失敗にする
        if (_sceneTransitionManager.IsFading) throw new InvalidOperationException("フェード遷移中のため実行できない。Scene/IsFading が false になってから再実行する");

        await _sceneTransitionManager.TransitionToSceneWithFade(scene);
        return SceneManager.GetActiveScene().name;
    }
}
