import 'package:flutter/material.dart';
import 'package:m3e_core/m3e_core.dart';

import '../../l10n/app_localizations.dart';
import '../../core/app_settings.dart';
import '../../core/shell_service.dart';
import '../../shared/widgets/shared_drawer.dart';
import 'lab_session.dart';

class LabPage extends StatefulWidget {
  const LabPage({super.key, required this.settings});

  final AppSettings settings;

  @override
  State<LabPage> createState() => _LabPageState();
}

class _LabPageState extends State<LabPage> {
  final _shellService = ShellService();
  final _scaffoldKey = GlobalKey<ScaffoldState>();

  List<FuncGroup> _groups = [];
  FuncGroup? _selectedGroup;
  int _selectedFuncIndex = 0;
  bool _loading = false;

  final Map<String, LabSession> _sessions = {};

  String get _configKey => '${widget.settings.soPath}|${widget.settings.scriptDir}';

  FuncItem? get _selectedFunc {
    final group = _selectedGroup;
    if (group == null || group.items.isEmpty) return null;
    return group.items[_selectedFuncIndex.clamp(0, group.items.length - 1)];
  }

  @override
  void initState() {
    super.initState();
    _loadFunctions();
  }

  @override
  void didUpdateWidget(covariant LabPage oldWidget) {
    super.didUpdateWidget(oldWidget);
    final oldKey = '${oldWidget.settings.soPath}|${oldWidget.settings.scriptDir}';
    if (oldKey != _configKey) {
      _groups = [];
      _selectedGroup = null;
      _selectedFuncIndex = 0;
      _disposeSessions();
      _loadFunctions(force: true);
    }
  }

  @override
  void dispose() {
    _disposeSessions();
    super.dispose();
  }

  void _disposeSessions() {
    for (final s in _sessions.values) {
      s.dispose();
    }
    _sessions.clear();
  }

  Future<void> _loadFunctions({bool force = false}) async {
    if (!widget.settings.isConfigured) return;

    setState(() {
      _loading = true;
    });

    final (_, ok) = await _shellService.initialize(
      soPath: widget.settings.soPath,
      scriptDir: widget.settings.scriptDir,
      forceReload: true,
    );

    if (!ok) {
      if (mounted) setState(() => _loading = false);
      return;
    }

    final result = await _shellService.matchFunctions(
        widget.settings.scriptDir, widget.settings.scriptDir);

    if (!mounted) return;

    setState(() {
      _groups = result.groups;
      _loading = false;
    });
  }

  Future<void> _selectGroup(FuncGroup group) async {
    _scaffoldKey.currentState?.closeDrawer();
    final func = group.items.isNotEmpty ? group.items.first : null;
    if (func == null) return;
    final session = _ensureSession(group, func);
    setState(() {
      _selectedGroup = group;
      _selectedFuncIndex = 0;
    });
    session.loadDynamicMapOptions(_shellService, widget.settings.scriptDir);
  }

  Future<void> _selectFunctionIndex(int index) async {
    final group = _selectedGroup;
    if (group == null) return;
    if (index < 0 || index >= group.items.length) return;
    final func = group.items[index];
    final session = _ensureSession(group, func);
    setState(() {
      _selectedFuncIndex = index;
    });
    session.loadDynamicMapOptions(_shellService, widget.settings.scriptDir);
  }

  LabSession _ensureSession(FuncGroup group, FuncItem func) {
    final existing = _sessions[func.funcName];
    if (existing != null) return existing;
    final session = LabSession(group: group, func: func)..prepare();
    _sessions[func.funcName] = session;
    return session;
  }

  @override
  Widget build(BuildContext context) {
    final l = AppLocalizations.of(context)!;
    final cs = Theme.of(context).colorScheme;
    final group = _selectedGroup;
    final func = _selectedFunc;

    return Scaffold(
      key: _scaffoldKey,
      onDrawerChanged: (_) {},
      appBar: AppBar(
        centerTitle: false,
        leading: IconButton(
          icon: const Icon(Icons.menu),
          onPressed: () {
            _scaffoldKey.currentState?.openDrawer();
          },
        ),
        title: Text(l.labTitle),
      ),
      drawer: SharedDrawer(
        title: l.pluginDrawerTitle,
        items: _groups.map((g) => g.title).toList(),
        selectedValue: _selectedGroup?.title ?? '',
        onSelected: (title) {
          final g = _groups.firstWhere((g) => g.title == title);
          _selectGroup(g);
        },
        loading: _loading,
        emptyMessage: l.noFunctions,
        onRefresh: () => _loadFunctions(force: true),
      ),
      body: !widget.settings.isConfigured
          ? Center(child: Text(l.configureFirst))
          : _loading && _groups.isEmpty
              ? const Center(child: CircularProgressIndicator())
              : group == null || func == null
                  ? Center(
                      child: Text(l.labEmptyHint,
                          style: TextStyle(color: cs.outline)))
                  : Builder(builder: (_) {
                      final session = _sessions[func.funcName];
                      if (session == null) {
                        return const SizedBox.shrink();
                      }
                      return _LabFunctionView(
                        key: ValueKey(func.funcName),
                        group: group,
                        func: func,
                        funcIndex: _selectedFuncIndex,
                        onFuncIndexChange: _selectFunctionIndex,
                        session: session,
                        shellService: _shellService,
                        settings: widget.settings,
                      );
                    }),
    );
  }
}

