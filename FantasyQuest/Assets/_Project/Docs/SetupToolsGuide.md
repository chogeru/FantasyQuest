# FantasyQuest セットアップツール ガイド

このドキュメントでは、ゲーム開発の効率化のために作成された専用の「セットアップツール」と「スポーンシステム」の使い方を解説します。
これらのツールを利用することで、数日かかるような面倒なコンポーネントやアニメーションの設定、敵の配置作業が数秒〜数分で完了します。

---

## 1. Enemy Setup Wizard (敵キャラクター全自動セットアップ)

水・陸・空、あらゆる環境に対応した敵キャラクターのコンポーネント一式とアニメーション、さらには衝突・被弾判定までをワンボタンで構築し、Prefab化する最強のツールです。

### 起動方法
Unity上部のメニューバーから **`FantasyQuest > Tools > Enemy Setup Wizard`** をクリック。

### 使い方
1. **1. Basic Settings (基本設定)**
   - `Enemy Name`: 作成する敵の名前を入力します。（例: Slime, FlyingEye など）
   - `Movement Type`: `Land`(陸), `Water`(水生), `Air`(飛行) から環境に合った挙動タイプを選択します。これにより、重力や障害物回避のAIが自動で切り替わります。
   - `Model Prefab`: （任意）表示したい3DモデルのPrefabをセットします。セットすると自動で子要素に配置されます。
2. **2. Animation Foundation (アニメーション基盤設定)**
   - **Base Controller 自動生成**: 初めて敵を作る場合や、プロジェクトにベースとなる `AnimatorController` が無い場合は、ここにある「自動生成」ボタンを押します。これで全敵共通のステートマシンが構築された `BaseEnemyController` が作成されます。
   - 作成された（またはセットされた）ベースのコントローラーに対して、各種モーション (`Idle`, `Move`, `Attack`, `Stagger`, `Die`) のアニメーションクリップをセットします。
3. **🚀 Create & Setup Enemy 🚀 (実行)**
   - 全て設定してボタンを押すと、以下の処理が完全自動で走ります。
     - **Animator Override Controller の作成**: 基盤にセットしたクリップを上書きした専用のコントローラーファイルが `Assets/_Project/Animations/Enemies/敵の名前/` に自動作成＆割り当てされます。
     - **必須コンポーネントの付与**: AI制御（NavMeshAgent含む）、ステータス（CharacterStats）、アニメーターへの自動アクセスが設定されます。
     - **戦闘モジュールの接続**: `Hurtbox` と `CapsuleCollider` が自動付与・調整され、`OnTakeDamage` イベントが自動的にHPを減らす処理へバインドされます。
     - **攻撃判定の設置**: 前方に子オブジェクトとしてMelee用 `Hitbox` と `SphereCollider(IsTrigger)` が設置されます。
     - **プレハブ化**: 完成したオブジェクトが `Assets/_Project/Prefabs/Enemies/` に自動で保存され、配置可能なPrefabになります。

---

## 2. Player Animator Setup Tool (プレイヤーアニメーション基盤構築)

プレイヤー向けの複雑な Animator Controller（Locomotionのブレンドツリー構築や、ジャンプ・落下・水泳・攻撃のステート遷移）を自動で組み上げるツールです。

### 起動方法
Unity上部のメニューバーから **`FantasyQuest > Tools > Create Player Animator Controller`** をクリック。

### 使い方
1. Inspector（または表示された専用のウィザードウィンドウ）にて、各行動ごとの `AnimationClip` をセットします。
   - **Locomotion**: 待機(Idle)、歩き(Walk)、走り(Sprint) のクリップ（これらは自動的に1D BlendTreeとして合成されます）
   - **Air**: ジャンプ上昇、落下のクリップ（Y軸の速度によってブレンドループします）
   - **Swimming**: 水泳中のクリップ
   - **Combat**: 基本攻撃のクリップ
2. ウィザード右下の **`Create Controller`** ボタンを押します。
3. 保存先のダイアログが出るので、任意の場所（例: `Assets/_Project/Animations/Player/` など）に保存します。
4. 全てのパラメータ（Speed, IsGrounded, IsSwimming, Jump...etc）と複雑なトランジション設定が完了済みの `AnimatorController` が瞬時に生成されます。そのままプレイヤーの Animator にアタッチしてください。

---

## 3. Spawning System (敵の配置・自動接地・環境抽選システム)

マップ上に敵を直感的に配置し、夜や雨などの環境変化に合わせた動的なスポーンを実現するシステム群です。使用する際は `Assets/_Project/Prefabs/Enemies/` で生成した敵Prefabを活用します。

### 個別に配置する: `EnemySpawnPoint`
単体の敵を出現させるポイントです。空のゲームオブジェクトにアタッチして使います。
- **Enemy Prefab**: 固定で出現させたい敵のPrefab。
- **Snap To Ground On Spawn**: オンにすると、ゲーム実行時に真下の地面を自動で探索し、空中に浮いたり地面に埋まったりしないように自動でY軸を最適化（接地）します。（※飛行・水生敵はオフを推奨します）
- **視覚化**: シーンビュー上で赤い玉と向いている方向の矢印が描画されるため、インスペクターを見なくても「どこに」「どの向きで」敵が出るかが一目でわかります。

### エリア単位で配置する: `EnemySpawner`
複数の `EnemySpawnPoint` を子オブジェクトに配置し、プレイヤーが近づいた瞬間に一斉に敵を出現させる「エリアマネージャー」です。
- **Trigger Radius**: 検知範囲。緑色の大きなワイヤーフレームの球で表示されます。プレイヤーがこの円に入ると子要素のSpawnPointが一斉に起動します。
- **Spawn Once**: 一度涌いたらそれきりか、全滅後にもう一度入ったら再湧きさせるかを切り替えられます。

### 環境によって敵を変える: `SpawnTable` と `EnvironmentManager`
時間帯（昼/夜）や天候（晴/雨/霧）に応じて出現する敵の確率を変える機能です。
1. **Spawn Table の作成**: プロジェクトウィンドウで 右クリック > `Create > FantasyQuest > Spawning > Spawn Table` を選択。
2. 作成したデータファイルの中身を設定し、例えば「昼・晴の時はゴブリン(Weight:90), スライム(Weight:10)」、「夜の時はアンデッド(Weight:100)」といった抽選グループ（ガチャ確率）を設定します。
3. 作成した `Spawn Table` を、`EnemySpawnPoint` の `Dynamic Spawn Table` 項目にドラッグ＆ドロップします。
4. ゲーム実行時、グローバルに存在する `EnvironmentManager` の現在の環境ステータスを読み取り、条件に合致したグループの中から重み付け抽選（確率はWeightの比率）を行い、当たった敵を自動でSpawn（出現）させます。
