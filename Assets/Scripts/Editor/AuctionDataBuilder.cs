using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 記憶オークションの初期データ (参加者 / ロット / 階層) を Docs/voidred の仕様から生成するエディタツール
/// 数値は仮値。生成後は Inspector で調整する (再実行すると上書きされる)
/// </summary>
public static class AuctionDataBuilder
{
    private const string ROOT = "Assets/ScriptableObjects/Auction";

    // 立ち絵 / カットイン / アイコン / 所属色。素材が無いキャラは空のまま (UI 側で隠す)
    private static readonly Dictionary<string, (string portrait, string cutIn, string icon, Color color)> VISUALS = new()
    {
        ["alv"] = ("Assets/Sprites/Character/Alv/Normal.png", "Assets/Sprites/Auction/Dialogue/cut-in_alv.png", "Assets/Sprites/Character/Alv/Alv_icon.png", new Color(0.45f, 0.5f, 0.85f)),
        ["cerica"] = ("Assets/Sprites/Character/Cerica/Normal.png", "Assets/Sprites/Auction/Dialogue/cut-in_cerica.png", "Assets/Sprites/Character/Cerica/Cerica_icon.png", new Color(0.95f, 0.85f, 0.35f)),
        ["veil"] = ("Assets/Sprites/Character/Veil/Normal.png", "", "", new Color(0.9f, 0.3f, 0.7f)),
    };

    private record Rival(string Id, string Name, EmotionType Emotion, bool IsMob, int Base, int Fav, int Spread,
        BidReaction Provoke, BidReaction Empathize, BidReaction Persuade, bool PersuadeFav, int Scale, CompetitionPolicy Policy, int CounterChance,
        string Prompt, string A, string B, BidReaction ReactA, BidReaction ReactB,
        string ObserveLine, string ProvokeLine, string EmpathizeLine, string PersuadeLine, string FailLine);

    private record Lot(string Id, string Title, EmotionType Emotion, string Flavor, int Resonance, bool IsKey = false);

    [MenuItem("VoidRed/Auction/Build Data")]
    public static void Build()
    {
        EnsureDir($"{ROOT}/Participants");
        EnsureDir($"{ROOT}/Lots");
        EnsureDir($"{ROOT}/Floors");

        var rivals = Rivals().ToDictionary(r => r.Id, r => CreateParticipant(r));
        var lots = Lots().ToDictionary(l => l.Id, l => CreateLot(l));

        var floors = new List<FloorData>
        {
            CreateFloor(0, "自己を明らかにすること", "記憶は選び取ることで運命から人格に変わる",
                new[] { "alv", "mob_joy", "mob_sadness", "mob_anger" }, new[] { "0-1", "0-2", "0-3", "0-4", "0-5" }, rivals, lots),
            CreateFloor(1, "許したかったもの", "",
                new[] { "cerica", "veil", "mob_surprise", "mob_trust" }, new[] { "1-1", "1-2", "1-3", "1-4", "1-5" }, rivals, lots),
            CreateFloor(2, "諦めたかったもの", "",
                new[] { "eris", "mira", "mob_disgust", "mob_anticipation" }, new[] { "2-1", "2-2", "2-3", "2-4", "2-5" }, rivals, lots),
            CreateFloor(3, "憧れていたもの", "",
                new[] { "aria", "iliya", "mob_sadness", "mob_anger" }, new[] { "3-1", "3-2", "3-3", "3-4", "3-5" }, rivals, lots),
            CreateFloor(4, "壊したくなかったもの", "",
                new[] { "reina", "morgana", "mob_fear", "mob_joy" }, new[] { "4-1", "4-2", "4-3", "4-4", "4-5" }, rivals, lots),
        };

        var all = LoadOrCreate<AllFloorData>($"{ROOT}/AllFloorData.asset");
        var so = new SerializedObject(all);
        var list = so.FindProperty("floors");
        list.arraySize = floors.Count;
        for (var i = 0; i < floors.Count; i++) list.GetArrayElementAtIndex(i).objectReferenceValue = floors[i];
        so.ApplyModifiedPropertiesWithoutUndo();

        AssetDatabase.SaveAssets();
        Debug.Log($"[AuctionDataBuilder] 参加者 {rivals.Count} / ロット {lots.Count} / 階層 {floors.Count} を生成");
    }