class _LabFunctionView extends StatelessWidget {
  const _LabFunctionView({
    super.key,
    required this.group,
    required this.func,
    required this.funcIndex,
    required this.onFuncIndexChange,
    required this.session,
    required this.shellService,
    required this.settings,
  });

  final FuncGroup group;
  final FuncItem func;
  final int funcIndex;
  final ValueChanged<int> onFuncIndexChange;
  final LabSession session;
  final ShellService shellService;
  final AppSettings settings;

  @override
  Widget build(BuildContext context) {
    final l = AppLocalizations.of(context)!;
    final cs = Theme.of(context).colorScheme;
    return ListenableBuilder(
      listenable: session,
      builder: (context, _) {
        return ListView(
          padding: const EdgeInsets.fromLTRB(16, 12, 16, 32),
          children: [
            Text(
              group.title,
              maxLines: 2,
              overflow: TextOverflow.ellipsis,
              style: Theme.of(context).textTheme.headlineSmall,
            ),
            const SizedBox(height: 12),
            _FunctionSwitcher(
              group: group,
              selectedIndex: funcIndex,
              onSelected: onFuncIndexChange,
            ),
            const SizedBox(height: 16),
            for (final param in func.params)
              _ParamCard(
                key: ValueKey('${func.funcName}:${param.name}'),
                param: param,
                value: session.values[param.name] ?? '',
                controller: session.controllerOf(param.name),
                options: _resolveOptions(param),
                onChanged: (value) => session.setValue(param.name, value),
              ),
            const SizedBox(height: 8),
            Row(children: [
              M3EToggleButton(
                checked: session.batchMode,
                icon: const Icon(Icons.insert_drive_file_outlined),
                checkedIcon: const Icon(Icons.folder_copy),
                label: Text(session.batchMode ? l.batchModeOn : l.batchModeOff),
                checkedLabel: Text(l.batchModeOn),
                decoration: M3EToggleButtonDecoration.styleFrom(
                  haptic: M3EHapticFeedback.light,
                  motion: M3EMotion.expressiveSpatialDefault,
                  checkedBackgroundColor: cs.primaryContainer,
                  checkedForegroundColor: cs.onPrimaryContainer,
                  backgroundColor: cs.surfaceContainerHighest,
                  foregroundColor: cs.onSurfaceVariant,
                ),
                onCheckedChange: (checked) => session.setBatchMode(checked),
              ),
              const Spacer(),
              M3EFilledButton(
                size: M3EButtonSize.md,
                enabled: !session.processing,
                decoration: M3EButtonDecoration.styleFrom(
                  motion: M3EMotion.expressiveSpatialDefault,
                  haptic: M3EHapticFeedback.medium,
                ),
                onPressed: () => session.run(shellService, settings),
                child: Text(l.startProcess),
              ),
            ]),
            const SizedBox(height: 16),
            AnimatedSwitcher(
              duration: const Duration(milliseconds: 220),
              child: session.status.isEmpty
                  ? const SizedBox.shrink()
                  : Text(
                      session.status,
                      key: ValueKey(session.status),
                      style: Theme.of(context)
                          .textTheme
                          .bodyMedium
                          ?.copyWith(
                            color: session.status.startsWith('Failed') ||
                                    session.status.startsWith('Exception')
                                ? cs.error
                                : cs.primary,
                          ),
                    ),
            ),
          ],
        );
      },
    );
  }

  List<OptionItem> _resolveOptions(FuncParam param) {
    if (param.type == 'map') {
      final staticOpts = param.mapOptions;
      if (staticOpts.isNotEmpty) return staticOpts;
      return session.dynamicOptionsOf(param.name);
    }
    if (param.type == 'list') {
      return param.listOptions
          .map((v) => OptionItem(label: v, value: v))
          .toList();
    }
    return const [];
  }
}

