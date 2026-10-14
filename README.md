# Glyph Cache Expansion

Sea Powerの字形キャッシュを **4096×4096** に設定する、AnchorChainプラグインです。言語に関係なく適用されます。

字形キャッシュは、描画する文字の形を画像として保持する領域です。2048×2048と比べて画像領域の面積は4倍になります。キャッシュを拡大すると、環境によっては文字欠けが改善する可能性があります。このMODは文字欠けの解消を保証するものではありません。

## 導入

1. [導入手順](https://seapower-modders.github.io/AnchorChain/install-anchor-chain.html)に従って、[Anchor Chain](https://steamcommunity.com/sharedfiles/filedetails/?id=3380210757)を導入します。
2. [Releases](https://github.com/mebiustos-sea-power/NoesisGlyphCacheExpansion/releases/latest)から配布ZIPをダウンロードして展開します。
3. ZIP内の `NoesisGlyphCacheExpansion` フォルダを、Sea Powerの `Sea Power_Data/StreamingAssets` に配置します。
4. ゲーム内のMOD管理で **「Glyph Cache Expansion」** を有効にし、ゲームを完全に終了して再起動します。

AnchorChainはMOD管理のチェック状態とは別にDLLを読み込みます。このプラグインは起動時に自身のチェック状態を確認し、有効な場合だけキャッシュ設定を変更します。有効・無効の変更後は再起動してください。

無効にして再起動すると、ゲーム本来のキャッシュ設定で起動します。

## 動作

- `NoesisSettings.Get()`の戻り値にある `glyphTextureSize` だけを変更します。導入済みNoesis DLLでは、この値がビューの生成前に読み込まれ、ネイティブ描画側へ4096×4096が渡されます。
- 変更は実行中の設定へ適用します。設定アセットの保存や、生成済み描画キャッシュの再初期化は行いません。
- 画面・テキストの走査、スクリーンショット採取、字形の詳細ログ、外部通信はありません。
- 必要なAPIが利用できない場合や、初期化・設定変更に失敗した場合は、原因をエラーログに記録し、再起動まで拡大処理を停止します。MOD側の例外はゲームやAnchorChainへ伝播させません。
- 起動・適用・エラーの記録は `BepInEx/LogOutput.log` に出ます。
- 他の日本語化MODやキャンペーンMODを必要としません。

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
.\build.ps1 -GameDirectory 'C:\path\to\Sea Power' -AnchorChainDirectory 'C:\path\to\Anchor Chain' -Test -InstallLocal
```

NuGetパッケージは使いません。ゲームとローダーのDLLは利用者の環境を参照し、配布物には同梱しません。`NuGet.Config`ではパッケージ取得元を空にしています。

単体検証は、サイズ変更が他の設定に影響しないこと、繰り返し適用、設定欠落時のエラー、MODの有効・無効判定、例外の捕捉と失敗後の処理停止を確認します。UnityのネイティブAPIやゲーム起動は行いません。

## 参照

- [AnchorChain開発者向け資料](https://seapower-modders.github.io/AnchorChain/modder-documentation.html)
- [Noesisの字形キャッシュに関する説明](https://www.noesisengine.com/bugs/view.php?id=2514)