    /// <summary>感情ごとの札の絵柄 (素材が無い属性は共通の下地を使う)</summary>
    private static Sprite LotSprite(EmotionType emotion)
    {
        return emotion switch
        {
            EmotionType.Joy => LoadSprite("Assets/Sprites/Card/card_joy_red.png"),
            EmotionType.Trust => LoadSprite("Assets/Sprites/Card/card_trust_blue.png"),
            EmotionType.Fear => LoadSprite("Assets/Sprites/Card/card_fear_red.png"),
            EmotionType.Surprise => LoadSprite("Assets/Sprites/Card/card_surprise_blue.png"),
            EmotionType.Sadness => LoadSprite("Assets/Sprites/Card/card_blue.png"),
            EmotionType.Disgust => LoadSprite("Assets/Sprites/Card/card_purple.png"),
            EmotionType.Anger => LoadSprite("Assets/Sprites/Card/card_red.png"),
            EmotionType.Anticipation => LoadSprite("Assets/Sprites/Card/card_green.png"),
            _ => LoadSprite("Assets/Sprites/Card/card_base.png"),
        };
    }

    /// <summary>札のカーテン絵。感情で 4 種を振り分けて彩りを出す</summary>
    private static MemoryType LotVisualStyle(EmotionType emotion)
    {
        return emotion switch
        {
            EmotionType.Joy or EmotionType.Anticipation => MemoryType.SelfMemory,
            EmotionType.Trust or EmotionType.Surprise => MemoryType.OtherMemory,
            EmotionType.Fear or EmotionType.Sadness => MemoryType.SpecificOtherMemory,
            _ => MemoryType.AmbiguousMemory,
        };
    }

