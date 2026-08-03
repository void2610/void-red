# 表示まわりの手動検証用。ストーリー進行からは参照されず、scenarioKeyOverride で単体再生する
bg 'Background/Lobby.jpg'

stage :single, [:alv]
portrait :alv, 'Character/Alv/Mask.png'

# 旧 Excel の CustomCharSpeed 相当。数値は文字送り速度の倍率
say :alv, '<speed=0.2>速度が5分の1のセリフ'
say :alv, '<speed=5>速度が5倍のセリフ'

se 'DoorArrivalBell'
narration 'SEが鳴ります'

# 旧 Excel の AutoAdvance 相当。行末に置くと表示後にその秒数だけ待つ
narration '表示後に0.5秒待ちます<w=0.5>'

bg 'Background/LobbyDark.jpg'
narration '背景が変わります'
bg 'Background/Lobby.jpg'
narration '背景が戻ります'

narration '<shake>揺れる文字</shake>と<wave>波打つ文字</wave>'

# カタログ未登録の話者はIDがそのまま表示される
say :unknown_speaker, 'カタログに無い話者'

portrait :alv, ''
narration '立ち絵が退場します'
