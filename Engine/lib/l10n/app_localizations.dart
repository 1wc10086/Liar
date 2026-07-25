import 'dart:async';

import 'package:flutter/foundation.dart';
import 'package:flutter/widgets.dart';
import 'package:flutter_localizations/flutter_localizations.dart';
import 'package:intl/intl.dart' as intl;

import 'app_localizations_en.dart';
import 'app_localizations_ja.dart';
import 'app_localizations_ko.dart';
import 'app_localizations_ru.dart';
import 'app_localizations_vi.dart';
import 'app_localizations_zh.dart';

// ignore_for_file: type=lint

/// Callers can lookup localized strings with an instance of AppLocalizations
/// returned by `AppLocalizations.of(context)`.
///
/// Applications need to include `AppLocalizations.delegate()` in their app's
/// `localizationDelegates` list, and the locales they support in the app's
/// `supportedLocales` list. For example:
///
/// ```dart
/// import 'l10n/app_localizations.dart';
///
/// return MaterialApp(
///   localizationsDelegates: AppLocalizations.localizationsDelegates,
///   supportedLocales: AppLocalizations.supportedLocales,
///   home: MyApplicationHome(),
/// );
/// ```
///
/// ## Update pubspec.yaml
///
/// Please make sure to update your pubspec.yaml to include the following
/// packages:
///
/// ```yaml
/// dependencies:
///   # Internationalization support.
///   flutter_localizations:
///     sdk: flutter
///   intl: any # Use the pinned version from flutter_localizations
///
///   # Rest of dependencies
/// ```
///
/// ## iOS Applications
///
/// iOS applications define key application metadata, including supported
/// locales, in an Info.plist file that is built into the application bundle.
/// To configure the locales supported by your app, you’ll need to edit this
/// file.
///
/// First, open your project’s ios/Runner.xcworkspace Xcode workspace file.
/// Then, in the Project Navigator, open the Info.plist file under the Runner
/// project’s Runner folder.
///
/// Next, select the Information Property List item, select Add Item from the
/// Editor menu, then select Localizations from the pop-up menu.
///
/// Select and expand the newly-created Localizations item then, for each
/// locale your application supports, add a new item and select the locale
/// you wish to add from the pop-up menu in the Value field. This list should
/// be consistent with the languages listed in the AppLocalizations.supportedLocales
/// property.
abstract class AppLocalizations {
  AppLocalizations(String locale)
      : localeName = intl.Intl.canonicalizedLocale(locale.toString());

  final String localeName;

  static AppLocalizations? of(BuildContext context) {
    return Localizations.of<AppLocalizations>(context, AppLocalizations);
  }

  static const LocalizationsDelegate<AppLocalizations> delegate =
      _AppLocalizationsDelegate();

  /// A list of this localizations delegate along with the default localizations
  /// delegates.
  ///
  /// Returns a list of localizations delegates containing this delegate along with
  /// GlobalMaterialLocalizations.delegate, GlobalCupertinoLocalizations.delegate,
  /// and GlobalWidgetsLocalizations.delegate.
  ///
  /// Additional delegates can be added by appending to this list in
  /// MaterialApp. This list does not have to be used at all if a custom list
  /// of delegates is preferred or required.
  static const List<LocalizationsDelegate<dynamic>> localizationsDelegates =
      <LocalizationsDelegate<dynamic>>[
    delegate,
    GlobalMaterialLocalizations.delegate,
    GlobalCupertinoLocalizations.delegate,
    GlobalWidgetsLocalizations.delegate,
  ];

  /// A list of this localizations delegate's supported locales.
  static const List<Locale> supportedLocales = <Locale>[
    Locale('en'),
    Locale('zh'),
    Locale('ja'),
    Locale('ko'),
    Locale('vi'),
    Locale('ru')
  ];

  /// No description provided for @appTitle.
  ///
  /// In en, this message translates to:
  /// **'Liar Hub'**
  String get appTitle;

