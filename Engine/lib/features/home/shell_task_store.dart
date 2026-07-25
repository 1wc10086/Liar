import 'package:flutter/foundation.dart';

import '../../core/app_settings.dart';
import 'shell_notifier.dart';

class ShellTask {
  ShellTask({required this.id, required this.name, required this.notifier});

  final String id;
  String name;
  final ShellNotifier notifier;
}

class ShellTaskStore extends ChangeNotifier {
  ShellTaskStore(this._settings) {
    _tasks.add(ShellTask(
        id: _nextId(),
        name: _defaultName,
        notifier: ShellNotifier(_settings)));
  }

  final AppSettings _settings;
  final List<ShellTask> _tasks = [];
  int _activeIndex = 0;
  int _counter = 0;

  static const String _defaultName = '任务';

  List<ShellTask> get tasks => List.unmodifiable(_tasks);
  int get activeIndex => _activeIndex;
  ShellTask get active => _tasks[_activeIndex];
  int get length => _tasks.length;

  String _nextId() => 'task_${_counter++}';

  void select(int index) {
    if (index < 0 || index >= _tasks.length || index == _activeIndex) return;
    _activeIndex = index;
    notifyListeners();
  }

  int copy(ShellTask? source) {
    final task = ShellTask(
      id: _nextId(),
      name: source == null ? _defaultName : '${source.name} copy',
      notifier: ShellNotifier(_settings),
    );
    _tasks.add(task);
    _activeIndex = _tasks.length - 1;
    notifyListeners();
    return _activeIndex;
  }

  void rename(ShellTask task, String name) {
    final trimmed = name.trim();
    if (trimmed.isEmpty) return;
    task.name = trimmed;
    notifyListeners();
  }

  void remove(int index) {
    if (_tasks.length <= 1) return;
    final task = _tasks.removeAt(index);
    task.notifier.dispose();
    if (_activeIndex >= _tasks.length) _activeIndex = _tasks.length - 1;
    notifyListeners();
  }

  @override
  void dispose() {
    for (final t in _tasks) {
      t.notifier.dispose();
    }
    _tasks.clear();
    super.dispose();
  }
}
