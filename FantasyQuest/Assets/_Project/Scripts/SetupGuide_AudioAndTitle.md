# タイトル画面とオーディオ管理機能のセットアップガイド

自動で作成されたスクリプトを利用して、タイトル画面とオーディオシステム（Odin・多重SOロード対応版）をセットアップする手順です。

## 1. オーディオシステム (AudioManager) の設定
このシステムは一度ロードされるとアプリ終了まで消えずにBGM/SEを管理します(DontDestroyOnLoad)。また、不要な音源メモリを節約するための仕組みを備えています。

1. **共通の AudioDataConfig を作成 (Common Config)**
   - UnityのProjectウィンドウで右クリック -> `Create` -> `Project` -> `Audio` -> `Audio Data Config` を選択します。
   - 作成されたファイル名を `CommonAudioConfig` などにします。
   - インスペクター(Odin対応)から `Bgm Dictionary` と `Se Dictionary` に要素を追加し、常に使う音源（UI操作音など）を設定します。

2. **AudioManager プレハブの作成 (または最初のシーンへの配置)**
   - 空の GameObject を作成し、名前を `AudioManager` にします。
   - `Project.Systems.Audio.AudioManager` スクリプトをアタッチします。
   - インスペクターの `Common Config` に、先ほど作成した `CommonAudioConfig` をアタッチします。
   - この `AudioManager` をProjectウィンドウにドラッグしてプレハブ化し、最初のシーン(タイトルシーンなど)に配置しておくと便利です。

3. **シーン固有の音源を節約して運用する (Audio Config Loader)**
   - 各シーンごとに固有のBGMやSEがある場合、専用の `AudioDataConfig` を作成します（例：`TitleAudioConfig`, `BattleAudioConfig`）。
   - シーン内の適当なオブジェクト（専用の空オブジェクトでも可）に `Project.Systems.Audio.AudioConfigLoader` をアタッチします。
   - その `Scene Audio Config` に固有の設定ファイル（例：`TitleAudioConfig`）をセットします。
   - これで、そのシーンがロードされた時に自動で音源が登録され、アンロードされた時に自動で解除されます。
   ```csharp
   // BGMの再生 (IDを指定)
   Project.Systems.Audio.AudioManager.Instance.PlayBGM("TitleBGM");
   
   // SEの再生 (IDを指定)
   Project.Systems.Audio.AudioManager.Instance.PlaySE("SubmitSE");
   ```

---

## 2. タイトル画面のセットアップ
タイトル画面のUIと遷移を処理するマネージャーです。

1. **UIキャンバスの作成**
   - 新しいシーン(名前: TitleScene)を作成します。
   - `Canvas` を作成し、その中に以下の2つの空の GameObject を作成します。
     - `PressAnyButtonContainer` (「Press Any Button」のテキスト等を配置する親)
     - `MainMenuContainer` (各メニューボタンを配置する親。最初は非アクティブに設定)

2. **ボタンの配置**
   - `MainMenuContainer` の下に、「New Game」「Continue」「Options」「Quit」の4つのUI Buttonを配置します。

3. **マネージャーの配置**
   - 空の GameObject を作成し、名前を `TitleManager` にします。
   - `TitleSceneManager` と `TitleUIController` の両方をアタッチします。

4. **アタッチの紐付け**
   - **Title UI Controller**
     - `Press Any Button Container` と `Main Menu Container` に手順1で作成したオブジェクトをアタッチ。
     - `New Game Button` ～ `Quit Button` に手順2で作成したボタンをアタッチ。
   - **Title Scene Manager**
     - `Title UI Controller` に同じオブジェクトをアタッチ。
     - `Input Reader` に、既存の `InputReader` のScriptableObjectのアセットをアタッチします。(これによりジャンプやアタックボタン押下で「Push Button」から「メニュー画面」に遷移します)

以上でセットアップが完了します。プレビュー再生し、ボタン押下でメニューが開き、各ボタンでコンソールにログが出力されることを確認してください。
