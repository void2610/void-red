
using UnityEngine;
using VContainer;
using VContainer.Unity;

/// <summary>
/// オークションシーン用の LifetimeScope
/// 進行度から階層を決め、主人公の手持ちを補充してセッションを組む
/// </summary>
public class AuctionLifetimeScope : LifetimeScope
{
    [Tooltip("-1 なら進行度に従う。0 以上ならその階層を単体で回す (デバッグ用)")]
    [SerializeField] private int floorOverride = -1;

    [Tooltip("0 なら毎回ランダム。それ以外は固定シードで決定的に回す (デバッグ用)")]
    [SerializeField] private int seedOverride = 0;

    protected override void Configure(IContainerBuilder builder)
    {
        builder.RegisterComponentInHierarchy<AuctionSceneView>();
        builder.Register(resolver =>
        {
            var allFloorData = resolver.Resolve<AllFloorData>();
            var progress = resolver.Resolve<GameProgressService>();
            var request = resolver.Resolve<AuctionStartRequest>();
            var floorIndex = request.FloorOverride ?? (floorOverride >= 0 ? floorOverride : (progress.GetNextNode() as AuctionNode)?.FloorIndex ?? 0);
            var seed = request.Seed ?? seedOverride;
            var rng = seed == 0 ? new System.Random() : new System.Random(seed);
            var timeout = request.CompetitionTimeout ?? GameConstants.COMPETITION_TIMEOUT_SECONDS;
            return new AuctionSession(allFloorData.GetFloor(floorIndex), progress.PrepareWalletForFloor(floorIndex), "ノア", rng, timeout);
        }, Lifetime.Scoped);

        // オークション画面には設定ボタンを置かないため、バトル用の登録 (SettingButtonView 無し) を使う
        builder.RegisterSettingsFeatureForBattle();
        builder.RegisterEntryPoint<AuctionPresenter>();
    }
}
