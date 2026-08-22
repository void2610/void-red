using System;

/// <summary>
/// InputSystem_Actionsのシングルトンプロバイダー
/// VContainerでSingletonとして管理され、アプリケーション全体で共有される
/// </summary>
public class InputActionsProvider : IDisposable
{
    /// <summary>
    /// Battle アクションマップへのアクセス
    /// </summary>
    public InputSystem_Actions.BattleActions Battle => _inputActions.Battle;

    /// <summary>
    /// UI アクションマップへのアクセス
    /// </summary>
    public InputSystem_Actions.UIActions UI => _inputActions.UI;

    /// <summary>
    /// Novel アクションマップへのアクセス
    /// </summary>
    public InputSystem_Actions.NovelActions Novel => _inputActions.Novel;

    private InputSystem_Actions _inputActions;

    public InputActionsProvider()
    {
        // InputSystem_Actionsのインスタンスを作成
        _inputActions = new InputSystem_Actions();

        // 現在のシーンに応じてアクションマップを有効化（デバッグで直接シーンを開いた場合に対応）
        var currentScene = SceneUtility.GetCurrentSceneType();
        EnableActionMapsForScene(currentScene);
    }

    /// <summary>
    /// 指定されたシーンに応じてアクションマップを有効化
    /// </summary>
    public void EnableActionMapsForScene(SceneType sceneType)
    {
        // 全てのアクションマップを無効化
        _inputActions.Battle.Disable();
        _inputActions.Novel.Disable();

        // UIは常に有効
        _inputActions.UI.Enable();

        // シーンに応じてアクションマップを有効化
        switch (sceneType)
        {
            case SceneType.Auction:
                _inputActions.Battle.Enable();
                break;
            case SceneType.Novel:
                _inputActions.Novel.Enable();
                break;
        }
    }

    // InputActionsのクリーンアップ
    public void Dispose()
    {
        _inputActions?.Dispose();
    }
}
