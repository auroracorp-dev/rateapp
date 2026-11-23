# RateApp Web API

RateApp は、建物を登録し、レビューを投稿・閲覧するための Web API です。登録は招待制で、ログイン後はレビューへの「いいね」などの機能を利用できます。

## 主な機能

- 招待コードを使ったユーザー登録と、端末のシリアル番号を使ったログイン
- 建物とレビューの登録・検索・更新・削除
- レビューへの「いいね」の追加・取り消し
- ユーザーの権限に応じた招待コードやアカウントの管理

## 開発環境

- .NET 8 SDK
- PostgreSQL
- ASP.NET Core Web API、Entity Framework Core（Npgsql）、Dapper
- JWT Bearer 認証、Swagger UI

## ローカルで起動する

### 1. PostgreSQL を設定する

アプリのテーブルが作成済みの開発用データベースを用意します。リポジトリにはマイグレーションや初期データが含まれていないため、空のデータベースを使う場合は事前にスキーマを作成してください。ログインの確認には登録済みのユーザー、ユーザー登録の確認には未使用の招待コードが必要です。

接続文字列は環境変数 `ConnectionStrings__DefaultConnection`、JWT の署名鍵は `Jwt__Key` で設定します。以下は Bash での設定例です。接続先と認証情報は手元の環境に合わせて変更してください。

```bash
export ConnectionStrings__DefaultConnection='Host=localhost;Port=5432;Database=rateapp;Username=rateapp;Password=CHANGE_ME'
export Jwt__Key="$(openssl rand -base64 32)"
```

### 2. API を起動する

リポジトリのルートで実行します。

```bash
dotnet dev-certs https --trust
dotnet run --project WebAPI/WebAPI.csproj --launch-profile https
```

開発環境の Swagger UI は [https://localhost:7199/swagger](https://localhost:7199/swagger) で開けます。

### 3. Swagger UI で認証する

1. `POST /api/auth/login` に登録済みユーザーの端末シリアル番号を送信します。リクエスト例は `{"deviceSN":"YOUR_DEVICE_SERIAL"}` です。
2. レスポンスの `token` をコピーし、Swagger UI の **Authorize** に `Bearer <token>` の形式で入力します。
3. `GET /api/buildings/getall` など、認証が必要な API を実行します。

## 主なエンドポイント

| メソッド | パス | 内容 |
| --- | --- | --- |
| POST | `/api/users/add` | 招待コードを使ってユーザーを登録 |
| POST | `/api/auth/login` | JWT を取得 |
| GET | `/api/buildings/getall` | 建物の一覧を取得 |
| GET | `/api/buildings/get/{id}` | 建物の詳細を取得 |
| POST | `/api/buildings/add` | 建物を登録 |
| GET | `/api/reviews/getall` | レビューの一覧を取得 |
| POST | `/api/reviews/add` | レビューを投稿 |
| POST | `/api/likes/toggle` | レビューへの「いいね」を追加・取り消し |
| GET | `/api/invites/get` | 自分に割り当てられた招待コードを取得 |
| POST | `/api/invites/manager/add/{count}` | 管理権限で招待コードを発行 |

各 API のリクエスト項目とレスポンス形式は Swagger UI で確認できます。

## ディレクトリ構成

| パス | 内容 |
| --- | --- |
| `WebAPI.sln` | Visual Studio ソリューション |
| `WebAPI/Program.cs` | API の起動設定、認証、依存関係の登録 |
| `WebAPI/Controllers/` | HTTP エンドポイント |
| `WebAPI/Manager/` | 業務ロジックとデータアクセス |
| `WebAPI/Context/` | Entity Framework Core のモデル設定 |
| `WebAPI/Models/` | エンティティとリクエストモデル |
| `WebAPI/00.Helper/` | DTO、マッピング、共通処理 |
