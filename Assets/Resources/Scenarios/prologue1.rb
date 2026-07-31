bg 'Background/LobbyDark.jpg'
se 'DoorArrival'
narration '(エレベーターの音)'

se 'DoorArrivalBell'
narration '(エレベーターの扉が開く)'

say :player, '………。ん……。', display_as: '■■■'
say :player, 'あれ……ここは……？', display_as: '■■■'
say :player, '………………。', display_as: '■■■'
say :player, 'なにも……思い出せない……。', display_as: '■■■'

se 'Footstep'
narration '（足音が近づく）'

say :player, '……何の音……？', display_as: '■■■'

stage :single, [:alv]
portrait :alv, 'Character/Alv/Mask.png'

say :alv, 'おかえりなさいませ、■■■様。', display_as: '???'
say :player, '……？', display_as: '■■■'
say :player, '誰……？', display_as: '■■■'
say :alv, 'おや。', display_as: '???'
say :alv, '■■■様、ずいぶん驚かれているようですね。', display_as: '???'
say :player, 'あなたは……誰？', display_as: '■■■'
say :player, 'ここは……どこなの？', display_as: '■■■'
say :alv, 'これはこれは。', display_as: '???'
say :alv, '申し遅れました。', display_as: '???'

say :alv, 'この場所の支配人を務めている、アルヴと申します。'
say :alv, '以後、お見知り置きを。'

say :player, 'アルヴ……？', display_as: '■■■'
say :player, 'ここって……何なの？', display_as: '■■■'
say :player, 'どうして私、ここに……', display_as: '■■■'

say :alv, '本当に、何も覚えていらっしゃらないのですね。'
say :alv, 'では、ボクがご説明いたしましょう。'
say :alv, 'ここはVOID RED。'
say :alv, 'あなたが自ら望んで来られた、オークション会場です。'

say :player, '……オークション？', display_as: '■■■'
say :player, '私が……望んで？', display_as: '■■■'

say :alv, 'ええ。あなたにはとても愉快なオークションにご参加いただきます。'
say :alv, 'その名も——「精神オークション」。'

say :player, '精神……オークション？', display_as: '■■■'

say :alv, 'この会場には、様々な品物が出品されています。'
say :alv, 'さて、ここでクイズです。'
say :alv, '何が並んでいると思いますか？'

say :player, '……壺？ 絵画？ 高そうなものとか……？', display_as: '■■■'

say :alv, '面白いですね。'
say :alv, '正解は——「記憶」。'

say :player, '……記憶！？', display_as: '■■■'
say :player, '一体どういうこと…？', display_as: '■■■'

say :alv, '驚かれるのも当然です。'
say :alv, 'さらに申し上げますと——'
say :alv, 'ここには、あなたの記憶も出品されています。'

say :player, '……っ！？', display_as: '■■■'
say :player, 'じゃあ……私が何も思い出せないのって……', display_as: '■■■'
say :player, 'あなたのせい……？', display_as: '■■■'
say :player, '返してよ……私の記憶！', display_as: '■■■'

say :alv, 'それはできません。'
say :alv, 'ここでは、自分の力で記憶を取り戻していただきます。'
say :alv, 'それが、この会場のルールです。'

say :player, '勝手に奪っておいて、ルール？', display_as: '■■■'
say :player, 'ふざけないで。そんなの、従うわけ——', display_as: '■■■'

say :alv, 'あなたには、ボクの話した通り——'
say :alv, 'このオークションに参加する以外、記憶を取り戻す術はありません。'
say :alv, '……理解が遅いですね。'
say :alv, '少々、くどい。'

say :player, '……もう……訳が分からない……。', display_as: '■■■'
say :player, '言い返す気力もなくなってきた……', display_as: '■■■'

say :alv, 'では、大人しくボクの案内を聞いていただけると嬉しいです。'
say :alv, 'さて、あなたには記憶を競り落とすために、他のお客様と戦っていただきます。'
say :alv, 'その際に使用するのが——精神札。'

# TODO: 旧 Excel の GetItem=Card,精神札 に相当するアイテム取得演出。novel-kit に該当コマンドが無く未移行
say :alv, 'こちらが精神札。'

say :alv, 'あなたの"価値"を数値化したものです。'
say :alv, 'これがなければ、オークションには参加できません。'
say :alv, '……くれぐれも、破損などなさらぬよう。'

say :player, '破損って……そんなガサツに見えてる？', display_as: '■■■'

say :alv, 'とんでもない。'
say :alv, 'ただ、無理はなさらぬように。'
say :alv, '……まあ、今はまだ意味が分からないでしょうから——'
say :alv, '模擬オークションで、慣れていただきましょう。'

narration '（足音が遠ざかる）'
