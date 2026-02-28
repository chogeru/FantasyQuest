# FantasyQuest コーディング＆運用ガイドライン

大規模開発に向けて、チーム全員が守るべき**10の改善ルールと仕組み**を定義します。

## 1. プレハブ・バリアント（Prefab Variant）の徹底利用
敵キャラやアイテムなど、パラメータのみが異なる類似オブジェクトを作成する場合は、ベースとなるプレハブから「Prefab Variant」を作成してください。元のPrefabを修正した際に全体へ一括適用されるため、変更への強さが劇的に向上します。

## 2. Addressablesの導入とリソース動的ロード化
ゲーム内で動的ロードする素材は全て `AddressableAssets/` に入れて管理します。将来的には設定漏れを防ぐため「指定フォルダ内のアセットを自動でAddressablesグループに割り当てる」監視用エディタスクリプトの連携を推奨します（Addressablesパッケージ導入後に構築）。

## 3. Singleton ScriptableObjectの実装 (✨実装済み)
ゲーム中で常にアクセスされる設定・管理クラスについて、シーンに不要なGameObjectを置くのは避けてください。
プロジェクトに用意されている基底クラス `Project.Core.Managers.ScriptableSingleton<T>` を継承し、`Resources/` 等からロードされるアセットとして運用してください。

## 4. テストシーンの隔離ルール (✨構築済み)
新機能の検証や個人の実験には、本番シーンを使わず `Assets/_Project/Tests/Playground/` 以下の個人用ディレクトリ等にテストシーンを作成してください。本番シーンが予期せず破壊される（コンフリクトする）ことを防ぎます。

## 5. 命名規則ツールによるチェック (✨実装済み)
アセットのインポート時に自動で動く `NamingValidator.cs` を設定しています。
_Project 以下の自作スクリプト等を追加する際は、必ず **パスカルケース（PascalCase: 大文字始まりの単語連結）** となるよう遵守してください。違反するとConsoleに警告が出力されます。

## 6. Project Settings や Packages の正しいGit管理 (✨構築済み)
最適化された `.gitignore` により、LibraryやTempなど不要なファイルはコミットされません。
反対に `ProjectSettings/` や `Packages/manifest.json` は全チームでの同期が必須のため、変更があった場合は漏れなくコミットに含めてください。

## 7. カスタムScene移動メニューの利用 (✨実装済み)
Unityエディタ上部のメニューから `Scenes` > `Quick Load` と進むと、主要なシーン（Title, Game, Playground等）へワンクリックでジャンプ・ロードできます。開発中のシーン切り替えは極力こちらを利用してください。

## 8. Odin Inspector等のEditor補助ツールの活用
データ入力やデバッグを容易にするため、ThirdParty内に配置するインスペクタ拡張ツール（Odin Inspectorなど）を積極的に活用し、プランナー等でも使いやすい管理画面を作ってください。

## 9. 「使用していないアセット」の定期クリーンアップ
大規模化するに従い、不要なThirdPartyアセットや一時モデルがGB単位で溜まります。月に1回など定期的にAsset Hunter等を用いてデッドアセットを一掃するフローを実行してください。

## 10. `RequireComponent` 属性の徹底
自作のMonoBehaviour内で他のコンポーネント（RigidbodyやColliderなど）が動作に必須となる場合、必ずクラスの先頭に `[RequireComponent(typeof(T))]` などの属性を記述し、アタッチ忘れによるNullエラーを未然に防いでください。
