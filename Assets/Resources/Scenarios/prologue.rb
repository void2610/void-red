chara :player

bg 'Lobby'

narration '……暗闇の中で、意識が浮かび上がる。'

stage :pair, [:cerica, :alv]
portrait :cerica, 'Cerica/Normal'
portrait :alv, 'Alv/Normal'

say :cerica, 'ここは……どこだ？', 'Cerica/Sad'
say :alv, '応える声は、ない。'
say :cerica, '<w=0.5>とにかく、進むしかないか。', 'Cerica/Smile'

exit_chara :alv
bg 'LobbyDark'

say :cerica, '……ひとりになってしまった。'
clear_stage
narration '(To be continued)'