    private static IEnumerable<Rival> Rivals()
    {
        yield return new("alv", "アルヴ", EmotionType.Surprise, false, 2, 3, 1, BidReaction.Increase, BidReaction.None, BidReaction.Decrease, false, 100, CompetitionPolicy.Normal, 40,
            "ふふ、ボクの手の内が気になりますか？", "気になる", "興味ない", BidReaction.BigIncrease, BidReaction.BigDecrease,
            "案内人ですから、手の内は隠しませんよ。", "おや、挑発ですか。乗ってあげましょう。", "共感、ですか。ボクには不要ですね。", "なるほど。では少し控えましょう。", "……ふふ、それは秘密です。");
        yield return new("cerica", "セリカ", EmotionType.Joy, false, 2, 6, 1, BidReaction.None, BidReaction.Increase, BidReaction.None, false, 30, CompetitionPolicy.Rarely, 50,
            "あなた、これが欲しいの？", "欲しい", "別に", BidReaction.BigIncrease, BidReaction.Decrease,
            "あらあら、見られているのね。", "可愛いわね。", "これが欲しいの？でも貰うわね。", "これは勝負だから……。", "うふふ、内緒よ。");
        yield return new("veil", "ヴェイル", EmotionType.Disgust, false, 3, 5, 2, BidReaction.Increase, BidReaction.None, BidReaction.Decrease, false, 120, CompetitionPolicy.FavoriteOnly, 15,
            "……アタシに何の用だし。", "組まない？", "邪魔しないで", BidReaction.Decrease, BidReaction.Increase,
            "見てんじゃないし。", "アタシとやろうっての？", "だから何？", "別に。そんなのくれてやるわ。", "はぁ？知らないし。");
        yield return new("eris", "エリス", EmotionType.Trust, false, 3, 3, 0, BidReaction.Increase, BidReaction.None, BidReaction.BigDecrease, false, 100, CompetitionPolicy.Never, 40,
            "ふええ、エリスはどうすればいいですの？", "賭けて", "引いて", BidReaction.BigIncrease, BidReaction.BigDecrease,
            "エリスの手を見るのですの？", "これに賭けろとおっしゃるのですのね。", "意図が、理解できませんの……。", "従いますの。", "ふええ……。");
        yield return new("mira", "ミラ", EmotionType.Sadness, false, 2, 4, 1, BidReaction.ShiftToNext, BidReaction.BigDecrease, BidReaction.Increase, false, 100, CompetitionPolicy.Rarely, 40,
            "わたくし、疑り深いのですわ。あなたは敵？", "味方", "敵", BidReaction.Decrease, BidReaction.BigIncrease,
            "およよ……見られていますわ。", "およよ……そんなことを。", "そんなことをされても……。", "およよ……では、逆に。", "……わかりませんわ。");
        yield return new("aria", "アリア", EmotionType.Anticipation, false, 3, 5, 3, BidReaction.PullFromNext, BidReaction.ShiftToNext, BidReaction.Random, false, 150, CompetitionPolicy.Random, 70,
            "ねぇねぇ、アタシのことどう思う？", "最高", "うるさい", BidReaction.BigIncrease, BidReaction.Random,
            "見てる見てる～★", "おもれ～！乗った！", "もっと遊んじゃおう★", "どっちを期待されてるのかな～？", "えっ何？聞いてなかった～");
        yield return new("iliya", "イリヤ", EmotionType.Fear, false, 1, 3, 1, BidReaction.Withdraw, BidReaction.PullFromNext, BidReaction.PullFromNext, false, 150, CompetitionPolicy.ByBidSize, 20,
            "ぼ、ぼくを観察してるの……？", "そうだよ", "気のせい", BidReaction.Withdraw, BidReaction.Increase,
            "み、見られてる……。", "わ、分からなすぎて怖いよ……。", "と、とられたら怖いから……。", "説得されるのが、怖いんだ……。", "……ぼ、ぼくには分からないよ。");
        yield return new("reina", "レイナ", EmotionType.Anger, false, 2, 7, 1, BidReaction.BigIncrease, BidReaction.None, BidReaction.BigDecrease, true, 120, CompetitionPolicy.AllIn, 40,
            "あたしと戦う覚悟はあるのか！", "ある！", "ない……", BidReaction.BigIncrease, BidReaction.Decrease,
            "見るがいい！隠すものなどないわよ！", "そちらがその気なら打ち砕いてやる！", "これは闘いなのだぞ、戯け！", "その判断が命取りだぞ！", "……ふん！");
        yield return new("morgana", "モルガナ", EmotionType.Surprise, false, 3, 4, 1, BidReaction.ShiftToNext, BidReaction.Random, BidReaction.Random, true, 120, CompetitionPolicy.FavoriteAggressive, 80,
            "アタクシを終わらせてくれるのは、あなた？", "ええ", "いいえ", BidReaction.BigIncrease, BidReaction.Decrease,
            "あら、アタクシの考えがお分かり？", "乗るかどうしようか、と言いつつ……。", "あらアタクシの考えがお分かり？", "このアタクシを説得しようとは、天晴！", "ですワ……？");

        var mobs = new (string id, EmotionType e, string name)[]
        {
            ("mob_joy", EmotionType.Joy, "喜ぶ参加者"), ("mob_trust", EmotionType.Trust, "信じる参加者"), ("mob_fear", EmotionType.Fear, "怯える参加者"),
            ("mob_surprise", EmotionType.Surprise, "驚く参加者"), ("mob_sadness", EmotionType.Sadness, "悲しむ参加者"), ("mob_disgust", EmotionType.Disgust, "嫌悪する参加者"),
            ("mob_anger", EmotionType.Anger, "怒る参加者"), ("mob_anticipation", EmotionType.Anticipation, "期待する参加者"),
        };
        foreach (var (id, e, name) in mobs)
        {
            yield return new(id, name, e, true, 2, 4, 1, BidReaction.Increase, BidReaction.Increase, BidReaction.Decrease, false, 100, CompetitionPolicy.Normal, 20,
                "……あなたも、やり直したいの？", "そうだ", "違う", BidReaction.Increase, BidReaction.Decrease,
                "……。", "……！", "……うん。", "……そうだね。", "……。");
        }
    }

