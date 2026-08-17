#if UNITY_EDITOR || DEVELOPMENT_BUILD || LIMINAL_PALETTE_FORCE_ENABLE
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
    public string IsFading() => _sceneTransitionManager.IsFading.ToString();

    [LiminalCommand("Scene/Transition", Description = "指定シーンへフェード付きで遷移し、完了後のシーン名を返す")]
    public async UniTask<string> Transition(SceneType scene)
    {
        await _sceneTransitionManager.TransitionToSceneWithFade(scene);
        return SceneManager.GetActiveScene().name;
    }
}
#endif
