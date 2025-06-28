using UnityEngine;
using VContainer;
using VContainer.Unity;

public class MainLifetimeScope : LifetimeScope
{
    [SerializeField] private AllCardData allCardData;
    [SerializeField] private AllThemeData allThemeData;
    [SerializeField] private Player player;
    [SerializeField] private Enemy enemy;
    
    private void RegisterAllData()
    {
        #if UNITY_EDITOR
        allCardData.RegisterAllCards();
        allThemeData.RegisterAllThemes();
        #endif
    }
    
    protected override void Configure(IContainerBuilder builder)
    {
        builder.RegisterInstance(player);
        builder.RegisterInstance(enemy);
        builder.RegisterInstance(allCardData);
        builder.RegisterInstance(allThemeData);
        RegisterAllData();
        
        builder.Register<CardPoolService>(Lifetime.Singleton);
        builder.Register<ThemeService>(Lifetime.Singleton);
        
        builder.RegisterEntryPoint<GameManager>();
        builder.RegisterComponentInHierarchy<UIPresenter>();
    }
}
