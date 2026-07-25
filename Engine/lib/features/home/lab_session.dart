import 'dart:async';
import 'package:flutter/foundation.dart';
import 'package:flutter/material.dart' show TextEditingController;

import '../../core/app_settings.dart';
import '../../core/shell_service.dart';

class LabSession extends ChangeNotifier {
  LabSession({required this.func, required this.group});

  final FuncGroup group;
  final FuncItem func;

  final Map<String, String> _values = {};
  final Map<String, TextEditingController> _textCtrls = {};
  final Map<String, List<OptionItem>> _dynamicOptions = {};
  bool _batchMode = false;
  bool _processing = false;
  String _status = '';

  Map<String, String> get values => _values;
  TextEditingController? controllerOf(String name) => _textCtrls[name];
  List<OptionItem> dynamicOptionsOf(String name) => _dynamicOptions[name] ?? const [];
  bool get batchMode => _batchMode;
  bool get processing => _processing;
  String get status => _status;

  Iterable<TextEditingController> get controllers => _textCtrls.values;

  FutureOr<void> prepare() {
    for (final param in func.params) {
      final name = param.name;
      switch (param.type) {
        case 'boolean':
          _values[name] = param.defaultValue == 'true' || param.defaultValue == '1'
              ? 'true'
              : 'false';
        case 'list':
          final opts = param.listOptions;
          _values[name] = opts.contains(param.defaultValue)
              ? param.defaultValue
              : (opts.isNotEmpty ? opts.first : '');
        case 'map':
          final opts = param.mapOptions;
          if (opts.isNotEmpty) {
            final vals = opts.map((e) => e.value).toList();
            _values[name] = vals.contains(param.defaultValue)
                ? param.defaultValue
                : opts.first.value;
          } else {
            _values[name] = param.defaultValue;
          }
        default:
          _textCtrls[name] = TextEditingController(text: param.defaultValue);
      }
    }
  }

  Future<void> loadDynamicMapOptions(ShellService svc, String scriptDir) async {
    for (final param in func.params) {
      if (param.type != 'map') continue;
      if (param.mapProvider.isEmpty) continue;
      if (param.mapOptions.isNotEmpty) continue;
      try {
        final opts = await svc.queryParamOptions(func.funcName, param.name);
        _dynamicOptions[param.name] = opts;
        final current = _values[param.name] ?? '';
        final vals = opts.map((e) => e.value).toSet();
        if ((current.isEmpty || !vals.contains(current)) && opts.isNotEmpty) {
          _values[param.name] = opts.first.value;
        }
      } catch (_) {
        _dynamicOptions[param.name] = const [];
      }
    }
    notifyListeners();
  }

  void setValue(String name, String value) {
    if (_values[name] == value) return;
    _values[name] = value;
    notifyListeners();
  }

  void setBatchMode(bool value) {
    if (_batchMode == value) return;
    _batchMode = value;
    notifyListeners();
  }

  Map<String, String> collectParams() {
    final result = <String, String>{};
    for (final param in func.params) {
      final name = param.name;
      final type = param.type;
      if (type == 'boolean' || type == 'list' || type == 'map') {
        result[name] = _values[name] ?? '';
      } else {
        result[name] = _textCtrls[name]?.text.trim() ?? '';
      }
    }
    return result;
  }

  Future<void> run(ShellService svc, AppSettings settings) async {
    if (_processing) return;
    _processing = true;
    _status = 'Processing...';
    notifyListeners();
    try {
      final params = collectParams();
      final res = _batchMode
          ? await _runBatch(svc, settings, params)
          : await _runSingle(svc, settings, params);

      final code = res['code'] as int? ?? 0;
      final time = res['time'] as double? ?? 0.0;
      if (code == 0) {
        if (_batchMode) {
          final sCount = res['successCount'] as int? ?? 0;
          final fCount = res['failCount'] as int? ?? 0;
          _status =
              'Completed \u00b7 ${time.toStringAsFixed(3)}s\nSuccess $sCount \u00b7 Failed $fCount';
        } else {
          _status = 'Completed \u00b7 ${time.toStringAsFixed(3)}s';
        }
      } else {
        final output = res['output'] as String? ?? '';
        _status = 'Failed \u00b7 code $code${output.isEmpty ? '' : '\n$output'}';
      }
    } catch (e) {
      _status = 'Exception\n$e';
    } finally {
      _processing = false;
      notifyListeners();
    }
  }

  Future<Map<String, dynamic>> _runSingle(
      ShellService svc, AppSettings settings, Map<String, String> params) async {
    final paramsList =
        params.entries.map((entry) => '${entry.key}:${entry.value}').toList();
    return await svc.runFunction(
      funcName: func.funcName,
      params: paramsList,
      scriptDir: settings.scriptDir,
    );
  }

  Future<Map<String, dynamic>> _runBatch(
      ShellService svc, AppSettings settings, Map<String, String> params) async {
    final pathParams =
        func.params.where((param) => param.type == 'path').toList();
    final inputName = pathParams.isNotEmpty ? pathParams[0].name : 'InputFile';
    final outputName =
        pathParams.length > 1 ? pathParams[1].name : 'OutputFile';
    final inputFolder = params[inputName] ?? '';
    final outputFolder = params[outputName] ?? '';
    final extra = params.entries
        .where((entry) => entry.key != inputName && entry.key != outputName)
        .map((entry) => '${entry.key}:${entry.value}')
        .toList();
    return await svc.runBatch(
      funcName: func.funcName,
      inputFolder: inputFolder,
      outputFolder: outputFolder,
      extraParams: extra,
      scriptDir: settings.scriptDir,
    );
  }

  void disposeControllers() {
    for (final c in _textCtrls.values) {
      c.dispose();
    }
    _textCtrls.clear();
  }

  @override
  void dispose() {
    disposeControllers();
    super.dispose();
  }
}
