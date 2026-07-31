bg 'Background/Lobby.jpg'

narration '（カタン、と小さな音がして、エレベーターが登り始めた）'
narration '（光の札。 なぜこれを選んだのか、自分でもよく分からない。 でも、他の札より魅力的に見えた気がした）'
narration '（カチンと音がなり、扉が開く）'
narration '（ここが1階ね...思ったより広いかも）'

se 'Footstep'
narration '（足音）'

stage :single, [:cerica]
portrait :cerica, 'Character/Cerica/Normal.png'

say :cerica, '「あなたも、光の札を選んだのね」', display_as: '???'
narration '（目の前にはきれいな女性の方がいた）'
narration '（彼女が手にしている札。私のと、同じ……）'
say :player, '「……うん。これしかないって、思ったから」'

portrait :cerica, 'Character/Cerica/Sad.png'
say :cerica, '「私もよ。選ばされた、って言った方が近いかもしれないけど」', display_as: '???'
say :player, '「もしかして……同じ札を選んだ人と、マッチングされるのかな」'

portrait :cerica, 'Character/Cerica/Normal.png'
say :cerica, "「おそらく、そうね。 \n私たちは、あの仮面の支配人から提示された二択で、\n同じものを選んでここに来た。 」", display_as: '???'
say :cerica, '「似たような感性の持ち主と、戦わせられるってことなのかも」', display_as: '???'
say :player, '「……記憶は失われてるのに、心の奥底に染みついた考え方は変わらないって感じがする。」'
say :cerica, '「ふふ、そうね。改めて実感させられたわね」', display_as: '???'
narration '（彼女は少し悲しげに顔を歪ませていた）'
say :cerica, '「それでは、自己紹介といきますか」', display_as: '???'

portrait :cerica, 'Character/Cerica/Smile.png'
say :cerica, '「私はセリカ。あなたは？」'

portrait :cerica, 'Character/Cerica/Normal.png'
say :player, '「……分からない。思い出せないの。名前も」'

portrait :cerica, 'Character/Cerica/Sad.png'
say :cerica, '「……名前まで奪われてしまったのね」'

portrait :cerica, 'Character/Cerica/Normal.png'
narration "（その声は、少しだけ震えていた。 \n彼女の表情が、ほんの一瞬だけ歪んだのが分かった）"

portrait :cerica, 'Character/Cerica/Smile.png'
say :cerica, "「じゃあ……そうね。あなたのこと、しばらく“ヒカリ”って呼ぶわ。 \nその札にちなんで。気に入らなかったら、あとで変えてもいいから」"
say :player, '「ヒカリ……」'
say :cerica, "「それじゃあ、よろしくね、ヒカリ。 \nこのオークションは、感情が落札に直結するわ。 \nお互い、恨みっこなしでよろしくね」"
narration '（感情が、落札に……？）'
narration '（分からないことばかり。でも、進むしかない）'
say :player, '「……うん。よろしく、セリカ」'
narration "（私たちは並んで歩き出す。 \n記憶のない私と、何かを知っている彼女。 \nこの先に、何が待っているんだろう）"
