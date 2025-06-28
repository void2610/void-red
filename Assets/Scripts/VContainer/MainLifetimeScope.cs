using UnityEngine;
using VContainer;
using VContainer.Unity;

public class MainLifetimeScope : LifetimeScope
{
    [Header("ゲーム設定")]
    [SerializeField] private AllCardDataList allCardDataList;
    
    protected override void Configure(IContainerBuilder builder)
    {
        // AllCardDataListをインスタンスとして登録
        builder.RegisterInstance(allCardDataList);
        
        // CardPoolServiceをシングルトンとして登録
        builder.Register<CardPoolService>(Lifetime.Singleton);
        
        // GameManagerをシングルトンとして登録
        builder.RegisterComponentInHierarchy<GameManager>();
    }
}