  /// No description provided for @navConsole.
  ///
  /// In en, this message translates to:
  /// **'Console'**
  String get navConsole;

  /// No description provided for @navSettings.
  ///
  /// In en, this message translates to:
  /// **'Settings'**
  String get navSettings;

  /// No description provided for @rename.
  ///
  /// In en, this message translates to:
  /// **'Rename'**
  String get rename;

  /// No description provided for @cancel.
  ///
  /// In en, this message translates to:
  /// **'Cancel'**
  String get cancel;

  /// No description provided for @copy.
  ///
  /// In en, this message translates to:
  /// **'Copy'**
  String get copy;

  /// No description provided for @remove.
  ///
  /// In en, this message translates to:
  /// **'Remove'**
  String get remove;

  /// No description provided for @save.
  ///
  /// In en, this message translates to:
  /// **'Save'**
  String get save;

  /// No description provided for @clear.
  ///
  /// In en, this message translates to:
  /// **'Clear'**
  String get clear;

  /// No description provided for @notConfigured.
  ///
  /// In en, this message translates to:
  /// **'Not configured, click to edit'**
  String get notConfigured;

  /// No description provided for @noFunctions.
  ///
  /// In en, this message translates to:
  /// **'No functions available'**
  String get noFunctions;

  /// No description provided for @configureFirst.
  ///
  /// In en, this message translates to:
  /// **'Please complete configuration first'**
  String get configureFirst;

  /// No description provided for @pathHint.
  ///
  /// In en, this message translates to:
  /// **'Path'**
  String get pathHint;

  /// No description provided for @optionHint.
  ///
  /// In en, this message translates to:
  /// **'Option'**
  String get optionHint;

  /// No description provided for @skipOption.
  ///
  /// In en, this message translates to:
  /// **' (Skip) '**
  String get skipOption;

  /// No description provided for @processing.
  ///
  /// In en, this message translates to:
  /// **'Processing...'**
  String get processing;

  /// No description provided for @sectionShell.
  ///
  /// In en, this message translates to:
  /// **'Shell'**
  String get sectionShell;

  /// No description provided for @sectionAppearance.
  ///
  /// In en, this message translates to:
  /// **'Appearance'**
  String get sectionAppearance;

  /// No description provided for @sectionAbout.
  ///
  /// In en, this message translates to:
  /// **'About'**
  String get sectionAbout;

  /// No description provided for @sectionPermissions.
  ///
  /// In en, this message translates to:
  /// **'Permissions'**
  String get sectionPermissions;

  /// No description provided for @sectionUpdate.
  ///
  /// In en, this message translates to:
  /// **'Update'**
  String get sectionUpdate;

  /// No description provided for @kernelPath.
  ///
  /// In en, this message translates to:
  /// **'Kernel File Path'**
  String get kernelPath;

  /// No description provided for @scriptDir.
  ///
  /// In en, this message translates to:
  /// **'Script Directory'**
  String get scriptDir;

  /// No description provided for @themeTitle.
  ///
  /// In en, this message translates to:
  /// **'Theme'**
  String get themeTitle;

  /// No description provided for @themeMode.
  ///
  /// In en, this message translates to:
  /// **'Theme Mode'**
  String get themeMode;

  /// No description provided for @themeColor.
  ///
  /// In en, this message translates to:
  /// **'Theme Color'**
  String get themeColor;

  /// No description provided for @dynamicColor.
  ///
  /// In en, this message translates to:
  /// **'Monet Color'**
  String get dynamicColor;

  /// No description provided for @lightMode.
  ///
  /// In en, this message translates to:
  /// **'Light Mode'**
  String get lightMode;

  /// No description provided for @darkMode.
  ///
  /// In en, this message translates to:
  /// **'Dark Mode'**
  String get darkMode;

  /// No description provided for @systemMode.
  ///
  /// In en, this message translates to:
  /// **'Follow System'**
  String get systemMode;

