chara :player

bg 'Background/Lobby.jpg'

narration '……暗闇の中で、意識が浮かび上がる。'

stage :pair, [:cerica, :alv]
portrait :cerica, 'Character/Cerica/Normal.png'
portrait :alv, 'Character/Alv/Normal.png'

say :cerica, 'ここは……どこだ？', 'Character/Cerica/Sad.png'
say :alv, '応える声は、ない。'
say :cerica, '<w=0.5>とにかく、進むしかないか。', 'Character/Cerica/Smile.png'

exit_chara :alv
bg 'Background/LobbyDark.jpg'

say :cerica, '……ひとりになってしまった。'
clear_stage
narration '(To be continued)'
