import 'dart:convert';
import 'dart:io';
import 'package:flutter/foundation.dart';
import 'package:path_provider/path_provider.dart';
import 'package:path/path.dart' as p;
import 'jsonc.dart';

class AppSettings extends ChangeNotifier {
  String _soPath = '';
  String _scriptDir = '';
  String _themeMode = 'system';
  int _themeSeed = 0xFF673AB7;
  bool _useDynamicColor = false;
  String _locale = '';
  bool _amoledMode = false;
  String _switchStyle = 'material';
  String _fontPath = '';
  double _fontScale = 1.0;
  bool _checkUpdateOnStartup = true;
  String _repoMirror = '';

  String get soPath => _soPath;
  String get scriptDir => _scriptDir;
  String get themeMode => _themeMode;
  int get themeSeed => _themeSeed;
  bool get useDynamicColor => _useDynamicColor;
  String get locale => _locale;
  bool get amoledMode => _amoledMode;
  String get switchStyle => _switchStyle;
  String get fontPath => _fontPath;
  double get fontScale => _fontScale;
  bool get checkUpdateOnStartup => _checkUpdateOnStartup;
  String get repoMirror => _repoMirror;

  bool get isConfigured => _soPath.isNotEmpty && _scriptDir.isNotEmpty;

  static Future<File> _getConfigFile() async {
    Directory? dir;
    try { dir = await getExternalStorageDirectory(); } catch (_) { dir = null; }
    dir ??= await getApplicationSupportDirectory();
    return File(p.join(dir.path, 'setting.json'));
  }

  static Future<AppSettings> load() async {
    final instance = AppSettings();
    try {
      final file = await _getConfigFile();
      if (await file.exists()) {
        final decoded = decodeJsonc(await file.readAsString());
        if (decoded is! Map) throw const FormatException('setting.json must be an object');
        final map = Map<String, dynamic>.from(decoded);
        final soPath = map['soPath'];
        final scriptDir = map['scriptDir'];
        final themeMode = map['themeMode'];
        final themeSeed = map['themeSeed'];
        final useDynamicColor = map['useDynamicColor'];
        final locale = map['locale'];
        final amoledMode = map['amoledMode'];
        final switchStyle = map['switchStyle'];
        final fontPath = map['fontPath'];
        final fontScale = map['fontScale'];
        final checkUpdateOnStartup = map['checkUpdateOnStartup'];
        final repoMirror = map['repoMirror'];
        instance._soPath = soPath is String ? soPath : '';
        instance._scriptDir = scriptDir is String ? scriptDir : '';
        instance._themeMode = themeMode is String ? themeMode : 'system';
        instance._themeSeed = themeSeed is int ? themeSeed : 0xFF673AB7;
        instance._useDynamicColor = useDynamicColor is bool ? useDynamicColor : false;
        instance._locale = locale is String ? locale : '';
        instance._amoledMode = amoledMode is bool ? amoledMode : false;
        instance._switchStyle = switchStyle is String ? switchStyle : 'material';
        instance._fontPath = fontPath is String ? fontPath : '';
        instance._fontScale = fontScale is num ? fontScale.toDouble() : 1.0;
        instance._checkUpdateOnStartup =
            checkUpdateOnStartup is bool ? checkUpdateOnStartup : true;
        instance._repoMirror = repoMirror is String ? repoMirror : '';
      }
    } catch (e) { debugPrint('AppSettings load failed: $e'); }
    return instance;
  }

  Future<void> _persist() async {
    try {
      final file = await _getConfigFile();
      if (!await file.parent.exists()) await file.parent.create(recursive: true);
      Map<String, dynamic> map = {};
      if (await file.exists()) {
        final decoded = decodeJsonc(await file.readAsString());
        if (decoded is! Map) throw const FormatException('setting.json must be an object');
        map = Map<String, dynamic>.from(decoded);
      }
      map.addAll({
        'soPath': _soPath, 'scriptDir': _scriptDir, 'themeMode': _themeMode,
        'themeSeed': _themeSeed, 'useDynamicColor': _useDynamicColor,
        'locale': _locale, 'amoledMode': _amoledMode, 'switchStyle': _switchStyle,
        'fontPath': _fontPath, 'fontScale': _fontScale,
        'checkUpdateOnStartup': _checkUpdateOnStartup, 'repoMirror': _repoMirror,
      });
      await file.writeAsString(jsonEncode(map));
    } catch (e) { debugPrint('AppSettings persist failed: $e'); }
  }

  Future<void> setSoPath(String v) async { _soPath = v.trim(); notifyListeners(); await _persist(); }
  Future<void> setScriptDir(String v) async { _scriptDir = v.trim(); notifyListeners(); await _persist(); }
  Future<void> setThemeMode(String v) async { _themeMode = v; notifyListeners(); await _persist(); }
  Future<void> setThemeSeed(int v) async { _themeSeed = v; notifyListeners(); await _persist(); }
  Future<void> setUseDynamicColor(bool v) async { _useDynamicColor = v; notifyListeners(); await _persist(); }
  Future<void> setLocale(String v) async { _locale = v; notifyListeners(); await _persist(); }
  Future<void> setAmoledMode(bool v) async { _amoledMode = v; notifyListeners(); await _persist(); }
  Future<void> setSwitchStyle(String v) async { _switchStyle = v; notifyListeners(); await _persist(); }
  Future<void> setFontPath(String v) async { _fontPath = v.trim(); notifyListeners(); await _persist(); }
  Future<void> setFontScale(double v) async { _fontScale = v; notifyListeners(); await _persist(); }
  Future<void> setCheckUpdateOnStartup(bool v) async { _checkUpdateOnStartup = v; notifyListeners(); await _persist(); }
  Future<void> setRepoMirror(String v) async { _repoMirror = v; notifyListeners(); await _persist(); }
}
