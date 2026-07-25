// ignore: unused_import
import 'package:intl/intl.dart' as intl;
import 'app_localizations.dart';

// ignore_for_file: type=lint

/// The translations for Chinese (`zh`).
class AppLocalizationsZh extends AppLocalizations {
  AppLocalizationsZh([String locale = 'zh']) : super(locale);

  @override
  String get appTitle => 'Liar Hub';

  @override
  String get navConsole => '控制台';

  @override
  String get navSettings => '设置';

  @override
  String get rename => '重命名';

  @override
  String get cancel => '取消';

  @override
  String get copy => '复制';

  @override
  String get remove => '移除';

  @override
  String get save => '保存';

  @override
  String get clear => '清空';

  @override
  String get notConfigured => '未配置，点击编辑';

  @override
  String get noFunctions => '暂无可用功能';

  @override
  String get configureFirst => '请先完成配置';

  @override
  String get pathHint => '路径';

  @override
  String get optionHint => '选项';

  @override
  String get skipOption => ' (跳过) ';

  @override
  String get processing => '处理中...';

  @override
  String get sectionShell => 'Shell';

  @override
  String get sectionAppearance => '外观';

  @override
  String get sectionAbout => '关于';

  @override
  String get sectionPermissions => '权限设置';

  @override
  String get sectionUpdate => '更新设置';

  @override
  String get kernelPath => 'Kernel 文件路径';

  @override
  String get scriptDir => 'Script 脚本目录';

  @override
  String get themeTitle => '主题';

  @override
  String get themeMode => '主题模式';

  @override
  String get themeColor => '主题色';

  @override
  String get dynamicColor => '莫奈取色';

  @override
  String get lightMode => '浅色模式';

  @override
  String get darkMode => '深色模式';

  @override
  String get systemMode => '跟随系统';

  @override
  String get seedDeepPurple => '深紫';

  @override
  String get seedIndigo => '靛蓝';

  @override
  String get seedBlue => '蓝色';

  @override
  String get seedTeal => '青绿';

  @override
  String get seedGreen => '绿色';

  @override
  String get seedOrange => '橙色';

  @override
  String get seedPink => '玫红';

  @override
  String get seedRed => '红色';

  @override
  String get seedCustom => '自定义';

  @override
  String get amoledMode => 'AMOLED 模式';

  @override
  String get amoledModeDesc => '使用纯黑背景以节省电量';

  @override
  String get fontSetting => '文字字体';

  @override
  String get fontSettingDesc => '点击设置字体文件路径（留空使用系统默认）';

  @override
  String get fontDialogTitle => '设置字体路径';

  @override
  String get fontPathHint => '输入字体文件路径（留空使用系统默认）';

  @override
  String get fontPathDefault => '系统默认';

  @override
  String get textSize => '文字大小';

  @override
  String get textSizeDesc => '调整全局文字缩放';

  @override
  String get layoutSettings => '布局';

  @override
  String get switchStyleTitle => '开关样式';

  @override
  String get switchStylePixel => 'Pixel';

  @override
  String get switchStyleMaterial => 'Material You';

  @override
  String get switchStyleToggleDesc => '开启：Pixel 样式；关闭：Material You。';

  @override
  String get aboutApp => '关于软件';

  @override
  String get version => '版本 1.0.7';

  @override
  String get language => '语言';

  @override
  String get languageDialogTitle => '选择语言';

  @override
  String get langFollowSystem => '跟随系统';

  @override
  String get langZh => '简体中文';

  @override
  String get langEn => 'English';

  @override
  String get langJa => '日语';

  @override
  String get langKo => '韩语';

  @override
  String get langVi => '越南语';

  @override
  String get langRu => '俄语';

  @override
  String get executorImpl => '执行器实现';

  @override
  String get executorDialogTitle => '选择执行器';

  @override
  String editDialogTitle(String label) {
    return '编辑 $label';
  }

