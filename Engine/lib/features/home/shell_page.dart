import 'dart:convert';
import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import '../../l10n/app_localizations.dart';
import '../../core/app_settings.dart';
import '../../shared/widgets/message_card.dart';
import 'shell_notifier.dart';
import 'shell_task_store.dart';

class ShellPage extends StatefulWidget {
  const ShellPage({super.key, required this.settings, required this.localeNotifier});
  final AppSettings settings;
  final dynamic localeNotifier;

  @override
  State<ShellPage> createState() => _ShellPageState();
}

class _ShellPageState extends State<ShellPage> {
  late final ShellTaskStore _store;
  final _scaffoldKey = GlobalKey<ScaffoldState>();

  @override
  void initState() {
    super.initState();
    _store = ShellTaskStore(widget.settings);
  }

  @override
  void dispose() {
    _store.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final l = AppLocalizations.of(context)!;
    return ListenableBuilder(
      listenable: _store,
      builder: (context, _) {
        return Scaffold(
          key: _scaffoldKey,
          appBar: AppBar(
            centerTitle: false,
            title: Text(l.shellCardTitle),
            actions: [
              Builder(
                builder: (innerContext) => IconButton(
                  icon: const Icon(Icons.account_tree_outlined),
                  tooltip: '任务',
                  onPressed: () =>
                      Scaffold.of(innerContext).openEndDrawer(),
                ),
              ),
            ],
          ),
          endDrawer: _ShellTasksDrawer(store: _store),
          body: IndexedStack(
            index: _store.activeIndex,
            children: [
              for (final task in _store.tasks)
                _ShellTaskView(notifier: task.notifier),
            ],
          ),
        );
      },
    );
  }
}

class _ShellTaskView extends StatefulWidget {
  const _ShellTaskView({required this.notifier});
  final ShellNotifier notifier;

  @override
  State<_ShellTaskView> createState() => _ShellTaskViewState();
}

class _ShellTaskViewState extends State<_ShellTaskView> {
  final _scrollCtrl = ScrollController();
  final _seenIds = <int>{};

  @override
  void initState() {
    super.initState();
    widget.notifier.addListener(_scrollToBottom);
  }

  void _scrollToBottom() {
    WidgetsBinding.instance.addPostFrameCallback((_) {
      if (_scrollCtrl.hasClients && _scrollCtrl.position.maxScrollExtent > 0) {
        _scrollCtrl.animateTo(_scrollCtrl.position.maxScrollExtent,
            duration: const Duration(milliseconds: 220), curve: Curves.easeOut);
      }
    });
  }

  @override
  void dispose() {
    widget.notifier.removeListener(_scrollToBottom);
    _scrollCtrl.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final l = AppLocalizations.of(context)!;
    final notifier = widget.notifier;
    return ListenableBuilder(
      listenable: notifier,
      builder: (context, _) {
        return Column(children: [
          Expanded(
            child: notifier.messages.isEmpty
                ? const SizedBox.shrink()
                : ListView.builder(
                    controller: _scrollCtrl,
                    padding: const EdgeInsets.fromLTRB(16, 8, 16, 8),
                    itemCount: notifier.messages.length,
                    itemBuilder: (_, i) {
                      final m = notifier.messages[i];
                      final isNew = !_seenIds.contains(m.id);
                      _seenIds.add(m.id);
                      return isNew
                          ? _AnimatedMessageCard(key: ValueKey(m.id), entry: m)
                          : Padding(
                              padding: const EdgeInsets.only(bottom: 8),
                              child: RepaintBoundary(
                                  child: MessageCard(
                                      type: m.type, title: m.title, lines: m.lines)),
                            );
                    },
                  ),
          ),
          LinearProgressIndicator(
              minHeight: 2, value: notifier.isProcessing ? null : 0.0),
          _ShellBottomBar(notifier: notifier),
        ]);
      },
    );
  }
}

class _AnimatedMessageCard extends StatefulWidget {
  const _AnimatedMessageCard({super.key, required this.entry});
  final MessageEntry entry;

  @override
  State<_AnimatedMessageCard> createState() => _AnimatedMessageCardState();
}

