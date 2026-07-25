// ignore: unused_import
import 'package:intl/intl.dart' as intl;
import 'app_localizations.dart';

// ignore_for_file: type=lint

/// The translations for English (`en`).
class AppLocalizationsEn extends AppLocalizations {
  AppLocalizationsEn([String locale = 'en']) : super(locale);

  @override
  String get appTitle => 'Liar Hub';

  @override
  String get navConsole => 'Console';

  @override
  String get navSettings => 'Settings';

  @override
  String get rename => 'Rename';

  @override
  String get cancel => 'Cancel';

  @override
  String get copy => 'Copy';

  @override
  String get remove => 'Remove';

  @override
  String get save => 'Save';

  @override
  String get clear => 'Clear';

  @override
  String get notConfigured => 'Not configured, click to edit';

  @override
  String get noFunctions => 'No functions available';

  @override
  String get configureFirst => 'Please complete configuration first';

  @override
  String get pathHint => 'Path';

  @override
  String get optionHint => 'Option';

  @override
  String get skipOption => ' (Skip) ';

  @override
  String get processing => 'Processing...';

  @override
  String get sectionShell => 'Shell';

  @override
  String get sectionAppearance => 'Appearance';

  @override
  String get sectionAbout => 'About';

  @override
  String get sectionPermissions => 'Permissions';

  @override
  String get sectionUpdate => 'Update';

  @override
  String get kernelPath => 'Kernel File Path';

  @override
  String get scriptDir => 'Script Directory';

  @override
  String get themeTitle => 'Theme';

  @override
  String get themeMode => 'Theme Mode';

  @override
  String get themeColor => 'Theme Color';

  @override
  String get dynamicColor => 'Monet Color';

  @override
  String get lightMode => 'Light Mode';

  @override
  String get darkMode => 'Dark Mode';

  @override
  String get systemMode => 'Follow System';

  @override
  String get seedDeepPurple => 'Deep Purple';

  @override
  String get seedIndigo => 'Indigo';

  @override
  String get seedBlue => 'Blue';

  @override
  String get seedTeal => 'Teal';

  @override
  String get seedGreen => 'Green';

  @override
  String get seedOrange => 'Orange';

  @override
  String get seedPink => 'Pink';

  @override
  String get seedRed => 'Red';

  @override
  String get seedCustom => 'Custom';

  @override
  String get amoledMode => 'AMOLED Mode';

  @override
  String get amoledModeDesc => 'Use pure black backgrounds to save battery';

  @override
  String get fontSetting => 'Font';

  @override
  String get fontSettingDesc =>
      'Tap to set a font file path (empty for system default)';

  @override
  String get fontDialogTitle => 'Set Font Path';

  @override
  String get fontPathHint => 'Enter font file path (empty = system default)';

  @override
  String get fontPathDefault => 'System Default';

  @override
  String get textSize => 'Text Size';

  @override
  String get textSizeDesc => 'Adjust the global text scale';

  @override
  String get layoutSettings => 'Layout';

  @override
  String get switchStyleTitle => 'Switch Style';

  @override
  String get switchStylePixel => 'Pixel';

  @override
  String get switchStyleMaterial => 'Material You';

  @override
  String get switchStyleToggleDesc => 'On: Pixel style. Off: Material You.';

  @override
  String get aboutApp => 'About';

  @override
  String get version => 'Version 1.0.7';

  @override
  String get language => 'Language';

  @override
  String get languageDialogTitle => 'Select Language';

  @override
  String get langFollowSystem => 'Follow System';

  @override
  String get langZh => 'Simplified Chinese';

  @override
  String get langEn => 'English';

  @override
  String get langJa => 'Japanese';

  @override
  String get langKo => 'Korean';

  @override
  String get langVi => 'Vietnamese';

  @override
  String get langRu => 'Russian';

  @override
  String get executorImpl => 'Executor Implementation';

  @override
  String get executorDialogTitle => 'Select Executor';

