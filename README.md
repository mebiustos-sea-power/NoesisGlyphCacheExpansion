# Glyph Cache Expansion

Sea Powerの字形キャッシュを **4096×4096** に設定する、AnchorChainプラグインです。言語に関係なく適用されます。

字形キャッシュは、描画する文字の形を画像として保持する領域です。2048×2048と比べて画像領域の面積は4倍になります。キャッシュを拡大すると、環境によっては文字欠けが改善する可能性があります。このMODは文字欠けの解消を保証するものではありません。

日本語環境では、試作版を使ったキャッシュ拡大後に文字欠けが再発しないことを利用者が確認しました。中国語・韓国語や他の環境での効果、およびこのAnchorChain版の実ゲーム動作は未検証です。

## 導入

1. [AnchorChainの導入手順](https://seapower-modders.github.io/AnchorChain/install-anchor-chain.html)に従い、ACPreloaderを手動で導入します。BepInExはACPreloaderに含まれています。
2. [Anchor Chain](https://steamcommunity.com/sharedfiles/filedetails/?id=3380210757)を購読します。
3. Workshop公開後は、このMODを購読します。ローカル版を使う場合は、配布フォルダ `NoesisGlyphCacheExpansion` を `Sea Power_Data/StreamingAssets` に配置します。
4. ゲーム内のMOD管理で **「Glyph Cache Expansion」** を有効にし、ゲームを完全に終了して再起動します。

AnchorChainはMOD管理のチェック状態とは別にDLLを読み込みます。このプラグインは起動時に自身のチェック状態を確認し、有効な場合だけキャッシュ設定を変更します。有効・無効の変更後は再起動してください。

無効にして再起動すると、ゲーム本来のキャッシュ設定で起動します。

## 動作

- `NoesisSettings.Get()`の戻り値にある `glyphTextureSize` だけを変更します。導入済みNoesis DLLでは、この値がビューの生成前に読み込まれ、ネイティブ描画側へ4096×4096が渡されます。
- 変更は実行中の設定へ適用します。設定アセットの保存や、生成済み描画キャッシュの再初期化は行いません。
- 画面・テキストの走査、スクリーンショット採取、字形の詳細ログ、外部通信はありません。
- 起動・適用・エラーの記録は `BepInEx/LogOutput.log` に出ます。
- 他の日本語化MODやキャンペーンMODを必要としません。

開発時の参照環境は Sea Power **0.8.6-261014**、NoesisGUI **4.0.0.18026**、AnchorChain **1.1.0** です。ゲーム更新で対象APIが変わった場合はエラーとして記録します。ビルドと単体検証は、ゲーム内の動作確認を代替しません。

## 開発・ビルド

1. .NET SDKを用意します。本体の対象は `netstandard2.1` です。付属の単体検証には.NET 10 SDKとランタイムを使います。
2. `Directory.Build.local.props.example` を `Directory.Build.local.props` にコピーし、Sea PowerとAnchorChainのフォルダを指定します。
3. Windows PowerShellから実行します。

```powershell
.\build.ps1
.\build.ps1 -Test
.\build.ps1 -Test -InstallLocal
```

フォルダを引数で指定することもできます。

```powershell
.\build.ps1 -GameDirectory 'N:\Games\Steam\steamapps\common\Sea Power' -AnchorChainDirectory 'N:\Games\Steam\steamapps\workshop\content\1286220\3380210757' -Test -InstallLocal
```

NuGetパッケージは使いません。ゲームとローダーのDLLは利用者の環境を参照し、配布物には同梱しません。`NuGet.Config`ではパッケージ取得元を空にしています。

単体検証は、サイズ変更が他の設定に影響しないこと、繰り返し適用、設定欠落時のエラー、MODの有効・無効判定を確認します。UnityのネイティブAPIやゲーム起動は行いません。

## Workshop公開

- ビルド済み配布フォルダ: `dist/NoesisGlyphCacheExpansion`
- `-InstallLocal` による配置先: `Sea Power_Data/StreamingAssets/NoesisGlyphCacheExpansion`
- Workshopのタイトル案: **Glyph Cache Expansion**
- タグ: **Scripting**
- 必要アイテム: **Anchor Chain**（Workshop ID `3380210757`）
- 説明文案: `WORKSHOP_DESCRIPTION.md`

Sea PowerのMOD作成・提出画面から、ローカルMODフォルダを選んで公開します。公開時のプレビュー画像はその画面で指定してください。必要アイテムはWorkshopの編集画面でAnchor Chainを追加します。

## 参照

- [AnchorChain開発者向け資料](https://seapower-modders.github.io/AnchorChain/modder-documentation.html)
- [Noesisの字形キャッシュに関する説明](https://www.noesisengine.com/bugs/view.php?id=2514)
