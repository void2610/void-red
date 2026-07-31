chara :player

bg 'Background/Lobby.jpg'

narration '……暗闇の中で、意識が浮かび上がる。'

stage :pair, [:cerica, :alv]
portrait :cerica, 'Character/Cerica/Normal.png'
portrait :alv, 'Character/Alv/Normal.png'

say :cerica, 'ここは……どこだ？', 'Character/Cerica/Sad.png'
say :alv, '応える声は、ない。'

se 'DoorArrivalBell'
say :cerica, '<w=0.5>とにかく、進むしかないか。', 'Character/Cerica/Smile.png'

# key を渡すと選択結果が安定キーで state に残り、セーブ対象になる
choose(['扉を開ける', 'その場に留まる'], key: :prologue_door)

if val(:prologue_door) == 0
  se 'DoorArrival'
  say :cerica, '……行こう。'
else
  say :cerica, '……もう少し、ここにいる。'
end

exit_chara :alv
bg 'Background/LobbyDark.jpg'

say :cerica, '……ひとりになってしまった。'
clear_stage
narration '(To be continued)'
