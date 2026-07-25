import 'dart:convert';
import 'package:flutter/foundation.dart';
import 'package:flutter/services.dart';
import '../../core/app_settings.dart';
import '../../core/shell_service.dart';
import '../../shared/widgets/message_card.dart';

typedef MessageEntry = ({
  int id,
  MessageType type,
  String title,
  List<String> lines,
});

enum ConsoleState { idle, inputPath, selectFunction, inputParam, processing }

class PendingTask {
  final String? funcName;
  final Map<String, String>? params;
  final bool isBatch;
  final List<PendingTask>? tasks;

  const PendingTask({
    this.funcName,
    this.params,
    this.isBatch = false,
    this.tasks,
  });

  bool get isSingle => funcName != null;
  bool get isMultiTask => tasks != null && tasks!.isNotEmpty;
}

class ShellNotifier extends ChangeNotifier {
  ShellNotifier(this._settings);

  final AppSettings _settings;
  final _shellService = ShellService();

  final _messages = <MessageEntry>[];
  List<MessageEntry> get messages => List.unmodifiable(_messages);
  int _nextMsgId = 0;

  ConsoleState currentState = ConsoleState.idle;

  Map<String, String> _locCache = {};
  String _loc(String key) => _locCache[key] ?? key;

  String _locFormat(String key, List<String> args) {
    String str = _loc(key);
    for (final arg in args) {
      str = str.replaceFirst('{}', arg);
    }
    return str;
  }

  final List<String> _pendingPaths = [];
  int _currentPathIndex = 0;
  double _totalTime = 0.0;

  String currentPath = '';
  bool isFolder = false;
  List<dynamic> matchedFunctions = [];
  Map<dynamic, dynamic>? selectedFunction;

  int currentParamIndex = 0;
  List<dynamic> currentParamsList = [];
  Map<String, String> collectedParams = {};

  List<PendingTask>? _composeTasks;
  int _composeTotal = 0;

  bool get isProcessing => currentState == ConsoleState.processing;
  bool get isConfigured => _settings.isConfigured;

  Map<dynamic, dynamic>? get currentParaming =>
      currentState == ConsoleState.inputParam &&
              currentParamIndex < currentParamsList.length
          ? currentParamsList[currentParamIndex] as Map<dynamic, dynamic>
          : null;

  Future<bool> _initialize({bool forceReload = false}) async {
    final (trans, ok) = await _shellService.initialize(
      soPath: _settings.soPath,
      scriptDir: _settings.scriptDir,
      forceReload: forceReload,
    );

    if (ok) {
      _locCache = trans.translations;
      final sys = trans.sysInfo;
      _addLog(MessageType.verbosity,
          _locFormat('shell.general.system_info', [
            sys['kernel'] ?? '?', sys['shell'] ?? '?',
            sys['script'] ?? '?', sys['arch'] ?? '?',
          ]), []);
      _addLog(MessageType.verbosity, _loc('shell.general.init_success'),
          [_locFormat('shell.general.init_time', [trans.initTime.toStringAsFixed(3)])]);
    }
    return ok;
  }

  Future<void> launch() async {
    if (!isConfigured) return;

    _messages.clear();
    _pendingPaths.clear();
    _currentPathIndex = 0;
    _totalTime = 0.0;
    currentState = ConsoleState.processing;
    notifyListeners();

    if (!await _initialize(forceReload: true)) return;

    _addLog(MessageType.input, _loc('shell.general.input_prompt'), [_loc('shell.general.input_desc')]);
    currentState = ConsoleState.inputPath;
    notifyListeners();
  }

