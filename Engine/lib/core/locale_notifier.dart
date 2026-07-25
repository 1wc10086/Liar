import 'dart:ui';
import 'package:flutter/foundation.dart';
import 'app_settings.dart';
import 'script_config.dart';

const _supportedLocales = ['en', 'zh', 'ja', 'ko', 'vi', 'ru'];

const _localeToShellLang = {
  'zh': 'chinese',
  'en': 'english',
  'ja': 'japanese',
  'ko': 'korean',
  'vi': 'vietnamese',
  'ru': 'russian',
};

class LocaleNotifier extends ChangeNotifier {
  LocaleNotifier(this._settings) {
    _locale = _resolve(_settings.locale);
  }

  final AppSettings _settings;
  late Locale _locale;

  Locale get locale => _locale;

  static Locale _resolve(String stored) {
    if (stored.isNotEmpty && _supportedLocales.contains(stored)) {
      return Locale(stored);
    }
    final sys = PlatformDispatcher.instance.locale.languageCode;
    if (_supportedLocales.contains(sys)) return Locale(sys);
    return const Locale('en');
  }

  Future<void> setLocale(Locale locale, String scriptDir) async {
    _locale = locale;
    await _settings.setLocale(locale.languageCode);
    notifyListeners();
    if (scriptDir.isNotEmpty) {
      final lang = _localeToShellLang[locale.languageCode] ?? 'english';
      await ScriptConfig.writeShellLanguage(scriptDir, lang);
    }
  }

  Future<void> setFollowSystem(String scriptDir) async {
    await _settings.setLocale('');
    _locale = _resolve('');
    notifyListeners();
    if (scriptDir.isNotEmpty) {
      final lang = _localeToShellLang[_locale.languageCode] ?? 'english';
      await ScriptConfig.writeShellLanguage(scriptDir, lang);
    }
  }
}
