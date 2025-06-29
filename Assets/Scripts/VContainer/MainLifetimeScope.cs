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
        // プレイヤーの初期化
        _player = new Player(playerHandView);
        builder.RegisterInstance(_player).AsSelf();
        // NPCの初期化
        _enemy = new Enemy(enemyHandView);
        builder.RegisterInstance(_enemy).AsSelf();
        
        builder.RegisterInstance(allCardData);
        builder.RegisterInstance(allThemeData);
        RegisterAllData();
        
        builder.Register<CardPoolService>(Lifetime.Singleton);
        builder.Register<ThemeService>(Lifetime.Singleton);
        
        builder.RegisterEntryPoint<GameManager>();
        builder.RegisterComponentInHierarchy<UIPresenter>();
    }
}
