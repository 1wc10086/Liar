import 'dart:convert';
import 'dart:io';
import 'package:flutter/services.dart';
import 'script_config.dart';

const _channel = MethodChannel('com.liar.byzymztools/shell');

class FuncParam {
  final String name;
  final String type;
  final String localizedName;
  final String defaultValue;
  final bool isRequired;
  final bool folder;
  final String mapProvider;
  final List<String> listOptions;
  final List<OptionItem> mapOptions;
  final Map<dynamic, dynamic> raw;

  const FuncParam({
    required this.name,
    required this.type,
    required this.localizedName,
    required this.defaultValue,
    required this.isRequired,
    required this.folder,
    required this.mapProvider,
    required this.listOptions,
    required this.mapOptions,
    required this.raw,
  });

  factory FuncParam.from(Map<dynamic, dynamic> raw) {
    final mapList = ((raw['map'] as List<dynamic>?) ?? [])
        .whereType<Map<dynamic, dynamic>>()
        .map((e) => OptionItem(
              label: (e['display'] as String?) ?? '',
              value: (e['value'] as String?) ?? '',
            ))
        .toList();

    return FuncParam(
      name: (raw['name'] as String?) ?? '',
      type: (raw['type'] as String?) ?? 'string',
      localizedName: (raw['localizedName'] as String?) ?? '',
      defaultValue: (raw['defaultValue'] as String?) ?? '',
      isRequired: raw['required'] == true,
      folder: raw['folder'] == true,
      mapProvider: (raw['mapProvider'] as String?) ?? '',
      listOptions: ((raw['list'] as List<dynamic>?) ?? []).map((e) => e.toString()).toList(),
      mapOptions: mapList,
      raw: raw,
    );
  }
}

class FuncItem {
  final String funcName;
  final String title;
  final String fullName;
  final Map<dynamic, dynamic> raw;
  List<FuncParam>? _params;

  FuncItem({
    required this.funcName,
    required this.title,
    required this.fullName,
    required this.raw,
  });

  List<FuncParam> get params {
    _params ??= ((raw['params'] as List<dynamic>?) ?? [])
        .whereType<Map<dynamic, dynamic>>()
        .map(FuncParam.from)
        .toList();
    return _params!;
  }
}

class FuncGroup {
  final String title;
  final List<FuncItem> items;

  const FuncGroup({required this.title, required this.items});
}

class OptionItem {
  final String label;
  final String value;

  const OptionItem({required this.label, required this.value});
}

class MatchResult {
  final List<FuncGroup> groups;
  final bool isFolder;

  const MatchResult({required this.groups, required this.isFolder});
}

class ShellTranslation {
  final Map<String, String> translations;
  final Map<String, dynamic> sysInfo;
  final double initTime;

  const ShellTranslation({
    required this.translations,
    required this.sysInfo,
    required this.initTime,
  });
}

class ShellService {
  static final ShellService _instance = ShellService._();
  factory ShellService() => _instance;
  ShellService._();

  bool _initialized = false;
  bool _initializing = false;
  bool _initSuccess = false;
  String _configKey = '';

  void reset() {
    _initialized = false;
    _initializing = false;
    _initSuccess = false;
    _configKey = '';
  }

  Future<(ShellTranslation, bool)> initialize({
    required String soPath,
    required String scriptDir,
    bool forceReload = false,
  }) async {
    final key = '$soPath|$scriptDir';

    if (_initialized && _configKey != key) reset();

    if (!forceReload && _initialized && _configKey == key) {
      return (_ShellInitCache.translations!, _initSuccess);
    }

    if (_initializing) {
      while (_initializing) {
        await Future.delayed(const Duration(milliseconds: 10));
      }
      if (_ShellInitCache.translations != null) {
        return (_ShellInitCache.translations!, _initSuccess);
      }
    }

    _initializing = true;
    final startTime = DateTime.now();

    try {
      final main = await ScriptConfig.readMain(scriptDir);
      final resStr = await _channel.invokeMethod('initKernel', {
        'soPath': soPath,
        'scriptDir': scriptDir,
        'libraryDir': main?.libraryPath ?? '',
        'forceReload': forceReload,
      });

      final res = jsonDecode(resStr as String);
      final ok = res['isSuccess'] == true;

      if (!ok) {
        _initializing = false;
        _initSuccess = false;
        _initialized = true;
        _configKey = key;
        return (const ShellTranslation(translations: {}, sysInfo: {}, initTime: 0.0), false);
      }

      final trans = ShellTranslation(
        translations: Map<String, String>.from(res['translations'] ?? {}),
        sysInfo: Map<String, dynamic>.from(res['sysInfo'] ?? {}),
        initTime: DateTime.now().difference(startTime).inMilliseconds / 1000.0,
      );

      _ShellInitCache.translations = trans;
      _initializing = false;
      _initSuccess = true;
      _initialized = true;
      _configKey = key;

      return (trans, true);
    } catch (_) {
      _initializing = false;
      _initSuccess = false;
      _initialized = true;
      _configKey = key;
      return (const ShellTranslation(translations: {}, sysInfo: {}, initTime: 0.0), false);
    }
  }