    private static IEnumerable<Lot> Lots()
    {
        var doc = new Lot[]
        {
            new("1-1", "上手に笑えなかったこと", EmotionType.Joy, "ただ人よりちょっと不器用だっただけなんだ。", 70),
            new("3-1", "ありのままに生きること", EmotionType.Trust, "誰かの理想になりたかったわけではないんだ。", 60),
            new("3-2", "あの日救えなかったこと", EmotionType.Trust, "正しさだけでは誰かを救えなかったんだ。", 80),
            new("3-3", "何かを切り捨てたこと", EmotionType.Trust, "切り捨ててきたものが道を作ってきたと知っていたんだ。", 50),
            new("5-2", "もがき苦しんだこと", EmotionType.Joy, "感じた痛みの分だけ誰かの痛みを分かつことができたんだ。", 65),
            new("6-1", "脆く弱かった本音", EmotionType.Fear, "ただずっと嫌われるのが怖かったんだ。", 75),
            new("8-1", "何もわからなかったこと", EmotionType.Surprise, "レールを歩いていた私には自由はありあまったんだ。", 55),
            new("8-2", "母を優先しなかったこと", EmotionType.Surprise, "信じた結果失ったものの大きさを知らなかったんだ。", 85),
        };
        // 階層ごとに 5 個。原典の 8 個を散らし、残りはプレースホルダ
        var emotions = EmotionWallet.ALL_EMOTIONS;
        var placeholderIndex = 0;
        var docQueue = new Queue<Lot>(doc);
        for (var floor = 0; floor <= 4; floor++)
        {
            for (var n = 1; n <= 5; n++)
            {
                var id = $"{floor}-{n}";
                if (docQueue.Count > 0 && (n == 1 || n == 3))
                {
                    var d = docQueue.Dequeue();
                    yield return new Lot(id, d.Title, d.Emotion, d.Flavor, d.Resonance);
                    continue;
                }
                var e = emotions[placeholderIndex % emotions.Length];
                placeholderIndex++;
                if (floor == 4 && n == 5)
                {
                    yield return new Lot(id, "楽園への鍵", EmotionType.Anger, "壊したくなかったものの、最後の一片。", 100, true);
                    continue;
                }
                yield return new Lot(id, $"名もなき記憶 {id}", e, "まだ言葉になっていない記憶の断片。", 20 + (placeholderIndex * 13) % 60);
            }
        }
    }

    private static ParticipantData CreateParticipant(Rival r)
    {
        var asset = LoadOrCreate<ParticipantData>($"{ROOT}/Participants/{r.Id}.asset");
        var so = new SerializedObject(asset);
        so.FindProperty("displayName").stringValue = r.Name;
        so.FindProperty("emotion").enumValueIndex = (int)r.Emotion;
        so.FindProperty("isMob").boolValue = r.IsMob;
        if (VISUALS.TryGetValue(r.Id, out var visual))
        {
            so.FindProperty("portrait").objectReferenceValue = LoadSprite(visual.portrait);
            so.FindProperty("cutInSprite").objectReferenceValue = LoadSprite(visual.cutIn);
            so.FindProperty("iconSprite").objectReferenceValue = LoadSprite(visual.icon);
            so.FindProperty("themeColor").colorValue = visual.color;
        }
        else
        {
            // 立ち絵が無いモブは、司る感情のアイコンで見分けられるようにする
            so.FindProperty("iconSprite").objectReferenceValue = EmotionIcon(r.Emotion);
            so.FindProperty("themeColor").colorValue = r.Emotion.GetColor();
        }
        var p = so.FindProperty("profile");
        p.FindPropertyRelative("baseBid").intValue = r.Base;
        p.FindPropertyRelative("favoriteBid").intValue = r.Fav;
        p.FindPropertyRelative("spread").intValue = r.Spread;
        p.FindPropertyRelative("provokeReaction").enumValueIndex = (int)r.Provoke;
        p.FindPropertyRelative("empathizeReaction").enumValueIndex = (int)r.Empathize;
        p.FindPropertyRelative("persuadeReaction").enumValueIndex = (int)r.Persuade;
        p.FindPropertyRelative("persuadeBoostsFavorite").boolValue = r.PersuadeFav;
        p.FindPropertyRelative("reactionScale").intValue = r.Scale;
        p.FindPropertyRelative("competitionPolicy").enumValueIndex = (int)r.Policy;
        p.FindPropertyRelative("counterDialogueChance").intValue = r.CounterChance;
        var c = p.FindPropertyRelative("counterDialogue");
        c.FindPropertyRelative("prompt").stringValue = r.Prompt;
        c.FindPropertyRelative("choiceA").stringValue = r.A;
        c.FindPropertyRelative("choiceB").stringValue = r.B;
        c.FindPropertyRelative("reactionA").enumValueIndex = (int)r.ReactA;
        c.FindPropertyRelative("reactionB").enumValueIndex = (int)r.ReactB;
        p.FindPropertyRelative("observeLine").stringValue = r.ObserveLine;
        p.FindPropertyRelative("provokeLine").stringValue = r.ProvokeLine;
        p.FindPropertyRelative("empathizeLine").stringValue = r.EmpathizeLine;
        p.FindPropertyRelative("persuadeLine").stringValue = r.PersuadeLine;
        p.FindPropertyRelative("failLine").stringValue = r.FailLine;
        so.ApplyModifiedPropertiesWithoutUndo();
        return asset;
    }

