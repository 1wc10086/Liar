import 'dart:convert';
import 'dart:io';
import 'package:flutter/foundation.dart';
import 'package:path/path.dart' as p;
import 'jsonc.dart';

class ScriptMainConfig {
  final String executorImplementation;
  final String settingsPath;
  final String languageDir;
  final String libraryPath;

  const ScriptMainConfig({
    required this.executorImplementation,
    required this.settingsPath,
    required this.languageDir,
    required this.libraryPath,
  });
}

class ScriptConfig {
  static Future<ScriptMainConfig?> readMain(String scriptDir) async {
    try {
      final file = File(p.join(scriptDir, 'main.json'));
      if (!await file.exists()) return null;
      final decoded = decodeJsonc(await file.readAsString());
      if (decoded is! Map) throw const FormatException('main.json must be an object');
      final map = Map<String, dynamic>.from(decoded);
      final pathsValue = map['paths'];
      if (pathsValue != null && pathsValue is! Map) throw const FormatException('main.json paths must be an object');
      final paths = pathsValue is Map ? Map<String, dynamic>.from(pathsValue) : <String, dynamic>{};
      final settingsPath = p.join(scriptDir, (paths['settings'] as String?) ?? '');
      String libraryPath = '';
      try {
        final settingsFile = File(settingsPath);
        if (await settingsFile.exists()) {
          final decoded = decodeJsonc(await settingsFile.readAsString());
          if (decoded is! Map) throw const FormatException('settings.json must be an object');
          final settings = Map<String, dynamic>.from(decoded);
          libraryPath = p.join(scriptDir, (settings['library'] as String?) ?? '');
        }
      } catch (_) {}
      return ScriptMainConfig(
        executorImplementation: (paths['executor_implementation'] as String?) ?? '',
        settingsPath:           settingsPath,
        languageDir:            p.join(scriptDir, (paths['language_dir'] as String?) ?? ''),
        libraryPath:            libraryPath,
      );
    } catch (e) {
      debugPrint('读取 main.json 失败: $e');
      return null;
    }
  }

  static Future<String> readShellLanguage(String scriptDir) async {
    try {
      final cfg  = await readMain(scriptDir);
      if (cfg == null) return 'english';
      final file = File(cfg.settingsPath);
      if (!await file.exists()) return 'english';
      final decoded = decodeJsonc(await file.readAsString());
      if (decoded is! Map) throw const FormatException('settings.json must be an object');
      final map = Map<String, dynamic>.from(decoded);
      return (map['language'] as String?) ?? 'english';
    } catch (e) {
      return 'english';
    }
  }

  static Future<void> writeShellLanguage(String scriptDir, String lang) async {
    final cfg = await readMain(scriptDir);
    if (cfg == null) return;
    final file = File(cfg.settingsPath);
    if (!await file.parent.exists()) await file.parent.create(recursive: true);
    if (!await file.exists()) {
      await file.writeAsString(const JsonEncoder.withIndent('    ')
          .convert(<String, dynamic>{'language': lang}));
      return;
    }
    final raw = await file.readAsString();
    final patched = patchJsoncString(raw, {'language': lang});
    await file.writeAsString(patched);
  }

  static Future<List<String>> listExecutors(String scriptDir) async {
    try {
      final executorDir = Directory(p.join(scriptDir, 'executor'));
      if (!await executorDir.exists()) return [];
      final entries = await executorDir.list().toList();
      return entries
          .whereType<Directory>()
          .map((d) => p.basename(d.path))
          .toList();
    } catch (e) {
      return [];
    }
  }

  static Future<String> readCurrentExecutor(String scriptDir) async {
    try {
      final cfg = await readMain(scriptDir);
      if (cfg == null) return '';
      final parts = cfg.executorImplementation.split('/');
      return parts.isNotEmpty ? parts.last : '';
    } catch (e) {
      return '';
    }
  }

  static Future<void> writeExecutor(String scriptDir, String implName) async {
    final file = File(p.join(scriptDir, 'main.json'));
    if (!await file.exists()) return;
    final raw = await file.readAsString();
    final patched = patchJsoncString(raw, {
      'executor_implementation': 'executor/$implName',
    });
    await file.writeAsString(patched);
  }
}
