// ignore: unused_import
import 'package:intl/intl.dart' as intl;
import 'app_localizations.dart';

// ignore_for_file: type=lint

/// The translations for Japanese (`ja`).
class AppLocalizationsJa extends AppLocalizations {
  AppLocalizationsJa([String locale = 'ja']) : super(locale);

  @override
  String get appTitle => 'Liar Hub';

  @override
  String get navConsole => 'コンソール';

  @override
  String get navSettings => '設定';

  @override
  String get rename => '名前を変更';

  @override
  String get cancel => 'キャンセル';

  @override
  String get copy => 'コピー';

  @override
  String get remove => '削除';

  @override
  String get save => '保存';

  @override
  String get clear => 'クリア';

  @override
  String get notConfigured => '未設定、クリックして編集';

  @override
  String get noFunctions => '利用可能な機能がありません';

  @override
  String get configureFirst => '先に設定を完了してください';

  @override
  String get pathHint => 'パス';

  @override
  String get optionHint => 'オプション';

  @override
  String get skipOption => '（スキップ）';

  @override
  String get processing => '処理中...';

  @override
  String get sectionShell => 'シェル';

  @override
  String get sectionAppearance => '外観';

  @override
  String get sectionAbout => 'このアプリについて';

  @override
  String get sectionPermissions => '権限設定';

  @override
  String get sectionUpdate => '更新設定';

  @override
  String get kernelPath => 'カーネルファイルのパス';

  @override
  String get scriptDir => 'スクリプトディレクトリ';

  @override
  String get themeTitle => 'テーマ';

  @override
  String get themeMode => 'テーマモード';

  @override
  String get themeColor => 'テーマカラー';

  @override
  String get dynamicColor => 'ダイナミックカラー（Monet）';

  @override
  String get lightMode => 'ライトモード';

  @override
  String get darkMode => 'ダークモード';

  @override
  String get systemMode => 'システム設定に従う';

  @override
  String get seedDeepPurple => 'ディープパープル';

  @override
  String get seedIndigo => 'インディゴ';

  @override
  String get seedBlue => 'ブルー';

  @override
  String get seedTeal => 'ティール';

  @override
  String get seedGreen => 'グリーン';

  @override
  String get seedOrange => 'オレンジ';

  @override
  String get seedPink => 'ピンク';

  @override
  String get seedRed => 'レッド';

  @override
  String get seedCustom => 'カスタム';

  @override
  String get amoledMode => 'AMOLED モード';

  @override
  String get amoledModeDesc => '純黒の背景でバッテリーを節約';

  @override
  String get fontSetting => 'フォント';

  @override
  String get fontSettingDesc => 'タップしてフォントファイルのパスを設定（空欄でシステム既定）';

  @override
  String get fontDialogTitle => 'フォントパスを設定';

  @override
  String get fontPathHint => 'フォントファイルのパスを入力（空欄=システム既定）';

  @override
  String get fontPathDefault => 'システム既定';

  @override
  String get textSize => '文字サイズ';

  @override
  String get textSizeDesc => '全体の文字サイズを調整';

  @override
  String get layoutSettings => 'レイアウト';

  @override
  String get switchStyleTitle => 'スイッチスタイル';

  @override
  String get switchStylePixel => 'Pixel';

  @override
  String get switchStyleMaterial => 'Material You';

  @override
  String get switchStyleToggleDesc => 'オン: Pixel スタイル。オフ: Material You。';

  @override
  String get aboutApp => 'このアプリについて';

  @override
  String get version => 'バージョン 1.0.7';

  @override
  String get language => '言語';

  @override
  String get languageDialogTitle => '言語を選択';

  @override
  String get langFollowSystem => 'システムに従う';

  @override
  String get langZh => '簡体字中国語';

  @override
  String get langEn => '英語';

  @override
  String get langJa => '日本語';

  @override
  String get langKo => '韓国語';

  @override
  String get langVi => 'ベトナム語';

  @override
  String get langRu => 'ロシア語';

  @override
  String get executorImpl => 'エグゼキューターの実装';

  @override
  String get executorDialogTitle => 'エグゼキューターを選択';

  @override
  String editDialogTitle(String label) {
    return '$label を編集';
  }