  Future<void> launchWithConfig(PendingTask task) async {
    if (task.isMultiTask) {
      await _processMultiTask(task.tasks!);
      return;
    }

    _messages.clear();
    _totalTime = 0.0;
    currentState = ConsoleState.processing;
    notifyListeners();

    if (!await _initialize()) return;

    final funcName = task.funcName!;
    final params = task.params!;
    final isBatch = task.isBatch;
    double time = 0.0;

    try {
      if (isBatch) {
        final inPath = params['InputFile'] ?? '';
        final outPath = params['OutputFile'] ?? '';
        final extra = params.entries
            .where((e) => e.key != 'InputFile' && e.key != 'OutputFile')
            .map((e) => '${e.key}:${e.value}')
            .toList();

        final res = await _shellService.runBatch(
          funcName: funcName,
          inputFolder: inPath,
          outputFolder: outPath,
          extraParams: extra,
          scriptDir: _settings.scriptDir,
        );

        time = (res['time'] as double?) ?? 0.0;
        final code = res['code'] as int? ?? 0;

        if (code == 0) {
          final total = (res['successCount'] as int? ?? 0) + (res['failCount'] as int? ?? 0);
          _addLog(MessageType.success, _locFormat('shell.general.batch_complete', [total.toString()]),
              [_locFormat('shell.general.batch_detail', [
                (res['successCount'] as int? ?? 0).toString(),
                (res['failCount'] as int? ?? 0).toString(),
              ])]);
          _addLog(MessageType.success, _loc('shell.general.command_exec_success'),
              [_locFormat('shell.general.command_exec_time', [time.toStringAsFixed(3)])]);
        } else {
          _handleExecError(code, res['output'] as String?);
        }
      } else {
        final paramsList = params.entries.map((e) => '${e.key}:${e.value}').toList();

        final res = await _shellService.runFunction(
          funcName: funcName,
          params: paramsList,
          scriptDir: _settings.scriptDir,
        );

        time = (res['time'] as double?) ?? 0.0;
        final code = res['code'] as int? ?? 0;

        if (code == 0) {
          _addLog(MessageType.success, _loc('shell.general.command_exec_success'),
              [_locFormat('shell.general.command_exec_time', [time.toStringAsFixed(3)])]);
        } else {
          _handleExecError(code, res['output'] as String?);
        }
      }
    } catch (e) {
      _addLog(MessageType.error, _loc('shell.general.exec_exception'), [e.toString()]);
    }

    _totalTime += time;
    _addLog(MessageType.verbosity, _loc('shell.general.all_done_title'),
        [_locFormat('shell.general.all_done_time', [_totalTime.toStringAsFixed(3)])]);
    _addLog(MessageType.input, _loc('shell.general.pause'), []);
    _addLog(MessageType.success, _loc('shell.general.succeeded'), []);

    currentState = ConsoleState.idle;
    notifyListeners();
  }

  Future<void> _processMultiTask(List<PendingTask> tasks) async {
    _messages.clear();
    _totalTime = 0.0;
    currentState = ConsoleState.processing;
    notifyListeners();

    if (!await _initialize()) return;

    _composeTasks = List.from(tasks);
    _composeTotal = tasks.length;
    await _executeNextComposeTask();
  }

