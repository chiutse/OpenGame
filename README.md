# Open Game
這個 Project 是利用 Windows語音辨識 (Windows Speech Recognition) 去辨識你的語音指令去打開你所想的遊戲/軟件.

# 設定
使用前需要設定好你可以打開的遊戲/軟件.
你可以打開 APP 內按 Search Game 去搜尋 STEAM 遊戲再做設定會比教方便.
主要設定你叫遊戲的叫法例如想開 Grand Theft Auto V 但只想叫 GTA 就要設定做 grammar.
但要記緊 grammar 不要重複使用在不同遊戲.
name : 遊戲名
steamAppid : Steam App Game ID
path : exe 位置
grammar : 你叫的遊戲名稱
設定 `appdata.json`
```
[
    {
      "name": "Black Mesa",
      "steamAppid": "362890",
      "path": null,
      "grammar": [
        "black mesa"
      ]
    },
    {
      "name": "Grand Theft Auto V",
      "steamAppid": "271590",
      "path": null,
      "grammar": [
        "GTA"
      ]
    },    
    {
        "name": "iTunes",
        "steamAppid": "",
        "path": "C:\\Program Files\\iTunes\\iTunes.exe",
        "grammar": [
          "iTunes"
        ]
    }
]
```

# 如何用
打開後叫系統內預設語音的名字再加指令和你自訂的遊戲名稱
假設我的系統內預設語音是 Tracy.
如果我想開玩 GTA
```
Tracy, 我想玩 GTA
```

如果我想開 iTunes
```
Tracy, 幫我開 itunes
```

關閉本程式
```
Tracy, 關閉
```

# 指令
執行指令
- 我想玩
- 幫我開

關閉本程式
- 關閉

# 限制
由於 Windows語音辨識還沒有廣東話支援.
所以用普通話會比較準確.