  /// No description provided for @seedDeepPurple.
  ///
  /// In en, this message translates to:
  /// **'Deep Purple'**
  String get seedDeepPurple;

  /// No description provided for @seedIndigo.
  ///
  /// In en, this message translates to:
  /// **'Indigo'**
  String get seedIndigo;

  /// No description provided for @seedBlue.
  ///
  /// In en, this message translates to:
  /// **'Blue'**
  String get seedBlue;

  /// No description provided for @seedTeal.
  ///
  /// In en, this message translates to:
  /// **'Teal'**
  String get seedTeal;

  /// No description provided for @seedGreen.
  ///
  /// In en, this message translates to:
  /// **'Green'**
  String get seedGreen;

  /// No description provided for @seedOrange.
  ///
  /// In en, this message translates to:
  /// **'Orange'**
  String get seedOrange;

  /// No description provided for @seedPink.
  ///
  /// In en, this message translates to:
  /// **'Pink'**
  String get seedPink;

  /// No description provided for @seedRed.
  ///
  /// In en, this message translates to:
  /// **'Red'**
  String get seedRed;

  /// No description provided for @seedCustom.
  ///
  /// In en, this message translates to:
  /// **'Custom'**
  String get seedCustom;

  /// No description provided for @amoledMode.
  ///
  /// In en, this message translates to:
  /// **'AMOLED Mode'**
  String get amoledMode;

  /// No description provided for @amoledModeDesc.
  ///
  /// In en, this message translates to:
  /// **'Use pure black backgrounds to save battery'**
  String get amoledModeDesc;

  /// No description provided for @fontSetting.
  ///
  /// In en, this message translates to:
  /// **'Font'**
  String get fontSetting;

  /// No description provided for @fontSettingDesc.
  ///
  /// In en, this message translates to:
  /// **'Tap to set a font file path (empty for system default)'**
  String get fontSettingDesc;

  /// No description provided for @fontDialogTitle.
  ///
  /// In en, this message translates to:
  /// **'Set Font Path'**
  String get fontDialogTitle;

  /// No description provided for @fontPathHint.
  ///
  /// In en, this message translates to:
  /// **'Enter font file path (empty = system default)'**
  String get fontPathHint;

  /// No description provided for @fontPathDefault.
  ///
  /// In en, this message translates to:
  /// **'System Default'**
  String get fontPathDefault;

  /// No description provided for @textSize.
  ///
  /// In en, this message translates to:
  /// **'Text Size'**
  String get textSize;

  /// No description provided for @textSizeDesc.
  ///
  /// In en, this message translates to:
  /// **'Adjust the global text scale'**
  String get textSizeDesc;

  /// No description provided for @layoutSettings.
  ///
  /// In en, this message translates to:
  /// **'Layout'**
  String get layoutSettings;

  /// No description provided for @switchStyleTitle.
  ///
  /// In en, this message translates to:
  /// **'Switch Style'**
  String get switchStyleTitle;

  /// No description provided for @switchStylePixel.
  ///
  /// In en, this message translates to:
  /// **'Pixel'**
  String get switchStylePixel;

  /// No description provided for @switchStyleMaterial.
  ///
  /// In en, this message translates to:
  /// **'Material You'**
  String get switchStyleMaterial;

  /// No description provided for @switchStyleToggleDesc.
  ///
  /// In en, this message translates to:
  /// **'On: Pixel style. Off: Material You.'**
  String get switchStyleToggleDesc;

  /// No description provided for @aboutApp.
  ///
  /// In en, this message translates to:
  /// **'About'**
  String get aboutApp;

  /// No description provided for @version.
  ///
  /// In en, this message translates to:
  /// **'Version 1.0.7'**
  String get version;

  /// No description provided for @language.
  ///
  /// In en, this message translates to:
  /// **'Language'**
  String get language;