  Future<void> _executeNextComposeTask() async {
    if (_composeTasks == null || _composeTasks!.isEmpty) {
      _addLog(MessageType.verbosity, _loc('shell.general.all_done_title'),
          [_locFormat('shell.general.all_done_time', [_totalTime.toStringAsFixed(3)])]);
      _addLog(MessageType.input, _loc('shell.general.pause'), []);
      _addLog(MessageType.success, _loc('shell.general.succeeded'), []);
      currentState = ConsoleState.idle;
      _composeTasks = null;
      _composeTotal = 0;
      notifyListeners();
      return;
    }

    final task = _composeTasks!.removeAt(0);
    final idx = _composeTotal - _composeTasks!.length;

    _addLog(MessageType.verbosity,
        _locFormat('shell.general.executing_title', [idx.toString(), _composeTotal.toString()]),
        [task.funcName ?? '']);

    double time = 0.0;

    try {
      if (task.isBatch) {
        final inPath = task.params!['InputFile'] ?? '';
        final outPath = task.params!['OutputFile'] ?? '';
        final extra = task.params!.entries
            .where((e) => e.key != 'InputFile' && e.key != 'OutputFile')
            .map((e) => '${e.key}:${e.value}')
            .toList();

        final res = await _shellService.runBatch(
          funcName: task.funcName!,
          inputFolder: inPath,
          outputFolder: outPath,
          extraParams: extra,
          scriptDir: _settings.scriptDir,
        );

        time = (res['time'] as double?) ?? 0.0;
        final code = res['code'] as int? ?? 0;

        if (code == 0) {
          final total = (res['successCount'] as int? ?? 0) + (res['failCount'] as int? ?? 0);
          _addLog(MessageType.success, _locFormat('shell.general.batch_complete', [total.toString()]),
              [_locFormat('shell.general.batch_detail', [
                (res['successCount'] as int? ?? 0).toString(),
                (res['failCount'] as int? ?? 0).toString(),
              ])]);
          _addLog(MessageType.success, _loc('shell.general.command_exec_success'),
              [_locFormat('shell.general.command_exec_time', [time.toStringAsFixed(3)])]);
        } else {
          _handleExecError(code, res['output'] as String?);
        }
      } else {
        final paramsList = task.params!.entries.map((e) => '${e.key}:${e.value}').toList();

        final res = await _shellService.runFunction(
          funcName: task.funcName!,
          params: paramsList,
          scriptDir: _settings.scriptDir,
        );

        time = (res['time'] as double?) ?? 0.0;
        final code = res['code'] as int? ?? 0;

        if (code == 0) {
          _addLog(MessageType.success, _loc('shell.general.command_exec_success'),
              [_locFormat('shell.general.command_exec_time', [time.toStringAsFixed(3)])]);
        } else {
          _handleExecError(code, res['output'] as String?);
        }
      }
    } catch (e) {
      _addLog(MessageType.error, _loc('shell.general.exec_exception'), [e.toString()]);
    }

    _totalTime += time;
    await _executeNextComposeTask();
  }

  Future<void> submitInput(dynamic value) async {
    switch (currentState) {
      case ConsoleState.inputPath:
        await _handlePathSubmit(value as String);
      case ConsoleState.selectFunction:
        await _handleFunctionSubmit(value as Map<dynamic, dynamic>?);
      case ConsoleState.inputParam:
        await _handleParamSubmit(value as String);
      default:
        break;
    }
  }

  Future<void> _handlePathSubmit(String path) async {
    if (path.isNotEmpty) {
      final normalized = _shellService.normalizePath(path);
      _pendingPaths.add(normalized);
      _addLog(MessageType.verbosity, _loc('shell.general.path'), [normalized]);
      notifyListeners();
      return;
    }

    _addLog(MessageType.verbosity, _loc('shell.general.path'), []);
    notifyListeners();

    if (_pendingPaths.isEmpty) {
      _addLog(MessageType.verbosity, _loc('shell.general.parse_done_title'),
          [_locFormat('shell.general.parse_done_desc', ['0'])]);
      _finishAll();
      return;
    }

    _addLog(MessageType.verbosity, _loc('shell.general.parse_done_title'),
        [_locFormat('shell.general.parse_done_desc', [_pendingPaths.length.toString()])]);
    notifyListeners();
    await _startNextPath();
  }

  Future<void> _startNextPath() async {
    if (_currentPathIndex >= _pendingPaths.length) {
      _finishAll();
      return;
    }

    currentPath = _pendingPaths[_currentPathIndex];
    currentState = ConsoleState.processing;
    notifyListeners();

    _addLog(MessageType.verbosity,
        _locFormat('shell.general.executing_title', [
          (_currentPathIndex + 1).toString(),
          _pendingPaths.length.toString(),
        ]),
        [currentPath]);

    try {
      final result = await _shellService.matchFunctions(currentPath, _settings.scriptDir);

      if (result.groups.isEmpty) {
        _addLog(MessageType.error, _loc('shell.general.invalid_input'),
            [_loc('shell.error.file_not_exist')]);
        _currentPathIndex++;
        notifyListeners();
        await _startNextPath();
        return;
      }

      final allFuncs = <dynamic>[];
      for (final g in result.groups) {
        for (final item in g.items) {
          allFuncs.add(item.raw);
        }
      }

      matchedFunctions = allFuncs;
      isFolder = result.isFolder;

      _addLog(MessageType.input, _loc('shell.general.select_func_prompt'),
          [_loc('shell.general.select_func_desc')]);
      currentState = ConsoleState.selectFunction;
    } catch (e) {
      _addLog(MessageType.error, _loc('shell.general.exec_exception'), [e.toString()]);
      _currentPathIndex++;
      notifyListeners();
      await _startNextPath();
      return;
    }

    notifyListeners();
  }

