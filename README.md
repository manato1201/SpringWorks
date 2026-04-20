# SpringWorks
春休みに行ったことを記録することを目的したリポジトリです

## [Unityで作成したshaderはこちら](./UnityShader.md)











# AnnomalyDetection — プロジェクトドキュメント

> BrainTQ チャットボットのセッション録音・モニタリング基盤

---

## 目次

1. [プロジェクト概要](#1-プロジェクト概要)
2. [システム全体構成](#2-システム全体構成)
3. [フォルダ構成](#3-フォルダ構成)
4. [Firebase 設定](#4-firebase-設定)
5. [Firestore データモデル](#5-firestore-データモデル)
6. [Web ダッシュボード（画面別説明）](#6-web-ダッシュボード画面別説明)
7. [Cloud Functions](#7-cloud-functions)
8. [Python クライアント](#8-python-クライアント)
9. [セキュリティルール](#9-セキュリティルール)
10. [セットアップ手順](#10-セットアップ手順)
11. [主要な設計ポイント](#11-主要な設計ポイント)

---

## 1. プロジェクト概要

| 項目 | 内容 |
|------|------|
| GCP プロジェクト | `anomalydetection-dev` |
| リージョン | `asia-northeast1`（東京） |
| Firebase サービス | Hosting / Firestore / Cloud Storage / Cloud Functions / Authentication |
| 主な用途 | BrainTQ チャットボットのセッション（会話録音・文字起こし）を記録・閲覧するダッシュボード |
| 認証方式 | Firebase Authentication（メール＋パスワード） |

このプロジェクトは **管理者向け Web ダッシュボード** です。  
チャットボット（Unity または Python クライアント）が会話セッションを Firestore に書き込み、その内容をブラウザ上で確認・再生することができます。

---

## 2. システム全体構成

```
┌─────────────────────────────────────────────────────────┐
│                   チャットボットクライアント              │
│  ┌──────────────────────┐  ┌──────────────────────────┐ │
│  │  Unity (BrainTQ_    │  │  Python CLI              │ │
│  │  Chatbot)           │  │  (live_chat_vertex.py)   │ │
│  └─────────┬────────────┘  └────────────┬─────────────┘ │
└────────────┼────────────────────────────┼───────────────┘
             │ セッションデータ書き込み    │ マイク音声→AI音声
             ▼                            ▼
┌────────────────────────────────────────────────────────┐
│                      Firebase / GCP                    │
│                                                        │
│  ┌─────────────────┐  ┌──────────────┐                │
│  │   Firestore     │  │ Cloud Storage│                │
│  │  sessions/      │  │ 音声ファイル │                │
│  │  {session_id}   │  │ (chunk wav)  │                │
│  └────────┬────────┘  └──────┬───────┘                │
│           │                  │                         │
│  ┌────────▼──────────────────▼───────┐                │
│  │       Firebase Hosting (SPA)       │                │
│  │  login → home → user → viewer     │                │
│  └───────────────────────────────────┘                │
│                                                        │
│  ┌───────────────────────────────────┐                │
│  │   Cloud Functions (asia-ne1)      │                │
│  │   helloWorld (疎通確認用)         │                │
│  └───────────────────────────────────┘                │
└────────────────────────────────────────────────────────┘
             │
             ▼
┌────────────────────────────────────────────────────────┐
│                  管理者ブラウザ                         │
│           Firebase Auth でログイン後閲覧                │
└────────────────────────────────────────────────────────┘
```

---

## 3. フォルダ構成

```
AnnomalyDetection/
├── firebase.json              # Firebase 設定（hosting / functions / firestore / storage）
├── firestore.rules            # Firestore セキュリティルール
├── firestore.indexes.json     # Firestore 複合インデックス定義
├── storage.rules              # Cloud Storage セキュリティルール
├── .firebaserc                # Firebase プロジェクト紐付け（anomalydetection-dev）
│
├── functions/                 # Cloud Functions (TypeScript)
│   ├── src/
│   │   └── index.ts           # 関数定義（現在は helloWorld のみ）
│   ├── lib/                   # TypeScript コンパイル後の JS
│   ├── package.json
│   └── tsconfig.json
│
├── public/                    # Firebase Hosting に公開される静的ファイル
│   ├── index.html             # セッション一覧画面（ホーム）
│   ├── login.html             # ログイン画面
│   ├── user.html              # ユーザー別セッション一覧
│   ├── viewer.html            # セッション詳細・会話内容再生
│   ├── css/
│   │   ├── home.css
│   │   ├── login.css
│   │   ├── user.css
│   │   └── viewer.css
│   └── js/
│       ├── home.js            # ホーム画面ロジック
│       ├── login.js           # ログイン処理
│       ├── user.js            # ユーザー詳細画面ロジック
│       ├── viewer.js          # セッション詳細・音声再生ロジック
│       └── shared/
│           ├── firebase-init.js  # Firebase SDK 初期化・エクスポート
│           └── auth.js           # 認証チェック・ログアウト共通処理
│
├── client-python/             # Python クライアント群
│   ├── smoke_test.py          # Cloud Functions への疎通テスト
│   ├── requirements.txt
│   └── chatbot-poc/
│       ├── live_chat_vertex.py  # Gemini Live音声チャット CLI
│       ├── requirements.txt
│       └── codex-memo.txt
│
└── client-web/                # Web クライアント（将来用、現在は空）
```

---

## 4. Firebase 設定

### `firebase.json` の主な設定

| サービス | 設定内容 |
|---------|---------|
| **Hosting** | `public/` フォルダを公開。SSR なし（静的 SPA）。全パスを `index.html` にリダイレクト（SPA 対応） |
| **Functions** | ランタイム Node.js 24、リージョン `asia-northeast1` |
| **Firestore** | `firestore.rules` / `firestore.indexes.json` を使用 |
| **Storage** | `storage.rules` を使用 |

### Firebase SDK 初期化（`firebase-init.js`）

```javascript
const firebaseConfig = {
    projectId: "anomalydetection-dev",
    authDomain: "anomalydetection-dev.firebaseapp.com",
    storageBucket: "anomalydetection-dev.firebasestorage.app",
    ...
};
```

- **Firestore**: IndexedDB に永続キャッシュ（`persistentLocalCache`）＋複数タブ対応（`persistentMultipleTabManager`）
- **Storage**: デフォルトバケット
- **Auth**: Firebase Authentication（メール＋パスワード）

---

## 5. Firestore データモデル

### コレクション: `sessions`

各ドキュメントはチャットボットの1セッション（会話1回分）を表します。

```
sessions/{session_id}
├── session_id: string         # セッションの一意 ID
├── uid: string                # ユーザー（患者）ID
├── client_started_at: string  # セッション開始時刻（ISO 8601）
├── client_ended_at: string    # セッション終了時刻（ISO 8601）
└── turns: array               # 会話ターン（発言）の配列
    └── [turn]
        ├── turn_index: int    # ターン番号（0始まり）
        ├── role: string       # "user" または "assistant"
        ├── text: string       # 発言テキスト（文字起こし）
        └── chunks: array      # 音声ファイルの分割チャンク
            └── [chunk]
                ├── chunk_index: int    # チャンク番号
                ├── start_ms: int       # 開始時間（ミリ秒）
                ├── end_ms: int         # 終了時間（ミリ秒）
                ├── upload_status: string  # "complete" または "failed"
                ├── storage_path: string   # Cloud Storage 上のパス
                └── last_error: string     # エラーメッセージ（失敗時）
```

### Firestore クエリ

| 用途 | クエリ |
|------|--------|
| 最新セッション10件取得 | `orderBy("client_started_at", "desc")` + `limit(10)` |
| UID 検索 | `where("uid", "==", uid)` + `orderBy("client_started_at", "desc")` |
| 期間フィルター（ユーザー詳細） | `where("uid", "==", uid)` + `where("client_started_at", ">=", from)` + `where("client_started_at", "<=", to)` |

> **注意**: `uid` + `client_started_at` の複合インデックスが `firestore.indexes.json` で定義されています。UID 検索の期間フィルターはこのインデックスを必要とします。

---

## 6. Web ダッシュボード（画面別説明）

### 画面遷移フロー

```
login.html
    │ ログイン成功
    ▼
index.html（ホーム：セッション一覧）
    │ UID リンクをクリック
    ├──────────────────────────────▶ user.html?uid={uid}（ユーザー詳細）
    │                                    │ 詳細リンクをクリック
    │                                    ▼
    │ 詳細リンクをクリック          viewer.html?uid={uid}&id={session_id}
    └──────────────────────────────▶ viewer.html?uid={uid}&id={session_id}
                                          （セッション詳細・音声再生）
```

---

### ログイン画面（`login.html` / `login.js`）

**機能**: Firebase Authentication のメール＋パスワードでログイン

- ログイン成功 → `index.html` へリダイレクト
- ログイン失敗 → 「ID またはパスワードが違います」を表示

---

### セッション一覧（`index.html` / `home.js`）

**機能**: 全ユーザーの最新セッションを一覧表示、UID 検索

| UI要素 | 説明 |
|--------|------|
| UID 検索フォーム | 特定ユーザーのセッションをフィルター |
| セッション一覧テーブル | 開始時刻 / セッションID / ユーザーID / 会話時間 / chunk状態 / 詳細リンク |
| UID リンク | クリックで `user.html` へ遷移 |
| 詳細リンク | クリックで `viewer.html` へ遷移 |

**キャッシュ**: sessionStorage に5分間キャッシュ（`latest_sessions` / `uid_sessions_{uid}`）

---

### ユーザー詳細（`user.html` / `user.js`）

**機能**: 特定ユーザーのセッション一覧＋統計情報＋期間フィルター

| 統計カード | 説明 |
|-----------|------|
| セッション数 | 該当期間のセッション合計 |
| 最終セッション | 一番新しいセッションの日時 |
| 初回セッション | 一番古いセッションの日時 |
| chunk 失敗 | 音声アップロードに失敗したセッション数 |

**期間フィルタープリセット**:
- 全期間 / 今日 / 直近7日 / 今月 / 先月

**キャッシュ**: `uid_sessions_{uid}_{from}_{to}` でキャッシュ（5分間）

---

### セッション詳細（`viewer.html` / `viewer.js`）

**機能**: 1セッションの詳細メタ情報＋ターン別会話内容＋音声再生

**メタ情報カード**:
| 項目 | 内容 |
|------|------|
| セッション ID | ドキュメントID |
| ユーザー ID | uid |
| 開始時刻 | `client_started_at` |
| 終了時刻 | `client_ended_at` |
| 会話時間 | 終了 - 開始（分:秒） |
| chunk 状態 | 全チャンク完了 / 失敗数 |

**会話内容（ターン表示）**:
- ユーザー発言（左）と AI 発言（右）をバブル形式で表示
- ユーザー発言ターンには音声チャンクテーブルを表示
- 各チャンクに `<audio>` プレイヤーを表示（Cloud Storage から署名付き URL を取得）
- 音声 URL の取得は `Promise.allSettled` で並列実行

**戻るリンク**:
- `uid` が URL パラメータにある場合 → `user.html?uid={uid}` へ戻る
- ない場合 → `index.html` へ戻る

---

### 共通処理（`shared/auth.js`）

全ページが `requireAuth()` を呼び出し、未ログインなら `login.html` にリダイレクト。  
ログアウトボタンは全ページ共通で `logout()` を呼び出す。

---

## 7. Cloud Functions

**ファイル**: `functions/src/index.ts`

| 関数名 | HTTPメソッド | 説明 |
|--------|------------|------|
| `helloWorld` | GET/POST | 疎通確認用。"Hello from Firebase Functions!" を返す |

```typescript
setGlobalOptions({ region: "asia-northeast1", maxInstances: 10 });
export const helloWorld = onRequest((req, res) => {
    res.send("Hello from Firebase Functions!");
});
```

> 現在は疎通確認のみ。今後、セッションデータの後処理・集計・通知などに活用予定。

---

## 8. Python クライアント

### 疎通テスト（`client-python/smoke_test.py`）

Cloud Functions の `helloWorld` エンドポイントに GET リクエストを送り、正常レスポンスを確認するスクリプト。

```bash
cd client-python
pip install -r requirements.txt
python smoke_test.py
```

---

### Gemini Live 音声チャット CLI（`client-python/chatbot-poc/live_chat_vertex.py`）

BrainTQ チャットボットの Python CLI 版実装。Unity クライアントと同等の機能を持つ。

#### 主な設定値

| パラメータ | 値 | 説明 |
|-----------|-----|------|
| `SEND_SAMPLE_RATE` | 16,000 Hz | マイク入力サンプリングレート |
| `RECEIVE_SAMPLE_RATE` | 24,000 Hz | スピーカー出力サンプリングレート |
| `CHUNK_SIZE` | 480 サンプル | 30ms 単位（webrtcvad 必須） |
| `VAD_MODE` | 2 | VAD 感度（0=低 〜 3=高） |
| `PRE_ROLL_CHUNKS` | 10 | 発話開始前のバッファ量（300ms） |
| `HANGOVER_CHUNKS` | 12 | 発話終了後の余韻時間（360ms） |
| `MODEL` | `gemini-2.5-flash-native-audio-preview-12-2025` | Gemini Live API モデル |

#### クラス構成

```
ListenAudio   マイク入力を取得し audio_queue_mic に積む
    ↓
SendAudio     VAD で発話区間を検出して Gemini Live に送信
    （PRE_ROLL で発話開始前の音声も含める）
    ↓
RecvAudio     Gemini Live からの返答音声を audio_queue_output に積む
    （interrupted が来たら再生バッファをクリア）
    ↓
PlayAudio     audio_queue_output から読み出してスピーカーに出力
```

#### 実行方法

```bash
cd client-python/chatbot-poc
pip install -r requirements.txt

# GCP ADC（Application Default Credentials）が必要
gcloud auth application-default login

python live_chat_vertex.py
```

> **注意**: `vertexai=False` になっているため、現在は Vertex AI ではなく Google AI（genai）を使用。Vertex AI を使う場合は `vertexai=True` に変更し、`PROJECT_ID` と `LOCATION` を設定する。

---

## 9. セキュリティルール

### Firestore

```
allow read: if request.auth != null;   // 認証済みユーザーのみ読み取り可
allow write: if false;                 // Web からの書き込みは不可
```

→ データ書き込みはチャットボットクライアント（サービスアカウント権限）のみが行う想定。

### Cloud Storage

```
allow read: if request.auth != null;   // 認証済みユーザーのみ音声ファイル取得可
allow write: if false;                 // Web からのアップロードは不可
```

---

## 10. セットアップ手順

### 前提条件

- Node.js 24+
- Firebase CLI（`firebase-tools`）がインストール済み
- `firebase login` でログイン済み
- GCP プロジェクト `anomalydetection-dev` へのアクセス権

### デプロイ

```bash
# プロジェクトルートで実行
cd AnnomalyDetection

# Functions のビルド
cd functions
npm install
npm run build
cd ..

# 全サービスをデプロイ
firebase deploy

# 個別デプロイ
firebase deploy --only hosting       # Web ダッシュボードのみ
firebase deploy --only functions     # Cloud Functions のみ
firebase deploy --only firestore     # Firestore ルール・インデックスのみ
firebase deploy --only storage       # Storage ルールのみ
```

### ローカル開発

```bash
# Firebase エミュレーターを起動
firebase emulators:start
```

---

## 11. 主要な設計ポイント

### SPA ルーティング
Firebase Hosting の `rewrites` で全パスを `index.html` にリダイレクトしているため、URL 直アクセスでも各画面が動作する。ただしこのプロジェクトでは各ページが独立した HTML ファイルなので、実質 MPA（Multi Page Application）に近い構成。

### オフライン対応（IndexedDB キャッシュ）
Firestore を `persistentLocalCache` で初期化しているため、一度取得したデータはブラウザの IndexedDB にキャッシュされる。ネットワーク切断中でも過去データを参照可能。

### sessionStorage キャッシュ（5分）
Firestore へのリクエスト数を抑えるため、JavaScript 側でも sessionStorage に5分間キャッシュしている。これはタブをまたがないキャッシュ（同一タブ内のみ有効）。

### 音声チャンクの並列取得
`viewer.js` では `Promise.allSettled` を使い、全チャンクの Cloud Storage 署名付き URL を並列取得してから画面描画する。一部チャンクの取得が失敗しても残りは表示される。

### ユーザー向け音声データは「ユーザー発言のみ」
`renderTurn()` でユーザーターンにのみ chunks テーブルを表示している。AI 発言の音声は現状保存・表示しない設計。

---

*最終更新: 2026-04-20*