  /// No description provided for @languageDialogTitle.
  ///
  /// In en, this message translates to:
  /// **'Select Language'**
  String get languageDialogTitle;

  /// No description provided for @langFollowSystem.
  ///
  /// In en, this message translates to:
  /// **'Follow System'**
  String get langFollowSystem;

  /// No description provided for @langZh.
  ///
  /// In en, this message translates to:
  /// **'Simplified Chinese'**
  String get langZh;

  /// No description provided for @langEn.
  ///
  /// In en, this message translates to:
  /// **'English'**
  String get langEn;

  /// No description provided for @langJa.
  ///
  /// In en, this message translates to:
  /// **'Japanese'**
  String get langJa;

  /// No description provided for @langKo.
  ///
  /// In en, this message translates to:
  /// **'Korean'**
  String get langKo;

  /// No description provided for @langVi.
  ///
  /// In en, this message translates to:
  /// **'Vietnamese'**
  String get langVi;

  /// No description provided for @langRu.
  ///
  /// In en, this message translates to:
  /// **'Russian'**
  String get langRu;

  /// No description provided for @executorImpl.
  ///
  /// In en, this message translates to:
  /// **'Executor Implementation'**
  String get executorImpl;

  /// No description provided for @executorDialogTitle.
  ///
  /// In en, this message translates to:
  /// **'Select Executor'**
  String get executorDialogTitle;

  /// No description provided for @editDialogTitle.
  ///
  /// In en, this message translates to:
  /// **'Edit {label}'**
  String editDialogTitle(String label);

  /// No description provided for @inputPathHint.
  ///
  /// In en, this message translates to:
  /// **'Enter path...'**
  String get inputPathHint;

  /// No description provided for @cannotOpenLink.
  ///
  /// In en, this message translates to:
  /// **'Cannot open link: {url}'**
  String cannotOpenLink(String url);

  /// No description provided for @aboutTitle.
  ///
  /// In en, this message translates to:
  /// **'About'**
  String get aboutTitle;

  /// No description provided for @aboutTagline.
  ///
  /// In en, this message translates to:
  /// **'A forever free, open-source tool — everyone deserves access to it!'**
  String get aboutTagline;

  /// No description provided for @sectionProject.
  ///
  /// In en, this message translates to:
  /// **'Project'**
  String get sectionProject;

  /// No description provided for @sectionDevelopers.
  ///
  /// In en, this message translates to:
  /// **'Developers'**
  String get sectionDevelopers;

  /// No description provided for @sectionCredits.
  ///
  /// In en, this message translates to:
  /// **'Credits'**
  String get sectionCredits;

  /// No description provided for @githubSource.
  ///
  /// In en, this message translates to:
  /// **'GitHub Source Code'**
  String get githubSource;

  /// No description provided for @githubSourceDesc.
  ///
  /// In en, this message translates to:
  /// **'View, fork, or contribute to this project'**
  String get githubSourceDesc;

  /// No description provided for @issueTracker.
  ///
  /// In en, this message translates to:
  /// **'Issues & Feedback'**
  String get issueTracker;

  /// No description provided for @issueTrackerDesc.
  ///
  /// In en, this message translates to:
  /// **'Submit a bug report or suggest a feature'**
  String get issueTrackerDesc;

  /// No description provided for @mainDeveloper.
  ///
  /// In en, this message translates to:
  /// **'Main Developer'**
  String get mainDeveloper;

  /// No description provided for @creditDesc1.
  ///
  /// In en, this message translates to:
  /// **'C# implementation, serving as a key reference for this project\'s C++ implementation'**
  String get creditDesc1;

  /// No description provided for @creditDesc2.
  ///
  /// In en, this message translates to:
  /// **'C++ implementation, providing essential functional ideas for this project'**
  String get creditDesc2;

  /// No description provided for @license.
  ///
  /// In en, this message translates to:
  /// **'License: AGPL-3.0'**
  String get license;