class _FunctionSwitcher extends StatelessWidget {
  const _FunctionSwitcher({
    required this.group,
    required this.selectedIndex,
    required this.onSelected,
  });

  final FuncGroup group;
  final int selectedIndex;
  final ValueChanged<int> onSelected;

  @override
  Widget build(BuildContext context) {
    return M3EToggleButtonGroup(
      type: M3EButtonGroupType.connected,
      size: M3EButtonSize.md,
      selectedIndex: selectedIndex,
      overflow: M3EButtonGroupOverflow.menu,
      overflowMenuStyle: M3EButtonGroupOverflowMenuStyle.popup,
      onSelectedIndexChanged: (index) {
        if (index != null) onSelected(index);
      },
      actions: [
        for (final item in group.items)
          M3EToggleButtonGroupAction(
            icon: const Icon(Icons.auto_awesome),
            checkedIcon: const Icon(Icons.auto_awesome),
            label: Text(item.title, maxLines: 1, overflow: TextOverflow.ellipsis),
            checkedLabel:
                Text(item.title, maxLines: 1, overflow: TextOverflow.ellipsis),
            semanticLabel: item.fullName,
          ),
      ],
    );
  }
}

class _ParamCard extends StatelessWidget {
  const _ParamCard({
    super.key,
    required this.param,
    required this.value,
    required this.controller,
    required this.options,
    required this.onChanged,
  });

  final FuncParam param;
  final String value;
  final TextEditingController? controller;
  final List<OptionItem> options;
  final ValueChanged<String> onChanged;

  bool get _allNumeric {
    if (options.isEmpty) return false;
    return options.every((item) => double.tryParse(item.value) != null);
  }

  @override
  Widget build(BuildContext context) {
    final cs = Theme.of(context).colorScheme;
    final l = AppLocalizations.of(context)!;
    final locName =
        param.localizedName.isEmpty ? param.name : param.localizedName;

    return Card(
      elevation: 0,
      margin: const EdgeInsets.only(bottom: 12),
      shape: RoundedRectangleBorder(
        borderRadius: BorderRadius.circular(20),
        side: BorderSide(color: cs.outlineVariant),
      ),
      child: Padding(
        padding: const EdgeInsets.fromLTRB(16, 14, 16, 16),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Text(
              locName,
              maxLines: 2,
              overflow: TextOverflow.ellipsis,
              style: Theme.of(context).textTheme.titleSmall?.copyWith(
                    color: cs.primary,
                    fontWeight: FontWeight.w700,
                  ),
            ),
            const SizedBox(height: 4),
            Text(
              _description(l),
              style: Theme.of(context)
                  .textTheme
                  .bodySmall
                  ?.copyWith(color: cs.onSurfaceVariant),
            ),
            const SizedBox(height: 14),
            _buildField(context),
          ],
        ),
      ),
    );
  }

  String _description(AppLocalizations l) => switch (param.type) {
        'path' => l.paramPathDesc,
        'integer' => l.paramIntegerDesc,
        'boolean' => l.paramBooleanDesc,
        'list' => l.paramListDesc,
        'map' => l.paramMapDesc,
        _ => l.paramStringDesc,
      };

  Widget _buildField(BuildContext context) => switch (param.type) {
        'boolean' => _SwitchField(value: value, onChanged: onChanged),
        'list' || 'map' when _allNumeric => _SliderField(
            value: value, options: options, onChanged: onChanged),
        'list' || 'map' => _DropdownField(
            key: ValueKey(
                '${param.name}:${options.map((e) => e.value).join('|')}'),
            value: value,
            options: options,
            searchable: options.length > 4,
            onChanged: onChanged),
        _ => _FilledTextField(param: param, controller: controller),
      };
}

class _FilledTextField extends StatelessWidget {
  const _FilledTextField({required this.param, required this.controller});
  final FuncParam param;
  final TextEditingController? controller;

  @override
  Widget build(BuildContext context) {
    final isInt = param.type == 'integer';
    final label = param.type == 'path'
        ? 'Path'
        : isInt
            ? 'Int'
            : 'Str';
    return TextField(
      controller: controller,
      keyboardType: isInt ? TextInputType.number : TextInputType.text,
      decoration: InputDecoration(
        filled: true,
        labelText: label,
        hintText: param.defaultValue,
        border: OutlineInputBorder(
            borderRadius: BorderRadius.circular(16), borderSide: BorderSide.none),
      ),
    );
  }
}

class _SwitchField extends StatelessWidget {
  const _SwitchField({required this.value, required this.onChanged});
  final String value;
  final ValueChanged<String> onChanged;