class _AnimatedMessageCardState extends State<_AnimatedMessageCard>
    with SingleTickerProviderStateMixin {
  late final AnimationController _ctrl;
  late final Animation<double> _opacity;
  late final Animation<Offset> _slide;

  @override
  void initState() {
    super.initState();
    _ctrl = AnimationController(vsync: this, duration: const Duration(milliseconds: 320));
    _opacity = CurvedAnimation(parent: _ctrl, curve: Curves.easeOut);
    _slide = Tween<Offset>(begin: const Offset(0, 0.12), end: Offset.zero)
        .animate(CurvedAnimation(parent: _ctrl, curve: Curves.easeOutCubic));
    _ctrl.forward();
  }

  @override
  void dispose() {
    _ctrl.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: const EdgeInsets.only(bottom: 8),
      child: FadeTransition(
        opacity: _opacity,
        child: SlideTransition(
          position: _slide,
          child: RepaintBoundary(
            child: MessageCard(
                type: widget.entry.type,
                title: widget.entry.title,
                lines: widget.entry.lines),
          ),
        ),
      ),
    );
  }
}

class _ShellBottomBar extends StatefulWidget {
  const _ShellBottomBar({required this.notifier});
  final ShellNotifier notifier;

  @override
  State<_ShellBottomBar> createState() => _ShellBottomBarState();
}

class _ShellBottomBarState extends State<_ShellBottomBar> {
  static const _channel = MethodChannel('com.liar.byzymztools/shell');
  final _textCtrl = TextEditingController();
  Map<dynamic, dynamic>? _selectedFunc;
  String _selectedMapValue = '';
  String _dynamicMapKey = '';
  List<MapEntry<String, String>>? _dynamicMapOptions;
  bool _dynamicMapLoading = false;

  @override
  void didUpdateWidget(_ShellBottomBar oldWidget) {
    super.didUpdateWidget(oldWidget);
    _checkMapParam();
  }

  void _checkMapParam() {
    final param = widget.notifier.currentParaming;
    if (param == null) return;
    final type = (param['type'] ?? 'string') as String;
    if (type != 'map') return;
    final mapList = (param['map'] as List<dynamic>?) ?? [];
    if (mapList.isNotEmpty) return;
    final mapProvider = (param['mapProvider'] ?? '') as String;
    if (mapProvider.isEmpty) return;
    final funcName = widget.notifier.selectedFunction?['funcName'] as String? ?? '';
    final paramName = (param['name'] ?? '') as String;
    final key = '$funcName:$paramName';
    if (key == _dynamicMapKey) return;
    _dynamicMapKey = key;
    _dynamicMapLoading = true;
    _dynamicMapOptions = null;
    _selectedMapValue = '';
    _fetchMapOptions(funcName, paramName);
  }

  Future<void> _fetchMapOptions(String funcName, String paramName) async {
    try {
      final raw = await _channel.invokeMethod<String>('queryParamOptions', {
        'funcName': funcName,
        'paramName': paramName,
      });
      final list = jsonDecode(raw ?? '[]') as List<dynamic>;
      final options = list.map((e) {
        final m = e as Map<dynamic, dynamic>;
        return MapEntry((m['display'] as String?) ?? '', (m['value'] as String?) ?? '');
      }).toList();
      if (!mounted) return;
      setState(() {
        _dynamicMapOptions = options;
        _dynamicMapLoading = false;
        if (options.isNotEmpty) _selectedMapValue = options.first.value;
      });
    } catch (_) {
      if (!mounted) return;
      setState(() {
        _dynamicMapOptions = [];
        _dynamicMapLoading = false;
      });
    }
  }

  @override
  void dispose() {
    _textCtrl.dispose();
    super.dispose();
  }

  void _submit() {
    final state = widget.notifier.currentState;
    if (state == ConsoleState.inputPath || state == ConsoleState.inputParam) {
      final param = widget.notifier.currentParaming;
      final type = (param?['type'] ?? 'string') as String;
      String value = _textCtrl.text.trim();
      if (type == 'map') value = _selectedMapValue;
      widget.notifier.submitInput(value);
      _textCtrl.clear();
      _selectedMapValue = '';
      _dynamicMapKey = '';
    } else if (state == ConsoleState.selectFunction) {
      widget.notifier.submitInput(_selectedFunc);
      setState(() => _selectedFunc = null);
    }
  }