  /// No description provided for @copyright.
  ///
  /// In en, this message translates to:
  /// **'© 2026 Liar ToolKit Studio'**
  String get copyright;

  /// No description provided for @loadingExecutors.
  ///
  /// In en, this message translates to:
  /// **'Loading...'**
  String get loadingExecutors;

  /// No description provided for @noExecutors.
  ///
  /// In en, this message translates to:
  /// **'No executors found'**
  String get noExecutors;

  /// No description provided for @batchModeOn.
  ///
  /// In en, this message translates to:
  /// **'Batch'**
  String get batchModeOn;

  /// No description provided for @batchModeOff.
  ///
  /// In en, this message translates to:
  /// **'Single File'**
  String get batchModeOff;

  /// No description provided for @labTitle.
  ///
  /// In en, this message translates to:
  /// **'Lab'**
  String get labTitle;

  /// No description provided for @pluginDrawerTitle.
  ///
  /// In en, this message translates to:
  /// **'Plugins'**
  String get pluginDrawerTitle;

  /// No description provided for @search.
  ///
  /// In en, this message translates to:
  /// **'Search'**
  String get search;

  /// No description provided for @labEmptyHint.
  ///
  /// In en, this message translates to:
  /// **'Select a plugin function from the drawer'**
  String get labEmptyHint;

  /// No description provided for @startProcess.
  ///
  /// In en, this message translates to:
  /// **'Start'**
  String get startProcess;

  /// No description provided for @paramPathDesc.
  ///
  /// In en, this message translates to:
  /// **'Select or enter a path parameter.'**
  String get paramPathDesc;

  /// No description provided for @paramStringDesc.
  ///
  /// In en, this message translates to:
  /// **'Enter a text parameter.'**
  String get paramStringDesc;

  /// No description provided for @paramIntegerDesc.
  ///
  /// In en, this message translates to:
  /// **'Enter an integer parameter.'**
  String get paramIntegerDesc;

  /// No description provided for @paramBooleanDesc.
  ///
  /// In en, this message translates to:
  /// **'Toggle a boolean parameter.'**
  String get paramBooleanDesc;

  /// No description provided for @paramListDesc.
  ///
  /// In en, this message translates to:
  /// **'Select one or more values from the list.'**
  String get paramListDesc;

  /// No description provided for @paramMapDesc.
  ///
  /// In en, this message translates to:
  /// **'Select one or more mapped values.'**
  String get paramMapDesc;

  /// No description provided for @shellCardTitle.
  ///
  /// In en, this message translates to:
  /// **'Shell'**
  String get shellCardTitle;

  /// No description provided for @shellCardSubtitle.
  ///
  /// In en, this message translates to:
  /// **'Run tasks and commands with ease.'**
  String get shellCardSubtitle;

  /// No description provided for @labCardSubtitle.
  ///
  /// In en, this message translates to:
  /// **'Let the interface start to feel.'**
  String get labCardSubtitle;

  /// No description provided for @storagePermission.
  ///
  /// In en, this message translates to:
  /// **'Storage'**
  String get storagePermission;

  /// No description provided for @storagePermissionDesc.
  ///
  /// In en, this message translates to:
  /// **'Tap to open the system Storage permission screen'**
  String get storagePermissionDesc;

  /// No description provided for @checkUpdateOnStartup.
  ///
  /// In en, this message translates to:
  /// **'Check for updates on startup'**
  String get checkUpdateOnStartup;

  /// No description provided for @checkUpdateOnStartupDesc.
  ///
  /// In en, this message translates to:
  /// **'Automatically check for new releases when the app starts'**
  String get checkUpdateOnStartupDesc;

  /// No description provided for @repoMirror.
  ///
  /// In en, this message translates to:
  /// **'Repository Mirror'**
  String get repoMirror;

  /// No description provided for @repoMirrorDesc.
  ///
  /// In en, this message translates to:
  /// **'Use a GitHub mirror proxy for downloads'**
  String get repoMirrorDesc;