  @override
  Widget build(BuildContext context) {
    final checked = value == 'true' || value == '1';
    return SwitchListTile(
      value: checked,
      contentPadding: EdgeInsets.zero,
      title: Text(checked ? 'true' : 'false'),
      onChanged: (v) => onChanged(v ? 'true' : 'false'),
    );
  }
}

class _SliderField extends StatelessWidget {
  const _SliderField({
    required this.value,
    required this.options,
    required this.onChanged,
  });
  final String value;
  final List<OptionItem> options;
  final ValueChanged<String> onChanged;

  @override
  Widget build(BuildContext context) {
    final values = options
        .map((item) => double.tryParse(item.value))
        .whereType<double>()
        .toList()
      ..sort();
    if (values.isEmpty) return const SizedBox.shrink();
    final min = values.first;
    final max = values.last;
    final parsed = double.tryParse(value) ?? min;
    final current = parsed.clamp(min, max).toDouble();
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Slider(
          min: min,
          max: max,
          divisions: values.length > 1 ? values.length - 1 : null,
          value: current,
          label: _formatNumber(current),
          onChanged: (v) {
            final nearest = values
                .reduce((a, b) => (a - v).abs() < (b - v).abs() ? a : b);
            onChanged(_formatNumber(nearest));
          },
        ),
        Text(_formatNumber(current),
            style: Theme.of(context).textTheme.bodyMedium),
      ],
    );
  }

  String _formatNumber(double value) =>
      value % 1 == 0 ? value.toInt().toString() : value.toString();
}

class _DropdownField extends StatefulWidget {
  const _DropdownField({
    super.key,
    required this.value,
    required this.options,
    required this.searchable,
    required this.onChanged,
  });
  final String value;
  final List<OptionItem> options;
  final bool searchable;
  final ValueChanged<String> onChanged;

  @override
  State<_DropdownField> createState() => _DropdownFieldState();
}

class _DropdownFieldState extends State<_DropdownField> {
  late final M3EDropdownController<String> _controller;
  late List<M3EDropdownItem<String>> _items;
  String _lastOptionsSignature = '';
  String _lastEmitted = '';

  @override
  void initState() {
    super.initState();
    _controller = M3EDropdownController<String>();
    _lastEmitted = widget.value;
    _rebuildItemsIfNeeded(force: true);
  }

  @override
  void didUpdateWidget(covariant _DropdownField oldWidget) {
    super.didUpdateWidget(oldWidget);
    if (oldWidget.value != widget.value) _lastEmitted = widget.value;
    _rebuildItemsIfNeeded();
  }

  @override
  void dispose() {
    _controller.dispose();
    super.dispose();
  }

  String _optionsSignature() => widget.options
      .map((item) => '${item.label}\u0000${item.value}')
      .join('\u0001');

  void _rebuildItemsIfNeeded({bool force = false}) {
    final signature = _optionsSignature();
    if (!force && signature == _lastOptionsSignature) return;
    _lastOptionsSignature = signature;
    _items = widget.options
        .map((item) => M3EDropdownItem<String>(
              label: item.label.isEmpty ? item.value : item.label,
              value: item.value,
            ))
        .toList();
  }

  void _emit(String value) {
    if (value == _lastEmitted) return;
    _lastEmitted = value;
    widget.onChanged(value);
  }

  @override
  Widget build(BuildContext context) {
    if (_items.isEmpty) return const Text('(no options)');
    return M3EDropdownMenu<String>(
      controller: _controller,
      items: _items,
      singleSelect: true,
      searchEnabled: widget.searchable,
      maxSelections: 1,
      fieldStyle: M3EDropdownFieldStyle(
        hintText: widget.value.isEmpty
            ? 'Select'
            : _items.firstWhere((i) => i.value == widget.value).label,
        showClearIcon: false,
        borderRadius: BorderRadius.circular(14),
        selectedBorderRadius: 28,
        hoverRadius: 18,
        pressedRadius: 8,
      ),
      dropdownStyle: const M3EDropdownStyle(containerRadius: 20),
      itemStyle: const M3EDropdownItemStyle(
          outerRadius: 18, innerRadius: 6, hoverRadius: 8, pressedRadius: 4),
      chipStyle: const M3EChipStyle(maxDisplayCount: 1),
      searchStyle: M3ESearchStyle(
          hintText: 'Search',
          filled: true,
          borderRadius: BorderRadius.circular(24)),
      openMotion: M3EMotion.expressiveSpatialDefault,
      closeMotion: M3EMotion.expressiveSpatialDefault,
      haptic: M3EHapticFeedback.light,
      onSelectionChanged: (selected) =>
          _emit(selected.isEmpty ? '' : selected.first.value),
    );
  }
}