  @override
  String get inputPathHint => '输入路径...';

  @override
  String cannotOpenLink(String url) {
    return '无法打开链接: $url';
  }

  @override
  String get aboutTitle => '关于';

  @override
  String get aboutTagline => '一个永远自由，开源，免费的工具应用，每个人都应该有权使用它！';

  @override
  String get sectionProject => '项目相关';

  @override
  String get sectionDevelopers => '开发者';

  @override
  String get sectionCredits => '特别鸣谢';

  @override
  String get githubSource => 'GitHub 源代码';

  @override
  String get githubSourceDesc => '查看、Fork 或参与本项目建设';

  @override
  String get issueTracker => '问题与反馈';

  @override
  String get issueTrackerDesc => '提交 Bug 或提出您宝贵的功能建议';

  @override
  String get mainDeveloper => '主要开发者';

  @override
  String get creditDesc1 => 'C# 功能实现，为本项目 C++ 实现提供重要参考';

  @override
  String get creditDesc2 => 'C++ 功能实现，为本项目提供重要功能思路';

  @override
  String get license => '开源协议: AGPL-3.0';

  @override
  String get copyright => '© 2026 Liar ToolKit Studio';

  @override
  String get loadingExecutors => '加载中...';

  @override
  String get noExecutors => '未找到执行器';

  @override
  String get batchModeOn => '批处理';

  @override
  String get batchModeOff => '单文件';

  @override
  String get labTitle => 'Lab';

  @override
  String get pluginDrawerTitle => '插件';

  @override
  String get search => '搜索';

  @override
  String get labEmptyHint => '从左侧抽屉选择一个插件功能';

  @override
  String get startProcess => '开始处理';

  @override
  String get paramPathDesc => '选择或输入路径参数。';

  @override
  String get paramStringDesc => '输入文本参数。';

  @override
  String get paramIntegerDesc => '输入整数参数。';

  @override
  String get paramBooleanDesc => '切换布尔参数。';

  @override
  String get paramListDesc => '从列表中选择一个或多个值。';

  @override
  String get paramMapDesc => '从映射选项中选择一个或多个值。';

  @override
  String get shellCardTitle => 'Shell';

  @override
  String get shellCardSubtitle => '轻松运行任务和指令。';

  @override
  String get labCardSubtitle => '让界面开始有情绪。';

  @override
  String get storagePermission => '存储';

  @override
  String get storagePermissionDesc => '点击跳转到系统存储权限设置';

  @override
  String get checkUpdateOnStartup => '应用启动时检查更新';

  @override
  String get checkUpdateOnStartupDesc => '应用启动时自动检查新版本';

  @override
  String get repoMirror => '仓库镜像';

  @override
  String get repoMirrorDesc => '下载时使用 GitHub 镜像代理';

  @override
  String get repoMirrorNone => '直连 (不使用镜像)';

  @override
  String get updatePageTitle => '更新';

  @override
  String newVersionFound(String version) {
    return '发现新版本 $version';
  }

  @override
  String get prereleaseSuffix => ' (预览版)';

  @override
  String publishDate(String date) {
    return '发布时间：$date';
  }

  @override
  String get remindLater => '稍后提醒';

  @override
  String get viewDetails => '查看详情';

  @override
  String get updateNow => '立即更新';

  @override
  String get connecting => '连接中…';

  @override
  String get downloadingUpdate => '正在下载更新…';

  @override
  String get extracting => '正在解压…';

  @override
  String get installingFiles => '正在安装文件…';

  @override
  String updateFailed(String error) {
    return '更新失败：$error';
  }

  @override
  String get updateComplete => '更新完成';

  @override
  String get updateCompleteMessage => '文件已替换完成，点击立即安装以更新应用。';

  @override
  String get installNow => '立即安装';

  @override
  String cannotOpenApk(String message) {
    return '无法打开安装包：$message';
  }
}