  /// No description provided for @repoMirrorNone.
  ///
  /// In en, this message translates to:
  /// **'Direct (no mirror)'**
  String get repoMirrorNone;

  /// No description provided for @updatePageTitle.
  ///
  /// In en, this message translates to:
  /// **'Update'**
  String get updatePageTitle;

  /// No description provided for @newVersionFound.
  ///
  /// In en, this message translates to:
  /// **'New version {version} available'**
  String newVersionFound(String version);

  /// No description provided for @prereleaseSuffix.
  ///
  /// In en, this message translates to:
  /// **' (Pre-release)'**
  String get prereleaseSuffix;

  /// No description provided for @publishDate.
  ///
  /// In en, this message translates to:
  /// **'Published: {date}'**
  String publishDate(String date);

  /// No description provided for @remindLater.
  ///
  /// In en, this message translates to:
  /// **'Remind Later'**
  String get remindLater;

  /// No description provided for @viewDetails.
  ///
  /// In en, this message translates to:
  /// **'View Details'**
  String get viewDetails;

  /// No description provided for @updateNow.
  ///
  /// In en, this message translates to:
  /// **'Update Now'**
  String get updateNow;

  /// No description provided for @connecting.
  ///
  /// In en, this message translates to:
  /// **'Connecting…'**
  String get connecting;

  /// No description provided for @downloadingUpdate.
  ///
  /// In en, this message translates to:
  /// **'Downloading update…'**
  String get downloadingUpdate;

  /// No description provided for @extracting.
  ///
  /// In en, this message translates to:
  /// **'Extracting…'**
  String get extracting;

  /// No description provided for @installingFiles.
  ///
  /// In en, this message translates to:
  /// **'Installing files…'**
  String get installingFiles;

  /// No description provided for @updateFailed.
  ///
  /// In en, this message translates to:
  /// **'Update failed: {error}'**
  String updateFailed(String error);

  /// No description provided for @updateComplete.
  ///
  /// In en, this message translates to:
  /// **'Update Complete'**
  String get updateComplete;

  /// No description provided for @updateCompleteMessage.
  ///
  /// In en, this message translates to:
  /// **'Files have been replaced. Tap to install the new APK.'**
  String get updateCompleteMessage;

  /// No description provided for @installNow.
  ///
  /// In en, this message translates to:
  /// **'Install Now'**
  String get installNow;

  /// No description provided for @cannotOpenApk.
  ///
  /// In en, this message translates to:
  /// **'Cannot open APK installer: {message}'**
  String cannotOpenApk(String message);
}

class _AppLocalizationsDelegate
    extends LocalizationsDelegate<AppLocalizations> {
  const _AppLocalizationsDelegate();

  @override
  Future<AppLocalizations> load(Locale locale) {
    return SynchronousFuture<AppLocalizations>(lookupAppLocalizations(locale));
  }

  @override
  bool isSupported(Locale locale) => <String>[
        'en',
        'ja',
        'ko',
        'ru',
        'vi',
        'zh'
      ].contains(locale.languageCode);

  @override
  bool shouldReload(_AppLocalizationsDelegate old) => false;
}

AppLocalizations lookupAppLocalizations(Locale locale) {
  // Lookup logic when only language code is specified.
  switch (locale.languageCode) {
    case 'en':
      return AppLocalizationsEn();
    case 'ja':
      return AppLocalizationsJa();
    case 'ko':
      return AppLocalizationsKo();
    case 'ru':
      return AppLocalizationsRu();
    case 'vi':
      return AppLocalizationsVi();
    case 'zh':
      return AppLocalizationsZh();
  }

  throw FlutterError(
      'AppLocalizations.delegate failed to load unsupported locale "$locale". This is likely '
      'an issue with the localizations generation tool. Please file an issue '
      'on GitHub with a reproducible sample app and the gen-l10n configuration '
      'that was used.');
}