  @override
  String get inputPathHint => 'パスを入力...';

  @override
  String cannotOpenLink(String url) {
    return 'リンクを開けません: $url';
  }

  @override
  String get aboutTitle => 'このアプリについて';

  @override
  String get aboutTagline => '永遠に無料・オープンソースのツールアプリ — すべての人が使う権利を持っています！';

  @override
  String get sectionProject => 'プロジェクト';

  @override
  String get sectionDevelopers => '開発者';

  @override
  String get sectionCredits => 'クレジット';

  @override
  String get githubSource => 'GitHub ソースコード';

  @override
  String get githubSourceDesc => 'プロジェクトの閲覧・フォーク・コントリビュートはこちら';

  @override
  String get issueTracker => '問題とフィードバック';

  @override
  String get issueTrackerDesc => 'バグの報告や機能のご提案はこちら';

  @override
  String get mainDeveloper => 'メイン開発者';

  @override
  String get creditDesc1 => 'C# による実装。本プロジェクトの C++ 実装における重要な参考資料を提供';

  @override
  String get creditDesc2 => 'C++ による実装。本プロジェクトに重要な機能アイデアを提供';

  @override
  String get license => 'ライセンス: AGPL-3.0';

  @override
  String get copyright => '© 2026 Liar ToolKit Studio';

  @override
  String get loadingExecutors => '読み込み中...';

  @override
  String get noExecutors => 'エグゼキューターが見つかりません';

  @override
  String get batchModeOn => 'バッチ処理';

  @override
  String get batchModeOff => '単一ファイル';

  @override
  String get labTitle => 'Lab';

  @override
  String get pluginDrawerTitle => 'プラグイン';

  @override
  String get search => '検索';

  @override
  String get labEmptyHint => 'ドロワーからプラグイン機能を選択してください';

  @override
  String get startProcess => '開始';

  @override
  String get paramPathDesc => 'パスパラメータを選択または入力します。';

  @override
  String get paramStringDesc => 'テキストパラメータを入力します。';

  @override
  String get paramIntegerDesc => '整数パラメータを入力します。';

  @override
  String get paramBooleanDesc => '真偽値パラメータを切り替えます。';

  @override
  String get paramListDesc => 'リストから値を選択します。';

  @override
  String get paramMapDesc => 'マッピングされた値を選択します。';

  @override
  String get shellCardTitle => 'Shell';

  @override
  String get shellCardSubtitle => 'タスクとコマンドを簡単に実行。';

  @override
  String get labCardSubtitle => '界面に感情を宿す。';

  @override
  String get storagePermission => 'ストレージ';

  @override
  String get storagePermissionDesc => 'タップしてシステムのストレージ権限画面を開く';

  @override
  String get checkUpdateOnStartup => '起動時に更新を確認';

  @override
  String get checkUpdateOnStartupDesc => 'アプリ起動時に新しいリリースを自動的に確認';

  @override
  String get repoMirror => 'リポジトリミラー';

  @override
  String get repoMirrorDesc => 'ダウンロードに GitHub ミラープロキシを使用';

  @override
  String get repoMirrorNone => '直接接続 (ミラーなし)';

  @override
  String get updatePageTitle => '更新';

  @override
  String newVersionFound(String version) {
    return '新バージョン $version が利用可能';
  }

  @override
  String get prereleaseSuffix => ' (プレリリース)';

  @override
  String publishDate(String date) {
    return '公開日：$date';
  }

  @override
  String get remindLater => '後で通知';

  @override
  String get viewDetails => '詳細を見る';

  @override
  String get updateNow => '今すぐ更新';

  @override
  String get connecting => '接続中…';

  @override
  String get downloadingUpdate => '更新をダウンロード中…';

  @override
  String get extracting => '展開中…';

  @override
  String get installingFiles => 'ファイルをインストール中…';

  @override
  String updateFailed(String error) {
    return '更新に失敗しました: $error';
  }

  @override
  String get updateComplete => '更新完了';

  @override
  String get updateCompleteMessage => 'ファイルを置き換えました。新しい APK をインストールしてください。';

  @override
  String get installNow => '今すぐインストール';

  @override
  String cannotOpenApk(String message) {
    return 'インストーラーを開けません: $message';
  }
}