  @override
  Widget build(BuildContext context) {
    final cs = Theme.of(context).colorScheme;
    final l = AppLocalizations.of(context)!;
    final state = widget.notifier.currentState;

    Widget inputWidget;
    VoidCallback? onFabPressed;
    IconData fabIcon = Icons.send;

    switch (state) {
      case ConsoleState.idle:
        inputWidget = const SizedBox.shrink();
        onFabPressed = widget.notifier.launch;

      case ConsoleState.processing:
        inputWidget = Center(
            child: Text(l.processing, style: TextStyle(color: cs.outline)));
        onFabPressed = null;
        fabIcon = Icons.local_cafe_outlined;

      case ConsoleState.inputPath:
        inputWidget = TextField(
          controller: _textCtrl,
          decoration: InputDecoration(
            hintText: l.pathHint,
            border: OutlineInputBorder(
                borderRadius: BorderRadius.circular(8),
                borderSide: BorderSide.none),
            filled: true,
            fillColor: cs.surfaceContainerHighest,
            contentPadding: const EdgeInsets.symmetric(horizontal: 12),
          ),
          onSubmitted: (_) => _submit(),
        );
        onFabPressed = _submit;
        fabIcon = Icons.send;

      case ConsoleState.selectFunction:
        inputWidget = Container(
          padding: const EdgeInsets.symmetric(horizontal: 12),
          decoration: BoxDecoration(
              color: cs.surfaceContainerHighest,
              borderRadius: BorderRadius.circular(8)),
          child: DropdownButtonHideUnderline(
            child: DropdownButton<Map<dynamic, dynamic>>(
              value: _selectedFunc,
              hint: Text(l.optionHint),
              isExpanded: true,
              items: [
                DropdownMenuItem(value: null, child: Text(l.skipOption)),
                ...widget.notifier.matchedFunctions.map((e) {
                  final m = e as Map<dynamic, dynamic>;
                  return DropdownMenuItem(
                      value: m, child: Text(m['localizedName'] as String? ?? ''));
                }),
              ],
              onChanged: (v) => setState(() => _selectedFunc = v),
            ),
          ),
        );
        onFabPressed = _submit;
        fabIcon = Icons.send;

      case ConsoleState.inputParam:
        final param = widget.notifier.currentParaming;
        final type = (param?['type'] ?? 'string') as String;
        final def = (param?['defaultValue'] ?? '') as String;

        if (type == 'boolean') {
          inputWidget = Row(children: [
            Expanded(
                child: FilledButton.tonal(
                    onPressed: () {
                      _textCtrl.text = 'false';
                      _submit();
                    },
                    child: const Text('No'))),
            const SizedBox(width: 8),
            Expanded(
                child: FilledButton(
                    onPressed: () {
                      _textCtrl.text = 'true';
                      _submit();
                    },
                    child: const Text('Yes'))),
          ]);
          onFabPressed = null;
          fabIcon = Icons.send;
        } else if (type == 'list') {
          final options = (param?['list'] as List<dynamic>?)
                  ?.map((e) => e.toString())
                  .toList() ??
              [];
          if (_textCtrl.text.isEmpty) {
            if (def.isNotEmpty && options.contains(def)) {
              _textCtrl.text = def;
            } else if (options.isNotEmpty) {
              _textCtrl.text = options.first;
            }
          } else if (options.isNotEmpty && !options.contains(_textCtrl.text)) {
            _textCtrl.text = options.first;
          }
          final safeValue =
              options.contains(_textCtrl.text) ? _textCtrl.text : options.firstOrNull;
          inputWidget = Container(
            padding: const EdgeInsets.symmetric(horizontal: 12),
            decoration: BoxDecoration(
                color: cs.surfaceContainerHighest,
                borderRadius: BorderRadius.circular(8)),
            child: DropdownButtonHideUnderline(
              child: options.isEmpty
                  ? Align(
                      alignment: Alignment.centerLeft,
                      child: Text('(no options)',
                          style: TextStyle(color: cs.outline)))
                  : DropdownButton<String>(
                      value: safeValue,
                      isExpanded: true,
                      items: options
                          .map((e) => DropdownMenuItem(value: e, child: Text(e)))
                          .toList(),
                      onChanged: (v) => setState(() => _textCtrl.text = v ?? '')),
            ),
          );
          onFabPressed = options.isNotEmpty ? _submit : null;
          fabIcon = Icons.send;
        } else if (type == 'map') {
          final mapList = (param?['map'] as List<dynamic>?) ?? [];
          if (_dynamicMapLoading) {
            inputWidget = const Center(
                child: SizedBox(
                    height: 24,
                    width: 24,
                    child: CircularProgressIndicator(strokeWidth: 2)));
            onFabPressed = null;
            fabIcon = Icons.send;
          } else {
            final effectiveOptions = mapList.isNotEmpty
                ? mapList.map((e) {
                    final m = e as Map<dynamic, dynamic>;
                    return MapEntry((m['display'] as String?) ?? '',
                        (m['value'] as String?) ?? '');
                  }).toList()
                : (_dynamicMapOptions ?? []);
            if (effectiveOptions.isNotEmpty &&
                (_selectedMapValue.isEmpty ||
                    !effectiveOptions.any((e) => e.value == _selectedMapValue))) {
              _selectedMapValue = effectiveOptions.first.value;
            }
            final safeMapValue = effectiveOptions.any((e) => e.value == _selectedMapValue)
                ? _selectedMapValue
                : effectiveOptions.firstOrNull?.value;
            inputWidget = Container(
              padding: const EdgeInsets.symmetric(horizontal: 12),
              decoration: BoxDecoration(
                  color: cs.surfaceContainerHighest,
                  borderRadius: BorderRadius.circular(8)),
              child: DropdownButtonHideUnderline(
                child: effectiveOptions.isEmpty
                    ? Align(
                        alignment: Alignment.centerLeft,
                        child: Text('(no options)',
                            style: TextStyle(color: cs.outline)))
                    : DropdownButton<String>(
                        value: safeMapValue,
                        isExpanded: true,
                        items: effectiveOptions
                            .map((e) =>
                                DropdownMenuItem(value: e.value, child: Text(e.key)))
                            .toList(),
                        onChanged: (v) {
                          if (v != null) setState(() => _selectedMapValue = v);
                        }),
              ),
            );
            onFabPressed = effectiveOptions.isNotEmpty ? _submit : null;
            fabIcon = Icons.send;
          }
        } else {
          inputWidget = TextField(
            controller: _textCtrl,
            keyboardType:
                type == 'integer' ? TextInputType.number : TextInputType.text,
            decoration: InputDecoration(
              hintText: '$type ($def)',
              border: OutlineInputBorder(
                  borderRadius: BorderRadius.circular(8),
                  borderSide: BorderSide.none),
              filled: true,
              fillColor: cs.surfaceContainerHighest,
              contentPadding: const EdgeInsets.symmetric(horizontal: 12),
            ),
            onSubmitted: (_) {
              if (_textCtrl.text.isEmpty) _textCtrl.text = def;
              _submit();
            },
          );
          onFabPressed = () {
            if (_textCtrl.text.isEmpty) _textCtrl.text = def;
            _submit();
          };
          fabIcon = Icons.send;
        }
    }

    return BottomAppBar(
      elevation: 8,
      padding: const EdgeInsets.fromLTRB(16, 12, 16, 16),
      height: 80,
      child: Row(crossAxisAlignment: CrossAxisAlignment.center, children: [
        Icon(
          switch (state) {
            ConsoleState.idle => Icons.terminal,
            ConsoleState.processing => Icons.local_cafe_outlined,
            _ => Icons.edit_note,
          },
          color: cs.primary,
        ),
        const SizedBox(width: 16),
        Expanded(child: SizedBox(height: 48, child: inputWidget)),
        const SizedBox(width: 16),
        FloatingActionButton(
          elevation: 0,
          highlightElevation: 0,
          backgroundColor: onFabPressed == null
              ? cs.surfaceContainerHighest
              : cs.primaryContainer,
          foregroundColor:
              onFabPressed == null ? cs.outline : cs.onPrimaryContainer,
          onPressed: onFabPressed,
          child: Icon(fabIcon),
        ),
      ]),
    );
  }
}