  @override
  String editDialogTitle(String label) {
    return 'Edit $label';
  }

  @override
  String get inputPathHint => 'Enter path...';

  @override
  String cannotOpenLink(String url) {
    return 'Cannot open link: $url';
  }

  @override
  String get aboutTitle => 'About';

  @override
  String get aboutTagline =>
      'A forever free, open-source tool — everyone deserves access to it!';

  @override
  String get sectionProject => 'Project';

  @override
  String get sectionDevelopers => 'Developers';

  @override
  String get sectionCredits => 'Credits';

  @override
  String get githubSource => 'GitHub Source Code';

  @override
  String get githubSourceDesc => 'View, fork, or contribute to this project';

  @override
  String get issueTracker => 'Issues & Feedback';

  @override
  String get issueTrackerDesc => 'Submit a bug report or suggest a feature';

  @override
  String get mainDeveloper => 'Main Developer';

  @override
  String get creditDesc1 =>
      'C# implementation, serving as a key reference for this project\'s C++ implementation';

  @override
  String get creditDesc2 =>
      'C++ implementation, providing essential functional ideas for this project';

  @override
  String get license => 'License: AGPL-3.0';

  @override
  String get copyright => '© 2026 Liar ToolKit Studio';

  @override
  String get loadingExecutors => 'Loading...';

  @override
  String get noExecutors => 'No executors found';

  @override
  String get batchModeOn => 'Batch';

  @override
  String get batchModeOff => 'Single File';

  @override
  String get labTitle => 'Lab';

  @override
  String get pluginDrawerTitle => 'Plugins';

  @override
  String get search => 'Search';

  @override
  String get labEmptyHint => 'Select a plugin function from the drawer';

  @override
  String get startProcess => 'Start';

  @override
  String get paramPathDesc => 'Select or enter a path parameter.';

  @override
  String get paramStringDesc => 'Enter a text parameter.';

  @override
  String get paramIntegerDesc => 'Enter an integer parameter.';

  @override
  String get paramBooleanDesc => 'Toggle a boolean parameter.';

  @override
  String get paramListDesc => 'Select one or more values from the list.';

  @override
  String get paramMapDesc => 'Select one or more mapped values.';

  @override
  String get shellCardTitle => 'Shell';

  @override
  String get shellCardSubtitle => 'Run tasks and commands with ease.';

  @override
  String get labCardSubtitle => 'Let the interface start to feel.';

  @override
  String get storagePermission => 'Storage';

  @override
  String get storagePermissionDesc =>
      'Tap to open the system Storage permission screen';

  @override
  String get checkUpdateOnStartup => 'Check for updates on startup';

  @override
  String get checkUpdateOnStartupDesc =>
      'Automatically check for new releases when the app starts';

  @override
  String get repoMirror => 'Repository Mirror';

  @override
  String get repoMirrorDesc => 'Use a GitHub mirror proxy for downloads';

  @override
  String get repoMirrorNone => 'Direct (no mirror)';

  @override
  String get updatePageTitle => 'Update';

  @override
  String newVersionFound(String version) {
    return 'New version $version available';
  }

  @override
  String get prereleaseSuffix => ' (Pre-release)';

  @override
  String publishDate(String date) {
    return 'Published: $date';
  }

  @override
  String get remindLater => 'Remind Later';

  @override
  String get viewDetails => 'View Details';

  @override
  String get updateNow => 'Update Now';

  @override
  String get connecting => 'Connecting…';

  @override
  String get downloadingUpdate => 'Downloading update…';

  @override
  String get extracting => 'Extracting…';

  @override
  String get installingFiles => 'Installing files…';

  @override
  String updateFailed(String error) {
    return 'Update failed: $error';
  }

  @override
  String get updateComplete => 'Update Complete';

  @override
  String get updateCompleteMessage =>
      'Files have been replaced. Tap to install the new APK.';

  @override
  String get installNow => 'Install Now';

  @override
  String cannotOpenApk(String message) {
    return 'Cannot open APK installer: $message';
  }
}
