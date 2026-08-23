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
    private readonly AllFloorData _allFloorData;
    private readonly AuctionStartRequest _auctionStartRequest;

    private StoryNode _currentNode;
    private readonly CompositeDisposable _disposables = new();

    /// <summary>
    /// コンストラクタ（依存性注入）
    /// </summary>
    public HomePresenter(
        HomeView homeView,
        GameProgressService gameProgressService,
        SceneTransitionManager sceneTransitionManager,
        IConfirmationDialog confirmationDialogService,
        AllFloorData allFloorData,
        AuctionStartRequest auctionStartRequest)
    {
        _homeView = homeView;
        _gameProgressService = gameProgressService;
        _sceneTransitionManager = sceneTransitionManager;
        _confirmationDialogService = confirmationDialogService;
        _allFloorData = allFloorData;
        _auctionStartRequest = auctionStartRequest;
    }

    private static string DescribeNextNode(StoryNode node)
    {
        return node switch
        {
            AuctionNode a => $"次: 第 {a.FloorIndex} 階層の記憶オークション",
            NovelNode n => $"次: ストーリー ({n.ScenarioId})",
            _ => "次: 楽園",
        };
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
            case AuctionNode:
                await _sceneTransitionManager.TransitionToSceneWithFade(SceneType.Auction);
                break;
            case NovelNode:
                await _sceneTransitionManager.TransitionToSceneWithFade(SceneType.Novel);
                break;
            default:
                await _sceneTransitionManager.TransitionToSceneWithFade(SceneType.Thanks);
                break;
        }
    }

    public void Start()
    {
        // 階層をまたいだので、デバッグ起動の予約 (階層 / seed / 競合秒数) はここで捨てる
        _auctionStartRequest.Clear();

        // Viewを初期化
        _homeView.Initialize();

        _homeView.TitleButtonClicked
            .Subscribe(_ => OnTitleButtonClicked())
            .AddTo(_disposables);

        _homeView.StoryButtonClicked
            .Subscribe(_ => StartCurrentNodeAsync().Forget())
            .AddTo(_disposables);

        _homeView.CollectionButtonClicked
            .Subscribe(_ => _homeView.CollectionView.Show(_allFloorData, _gameProgressService.Persona))
            .AddTo(_disposables);

        _homeView.PersonaButtonClicked
            .Subscribe(_ => _homeView.PersonaView.Show(_allFloorData, _gameProgressService.Persona, _gameProgressService.PlayerWallet, _gameProgressService.CollapsedParticipantIds))
            .AddTo(_disposables);

        _homeView.SetProgressText(DescribeNextNode(_gameProgressService.GetNextNode()));

        // ホームBGMを再生
        BgmManager.Instance.PlayBGM("Home");

        SafeNavigationManager.SelectRootForceSelectable().Forget();
    }

    public void Dispose()
    {
        _disposables.Dispose();
    }
}