class _ShellTasksDrawer extends StatelessWidget {
  const _ShellTasksDrawer({required this.store});
  final ShellTaskStore store;

  @override
  Widget build(BuildContext context) {
    final l = AppLocalizations.of(context)!;
    final cs = Theme.of(context).colorScheme;

    return Drawer(
      child: SafeArea(
        child: Column(
          children: [
            SizedBox(
              height: 64,
              child: Padding(
                padding: const EdgeInsets.fromLTRB(24, 4, 24, 4),
                child: Align(
                  alignment: Alignment.centerLeft,
                  child: Text(
                    '任务',
                    maxLines: 1,
                    overflow: TextOverflow.ellipsis,
                    style: Theme.of(context).textTheme.titleLarge,
                  ),
                ),
              ),
            ),

            Expanded(child: ListenableBuilder(
              listenable: store,
              builder: (context, _) {
                return ListView.builder(
                  padding: const EdgeInsets.symmetric(vertical: 8),
                  itemCount: store.length,
                  itemBuilder: (context, index) {
                    final task = store.tasks[index];
                    final selected = index == store.activeIndex;
                    return Padding(
                      padding: const EdgeInsets.symmetric(horizontal: 12),
                      child: SizedBox(
                        height: 56,
                        child: TextButton(
                          style: TextButton.styleFrom(
                            padding: const EdgeInsets.only(left: 16),
                            backgroundColor: selected
                                ? cs.secondaryContainer
                                : null,
                            foregroundColor: selected
                                ? cs.onSecondaryContainer
                                : cs.onSurfaceVariant,
                            shape: RoundedRectangleBorder(
                                borderRadius: BorderRadius.circular(100)),
                          ),
                          onPressed: () {
                            store.select(index);
                            Navigator.of(context).pop();
                          },
                          child: Row(children: [
                            Icon(selected ? Icons.task_alt : Icons.task_outlined,
                                size: 22),
                            const SizedBox(width: 12),
                            Expanded(
                              child: Text(task.name,
                                  maxLines: 1,
                                  overflow: TextOverflow.ellipsis,
                                  softWrap: false,
                                  style: Theme.of(context).textTheme.labelLarge),
                            ),
                            _TaskOverflowMenu(task: task, store: store),
                            const SizedBox(width: 4),
                          ]),
                        ),
                      ),
                    );
                  },
                );
              },
            )),
          ],
        ),
      ),
    );
  }
}

