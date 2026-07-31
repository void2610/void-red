bg 'Background/LobbyDark.jpg'

stage :single, [:alv]
portrait :alv, 'Character/Alv/Mask.png'

say :alv, 'お疲れ様でした。模擬オークションは如何でしたか？'
say :player, '……まだ、よくわからない。 でも……少しだけ、自分のことが見えてきた気がする。'
say :alv, 'それは何よりです。'
say :alv, 'この階には、いつでも戻ってこられます。'
say :alv, '迷ったとき、休みたいとき'
say :alv, '——あるいは、ただボクと話したいときでも。'

say :alv, 'さて。 ここからが、本当の始まりです。 あなたに、最初の"選択"をしていただきます。'
say :player, '選択……？'
say :alv, 'ええ。'
say :alv, 'このVOID REDでは、記憶の深層に触れるために、運命の札を一枚引いていただきます。'

say :alv, 'どちらの札にも、意味があります。'
say :alv, 'どちらが正しいということはありません。'
say :alv, 'あなたの心が、今、どちらに触れたいか——それだけです。'

# TODO: 旧 Excel の CardChoice (札の画像 LightCard/ShadowCard 付き) 相当のコマンドが無く、素の choose で代替中
choose(['光の札を選ぶ', '影の札を選ぶ'], key: :prologue_fate_card)
