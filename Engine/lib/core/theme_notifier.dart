import 'dart:io';
import 'dart:typed_data';
import 'package:flutter/material.dart';
import 'package:flutter/services.dart' show FontLoader;
import 'app_settings.dart';

class ThemeNotifier extends ChangeNotifier {
  ThemeNotifier(this._settings) {
    _mode = _parseThemeMode(_settings.themeMode);
    _seed = Color(_settings.themeSeed);
    _useDynamicColor = _settings.useDynamicColor;
    _applyFontFromSettings();
  }

  final AppSettings _settings;
  late ThemeMode _mode;
  late Color _seed;
  late bool _useDynamicColor;
  String? _fontFamily;

  static const String _appFontFamily = '_liarAppFont';

  ThemeMode get mode => _mode;
  Color get seed => _seed;
  bool get useDynamicColor => _useDynamicColor;
  bool get amoledMode => _settings.amoledMode;
  String get switchStyle => _settings.switchStyle;
  String? get fontFamily => _fontFamily;
  double get fontScale => _settings.fontScale;
  String get fontPath => _settings.fontPath;

  ThemeMode _parseThemeMode(String modeStr) {
    switch (modeStr) {
      case 'light': return ThemeMode.light;
      case 'dark': return ThemeMode.dark;
      default: return ThemeMode.system;
    }
  }

  String _themeModeToString(ThemeMode mode) {
    switch (mode) {
      case ThemeMode.light: return 'light';
      case ThemeMode.dark: return 'dark';
      case ThemeMode.system: return 'system';
    }
  }

  void setMode(ThemeMode mode) {
    if (_mode == mode) return;
    _mode = mode;
    _settings.setThemeMode(_themeModeToString(mode));
    notifyListeners();
  }

  void setSeed(Color color) {
    _useDynamicColor = false;
    _settings.setUseDynamicColor(false);
    _seed = color;
    _settings.setThemeSeed(color.value);
    notifyListeners();
  }

  void enableDynamicColor() {
    _useDynamicColor = true;
    _settings.setUseDynamicColor(true);
    notifyListeners();
  }

  void setAmoledMode(bool v) { _settings.setAmoledMode(v); notifyListeners(); }
  void setSwitchStyle(String v) { _settings.setSwitchStyle(v); notifyListeners(); }
  void setFontScale(double v) { _settings.setFontScale(v); notifyListeners(); }

  Future<void> _applyFontFromSettings() async {
    final path = _settings.fontPath;
    if (path.isEmpty) { _fontFamily = null; return; }
    await _loadFontFile(path);
    notifyListeners();
  }

  Future<void> setFontPath(String path) async {
    _settings.setFontPath(path);
    await _loadFontFile(path);
    notifyListeners();
  }

  Future<void> _loadFontFile(String path) async {
    try {
      final file = File(path);
      if (!await file.exists()) { _fontFamily = null; return; }
      final bytes = await file.readAsBytes();
      final loader = FontLoader(_appFontFamily);
      loader.addFont(Future<ByteData>.value(
          ByteData.sublistView(bytes.buffer.asUint8List())));
      await loader.load();
      _fontFamily = _appFontFamily;
    } catch (_) { _fontFamily = null; }
  }

  ThemeData buildTheme(Brightness brightness) {
    final scheme = ColorScheme.fromSeed(seedColor: _seed, brightness: brightness);
    final fontFamily = _fontFamily;

    if (_settings.amoledMode && brightness == Brightness.dark) {
      return ThemeData(
        useMaterial3: true,
        fontFamily: fontFamily,
        sliderTheme: const SliderThemeData(year2023: false),
        colorScheme: scheme.copyWith(
          surface: Colors.black,
          surfaceContainerHighest: const Color(0xFF0A0A0A),
          surfaceContainerHigh: const Color(0xFF080808),
          surfaceContainer: const Color(0xFF050505),
          surfaceContainerLow: const Color(0xFF020202),
          surfaceContainerLowest: Colors.black,
          surfaceDim: Colors.black,
          surfaceBright: const Color(0xFF121212),
          onSurface: Colors.white,
          onSurfaceVariant: Colors.white70,
        ),
        scaffoldBackgroundColor: Colors.black,
        dialogBackgroundColor: const Color(0xFF0A0A0A),
        appBarTheme: const AppBarTheme(backgroundColor: Colors.black),
        drawerTheme: const DrawerThemeData(backgroundColor: Colors.black),
        navigationBarTheme: NavigationBarThemeData(
          backgroundColor: Colors.black,
          surfaceTintColor: Colors.transparent,
        ),
        bottomAppBarTheme: const BottomAppBarThemeData(color: Colors.black),
        cardTheme: CardThemeData(color: const Color(0xFF0A0A0A)),
      );
    }

    return ThemeData(useMaterial3: true, fontFamily: fontFamily,
        sliderTheme: const SliderThemeData(year2023: false), colorScheme: scheme);
  }
}
