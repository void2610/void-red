using UnityEngine;
using VContainer;
using VContainer.Unity;

public class MainLifetimeScope : LifetimeScope
{
    [SerializeField] private AllCardData allCardData;
    [SerializeField] private AllThemeData allThemeData;
    [SerializeField] private HandView playerHandView;
    [SerializeField] private HandView enemyHandView;
    
    private Player _player;
    private Enemy _enemy;
    
    private void RegisterAllData()
    {
        #if UNITY_EDITOR
        allCardData.RegisterAllCards();
        allThemeData.RegisterAllThemes();
        #endif
    }
    
    protected override void Configure(IContainerBuilder builder)
    {
        // === プレイヤーの初期化（2層構造） ===
        
        // Player Model・HandView の作成
        _player = new Player(playerHandView, 3); // 最大手札数3
        builder.RegisterInstance(_player).AsSelf();
        
        // Enemy Model・HandView の作成
        _enemy = new Enemy(enemyHandView, 3); // 最大手札数3
        builder.RegisterInstance(_enemy).AsSelf();
        
        // === データとサービスの登録 ===
        
        builder.RegisterInstance(allCardData);
        builder.RegisterInstance(allThemeData);
        RegisterAllData();
        
        builder.Register<CardPoolService>(Lifetime.Singleton);
        builder.Register<ThemeService>(Lifetime.Singleton);
        
        // === エントリーポイントとPresenterの登録 ===
        
        builder.RegisterEntryPoint<GameManager>();
        builder.RegisterComponentInHierarchy<UIPresenter>();
    }
}