class _TaskOverflowMenu extends StatelessWidget {
  const _TaskOverflowMenu({required this.task, required this.store});
  final ShellTask task;
  final ShellTaskStore store;

  @override
  Widget build(BuildContext context) {
    final l = AppLocalizations.of(context)!;
    return PopupMenuButton<String>(
      icon: const Icon(Icons.more_vert, size: 20),
      tooltip: l.rename,
      onSelected: (action) async {
        switch (action) {
          case 'edit':
            final name = await _promptRename(context, task.name);
            if (name != null) store.rename(task, name);
            break;
          case 'copy':
            store.copy(task);
            break;
          case 'delete':
            final idx = store.tasks.indexOf(task);
            if (idx >= 0) store.remove(idx);
            break;
        }
      },
      itemBuilder: (ctx) => [
        PopupMenuItem(value: 'edit', child: Row(children: [
          const Icon(Icons.edit_outlined, size: 18),
          const SizedBox(width: 8),
          Text(l.rename),
        ])),
        PopupMenuItem(value: 'copy', child: Row(children: [
          const Icon(Icons.copy_outlined, size: 18),
          const SizedBox(width: 8),
          Text(l.copy),
        ])),
        PopupMenuItem(value: 'delete', child: Row(children: [
          const Icon(Icons.delete_outline, size: 18),
          const SizedBox(width: 8),
          Text(l.remove),
        ])),
      ],
    );
  }

  Future<String?> _promptRename(BuildContext context, String current) {
    final l = AppLocalizations.of(context)!;
    final ctrl = TextEditingController(text: current);
    return showDialog<String>(
      context: context,
      builder: (ctx) => AlertDialog(
        title: Text(l.rename),
        content: TextField(
          controller: ctrl,
          autofocus: true,
          decoration: InputDecoration(
              hintText: l.rename, border: const OutlineInputBorder()),
          onSubmitted: (v) => Navigator.of(ctx).pop(v),
        ),
        actions: [
          TextButton(
              onPressed: () => Navigator.of(ctx).pop(), child: Text(l.cancel)),
          FilledButton(
              onPressed: () => Navigator.of(ctx).pop(ctrl.text),
              child: Text(l.save)),
        ],
      ),
    );
  }
}
