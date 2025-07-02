/// <summary>
/// プレイヤークラス
/// ユーザーが操作するプレイヤーを表す
/// </summary>
public class Player : PlayerPresenter
{
    public Player(HandView handView, int maxHandSize = 3) 
        : base(handView, maxHandSize) { }
}