  Future<void> _handleFunctionSubmit(Map<dynamic, dynamic>? func) async {
    if (func == null) {
      _addLog(MessageType.verbosity, _loc('shell.general.enumeration'), []);
      _addLog(MessageType.warning, _loc('shell.general.func_skipped'),
          [_loc('shell.general.func_skipped_desc')]);
      _currentPathIndex++;
      notifyListeners();
      await _startNextPath();
      return;
    }

    selectedFunction = func;
    currentParamsList = (func['params'] as List<dynamic>?) ?? [];
    collectedParams.clear();
    currentParamIndex = 0;

    _addLog(MessageType.verbosity, _loc('shell.general.enumeration'), [func['localizedName'] ?? '']);
    _checkNextParamOrExecute();
  }

  Future<void> _handleParamSubmit(String value) async {
    final param = currentParamsList[currentParamIndex] as Map;
    final String type = param['type'] ?? 'string';
    final String locName = param['localizedName'] ?? param['name'];

    if (value.isEmpty && type != 'string' && type != 'path') {
      _addLog(MessageType.error, _loc('shell.general.invalid_input'),
          [_loc('shell.general.cannot_be_empty')]);
      notifyListeners();
      return;
    }

    _addLog(MessageType.verbosity, locName, [value]);
    collectedParams[param['name'] as String] = value;
    currentParamIndex++;
    _checkNextParamOrExecute();
  }

  void _checkNextParamOrExecute() {
    while (currentParamIndex < currentParamsList.length) {
      final param = currentParamsList[currentParamIndex] as Map;
      final String name = param['name'];
      final String type = param['type'];
      final bool isReq = param['required'] == true;
      final String uiNo = param['ui_no'] ?? '';
      final String def = param['defaultValue'] ?? '';
      final String locName = param['localizedName'] ?? name;

      bool skipInput = false;
      String finalValue = def;

      if (!isReq && def.isEmpty) skipInput = true;

      if (uiNo.isNotEmpty && !skipInput) {
        String depKey = uiNo;
        bool negate = false;
        if (depKey.startsWith('!')) {
          negate = true;
          depKey = depKey.substring(1);
        }
        final depVal = collectedParams[depKey];
        final isTrue = depVal == 'true' || depVal == '1';
        if (negate ? !isTrue : isTrue) skipInput = true;
      }

      if (type == 'path' && !skipInput) {
        if (currentParamIndex == 0) {
          finalValue = currentPath;
          skipInput = true;
        } else if (currentParamIndex == 1) {
          if (isFolder) {
            finalValue = '$currentPath/.dst';
          } else {
            final paramFolder = param['folder'] == true;
            final exts = param['extensions'] ?? [];
            if (paramFolder) {
              finalValue = '$currentPath.bundle';
            } else {
              final ext = exts.isNotEmpty ? exts.first : '.out';
              finalValue = currentPath.replaceAll(RegExp(r'\.[^\.]+$'), '') + ext;
            }
          }
          skipInput = true;
        }
      }

      if (type == 'map' && !skipInput) {
        final mapList = (param['map'] as List<dynamic>?) ?? [];
        if (mapList.isNotEmpty && finalValue.isEmpty) {
          final firstEntry = mapList.first as Map<dynamic, dynamic>;
          finalValue = (firstEntry['value'] as String?) ?? '';
        }
      }

      if (skipInput) {
        final logTitle = currentParamIndex == 0
            ? _loc('shell.general.arg_obtained')
            : _loc('shell.general.arg_generated');
        _addLog(MessageType.verbosity, logTitle.replaceFirst('{}', locName), [finalValue]);
        collectedParams[name] = finalValue;
        currentParamIndex++;
      } else {
        _addLog(MessageType.input, _locFormat('shell.general.arg_need_input', [locName]), []);
        currentState = ConsoleState.inputParam;
        notifyListeners();
        return;
      }
    }

    notifyListeners();
    _executeFunction();
  }