  Future<MatchResult> matchFunctions(String path, String scriptDir) async {
    final res = await _channel.invokeMethod('matchFunctions', {
      'path': path,
      'scriptDir': scriptDir,
    });

    if (res['isSuccess'] != true) return const MatchResult(groups: [], isFolder: false);

    final list = jsonDecode(res['payload'] as String) as List<dynamic>;
    final funcs = list
        .whereType<Map<dynamic, dynamic>>()
        .where((m) => m['errorCode'] == null)
        .toList();

    final isFolder = res['isFolder'] == true || File(path).existsSync() == false;

    return MatchResult(groups: _buildGroups(funcs), isFolder: isFolder);
  }

  Future<Map<String, dynamic>> runFunction({
    required String funcName,
    required List<String> params,
    required String scriptDir,
  }) async {
    try {
      final res = await _channel.invokeMethod('runFunction', {
        'funcName': funcName,
        'params': params,
        'scriptDir': scriptDir,
      });
      return Map<String, dynamic>.from(res as Map);
    } catch (e) {
      return {'isSuccess': false, 'code': 3, 'output': e.toString()};
    }
  }

  Future<Map<String, dynamic>> runBatch({
    required String funcName,
    required String inputFolder,
    required String outputFolder,
    required List<String> extraParams,
    required String scriptDir,
  }) async {
    try {
      final res = await _channel.invokeMethod('runBatchFunction', {
        'funcName': funcName,
        'inputFolder': inputFolder,
        'outputFolder': outputFolder,
        'extraParams': extraParams,
        'scriptDir': scriptDir,
      });
      return Map<String, dynamic>.from(res as Map);
    } catch (e) {
      return {'isSuccess': false, 'code': 3, 'output': e.toString()};
    }
  }

  Future<List<OptionItem>> queryParamOptions(String funcName, String paramName) async {
    try {
      final raw = await _channel.invokeMethod<String>('queryParamOptions', {
        'funcName': funcName,
        'paramName': paramName,
      });
      final list = jsonDecode(raw ?? '[]') as List<dynamic>;
      return list.map((e) {
        final m = e as Map<dynamic, dynamic>;
        return OptionItem(
          label: (m['display'] as String?) ?? '',
          value: (m['value'] as String?) ?? '',
        );
      }).toList();
    } catch (_) {
      return [];
    }
  }

  List<FuncGroup> _buildGroups(List<Map<dynamic, dynamic>> functions) {
    final groupMap = <String, List<FuncItem>>{};

    for (final f in functions) {
      final rawName = _cleanName((f['localizedName'] as String?) ?? '');
      if (rawName.isEmpty) continue;

      final parts = rawName.split(RegExp(r'\s+')).where((e) => e.isNotEmpty).toList();
      final groupTitle = parts.length <= 1 ? rawName : parts.sublist(0, parts.length - 1).join(' ');
      final displayName = parts.length <= 1 ? rawName : parts.last;

      groupMap.putIfAbsent(groupTitle, () => []);
      groupMap[groupTitle]!.add(FuncItem(
        funcName: f['funcName'] as String,
        title: displayName,
        fullName: rawName,
        raw: Map<dynamic, dynamic>.from(f),
      ));
    }

    final groups = groupMap.entries.map((entry) {
      final items = entry.value..sort((a, b) => a.title.compareTo(b.title));
      return FuncGroup(title: entry.key, items: items);
    }).toList();

    groups.sort((a, b) => a.title.compareTo(b.title));
    return groups;
  }

  String _cleanName(String value) =>
      value.replaceAll(RegExp(r'^\[\*\]\s*'), '').trim();

  String normalizePath(String path) =>
      path.trim().replaceAll(RegExp(r'[/\\]+$'), '');
}

class _ShellInitCache {
  static ShellTranslation? translations;
}
