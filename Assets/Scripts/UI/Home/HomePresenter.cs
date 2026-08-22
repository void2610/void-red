using System;
using Cysharp.Threading.Tasks;
using R3;
using UnityEngine;
using VContainer.Unity;
using Void2610.SettingsSystem;
using Void2610.UnityTemplate;

/// <summary>
/// ホーム画面のPresenter
/// ビジネスロジックとイベント処理を担当
/// </summary>
public class HomePresenter : IStartable, IDisposable
{
    private readonly HomeView _homeView;
    private readonly GameProgressService _gameProgressService;
    private readonly SceneTransitionManager _sceneTransitionManager;
    private readonly IConfirmationDialog _confirmationDialogService;

    private StoryNode _currentNode;
    private readonly CompositeDisposable _disposables = new();

    /// <summary>
    /// コンストラクタ（依存性注入）
    /// </summary>
    public HomePresenter(
        HomeView homeView,
        GameProgressService gameProgressService,
        SceneTransitionManager sceneTransitionManager,
        IConfirmationDialog confirmationDialogService)
    {
        _homeView = homeView;
        _gameProgressService = gameProgressService;
        _sceneTransitionManager = sceneTransitionManager;
        _confirmationDialogService = confirmationDialogService;
    }

    /// <summary>
    /// タイトルボタンがクリックされた時の処理
    /// </summary>
    private void OnTitleButtonClicked()
    {
        _sceneTransitionManager.TransitionToSceneWithFade(SceneType.Title).Forget();
    }

    /// <summary>
    /// 現在のノードを開始
    /// </summary>
    private async UniTask StartCurrentNodeAsync()
    {
        _currentNode = _gameProgressService.GetNextNode();

        switch (_currentNode)
        {
            case BattleNode battleNode:
                await _sceneTransitionManager.TransitionToSceneWithFade(SceneType.Battle);
                break;
            case NovelNode novelNode:
                await _sceneTransitionManager.TransitionToSceneWithFade(SceneType.Novel);
                break;
            default:
                await _sceneTransitionManager.TransitionToSceneWithFade(SceneType.Thanks);
                break;
        }
    }

    public void Start()
    {
        // Viewを初期化
        _homeView.Initialize();

        _homeView.TitleButtonClicked
            .Subscribe(_ => OnTitleButtonClicked())
            .AddTo(_disposables);

        _homeView.StoryButtonClicked
            .Subscribe(_ => StartCurrentNodeAsync().Forget())
            .AddTo(_disposables);

        // ホームBGMを再生
        BgmManager.Instance.PlayBGM("Home");

        SafeNavigationManager.SelectRootForceSelectable().Forget();
    }

    public void Dispose()
    {
        _disposables.Dispose();
    }
}