  void _handleExecError(int code, String? output) {
    if (code == 1) {
      _addLog(MessageType.error, _loc('shell.general.invalid_input'),
          [_loc('shell.error.file_not_exist')]);
    } else if (code == 2) {
      _addLog(MessageType.error, _loc('shell.error.function_not_found'), []);
    } else {
      _addLog(MessageType.error, _loc('shell.error.execution_failed'), [output ?? '']);
    }
  }

  Future<void> _executeFunction() async {
    currentState = ConsoleState.processing;
    notifyListeners();

    final funcName = selectedFunction!['funcName'] as String;
    double time = 0.0;

    try {
      if (isFolder) {
        final extraParams = currentParamsList.skip(2).map((raw) {
          final p = raw as Map;
          return '${p['name']}:${collectedParams[p['name'] as String]}';
        }).toList();

        final outFolder = collectedParams[currentParamsList[1]['name']] ?? '$currentPath/.dst';

        final res = await _shellService.runBatch(
          funcName: funcName,
          inputFolder: currentPath,
          outputFolder: outFolder,
          extraParams: extraParams,
          scriptDir: _settings.scriptDir,
        );

        final code = res['code'] as int? ?? 0;
        time = res['time'] as double? ?? 0.0;

        if (code == 0) {
          final sCount = res['successCount'] as int? ?? 0;
          final fCount = res['failCount'] as int? ?? 0;
          _addLog(MessageType.success,
              _locFormat('shell.general.batch_complete', [(sCount + fCount).toString()]),
              [_locFormat('shell.general.batch_detail', [sCount.toString(), fCount.toString()])]);
          _addLog(MessageType.success, _loc('shell.general.command_exec_success'),
              [_locFormat('shell.general.command_exec_time', [time.toStringAsFixed(3)])]);
        } else {
          _handleExecError(code, res['output'] as String?);
        }
      } else {
        final paramsList = collectedParams.entries.map((e) => '${e.key}:${e.value}').toList();

        final res = await _shellService.runFunction(
          funcName: funcName,
          params: paramsList,
          scriptDir: _settings.scriptDir,
        );

        final code = res['code'] as int? ?? 0;
        time = res['time'] as double? ?? 0.0;

        if (code == 0) {
          _addLog(MessageType.success, _loc('shell.general.command_exec_success'),
              [_locFormat('shell.general.command_exec_time', [time.toStringAsFixed(3)])]);
        } else {
          _handleExecError(code, res['output'] as String?);
        }
      }
    } catch (e) {
      _addLog(MessageType.error, _loc('shell.general.exec_exception'), [e.toString()]);
    }

    _totalTime += time;
    _currentPathIndex++;
    await _startNextPath();
  }

  void _finishAll() {
    _addLog(MessageType.verbosity, _loc('shell.general.all_done_title'),
        [_locFormat('shell.general.all_done_time', [_totalTime.toStringAsFixed(3)])]);
    _addLog(MessageType.input, _loc('shell.general.pause'), []);
    _addLog(MessageType.success, _loc('shell.general.succeeded'), []);
    _pendingPaths.clear();
    currentState = ConsoleState.idle;
    notifyListeners();
  }

  void _addLog(MessageType type, String title, List<String> lines) {
    final clean = lines.where((l) => l.trim().isNotEmpty).toList();
    _messages.add((id: _nextMsgId++, type: type, title: title, lines: clean));
  }

  @override
  void dispose() {
    super.dispose();
  }
}