    private static MemoryLotData CreateLot(Lot l)
    {
        var asset = LoadOrCreate<MemoryLotData>($"{ROOT}/Lots/{l.Id}.asset");
        var so = new SerializedObject(asset);
        so.FindProperty("title").stringValue = l.Title;
        so.FindProperty("flavor").stringValue = l.Flavor;
        so.FindProperty("emotion").enumValueIndex = (int)l.Emotion;
        so.FindProperty("resonance").intValue = l.Resonance;
        so.FindProperty("isKey").boolValue = l.IsKey;
        so.FindProperty("image").objectReferenceValue = LotSprite(l.Emotion);
        so.FindProperty("visualStyle").enumValueIndex = (int)LotVisualStyle(l.Emotion);
        so.ApplyModifiedPropertiesWithoutUndo();
        return asset;
    }

    private static FloorData CreateFloor(int index, string theme, string clarified, string[] rivalIds, string[] lotIds, Dictionary<string, ParticipantData> rivals, Dictionary<string, MemoryLotData> lots)
    {
        var asset = LoadOrCreate<FloorData>($"{ROOT}/Floors/Floor{index}.asset");
        var so = new SerializedObject(asset);
        so.FindProperty("floorIndex").intValue = index;
        so.FindProperty("themeTitle").stringValue = theme;
        so.FindProperty("clarifiedTheme").stringValue = clarified;
        var r = so.FindProperty("rivals");
        r.arraySize = rivalIds.Length;
        for (var i = 0; i < rivalIds.Length; i++) r.GetArrayElementAtIndex(i).objectReferenceValue = rivals[rivalIds[i]];
        var l = so.FindProperty("lots");
        l.arraySize = lotIds.Length;
        for (var i = 0; i < lotIds.Length; i++) l.GetArrayElementAtIndex(i).objectReferenceValue = lots[lotIds[i]];
        so.ApplyModifiedPropertiesWithoutUndo();
        return asset;
    }

    /// <summary>感情アイコン (入札ウィンドウの車輪と同じ絵柄)</summary>
    private static Sprite EmotionIcon(EmotionType emotion)
    {
        var name = emotion switch
        {
            EmotionType.Joy => "icon_joy",
            EmotionType.Trust => "icon_trust",
            EmotionType.Fear => "icon_fear",
            EmotionType.Surprise => "icon_surprise",
            EmotionType.Sadness => "icon_sorrow",
            EmotionType.Disgust => "icon_disgust",
            EmotionType.Anger => "icon_anger",
            _ => "icon_expectations",
        };
        return LoadSprite($"Assets/Sprites/Auction/Bid/EmotionIcons/{name}.png");
    }

    private static Sprite LoadSprite(string path)
    {
        return string.IsNullOrEmpty(path) ? null : AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    private static T LoadOrCreate<T>(string path) where T : ScriptableObject
    {
        var existing = AssetDatabase.LoadAssetAtPath<T>(path);
        if (existing != null) return existing;
        var created = ScriptableObject.CreateInstance<T>();
        AssetDatabase.CreateAsset(created, path);
        return created;
    }

    private static void EnsureDir(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        var parent = System.IO.Path.GetDirectoryName(path).Replace('\\', '/');
        EnsureDir(parent);
        AssetDatabase.CreateFolder(parent, System.IO.Path.GetFileName(path));
    }
}
