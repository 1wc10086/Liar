import 'package:flutter/material.dart';

import '../../l10n/app_localizations.dart';

class SharedDrawer extends StatefulWidget {
  final String title;
  final List<String> items;
  final String selectedValue;
  final ValueChanged<String> onSelected;
  final VoidCallback? onRefresh;
  final bool loading;
  final String? emptyMessage;
  final String Function(String item)? itemLabel;
  final Widget Function(String item, bool selected)? leadingBuilder;

  const SharedDrawer({
    super.key,
    required this.title,
    required this.items,
    required this.selectedValue,
    required this.onSelected,
    this.onRefresh,
    this.loading = false,
    this.emptyMessage,
    this.itemLabel,
    this.leadingBuilder,
  });

  @override
  State<SharedDrawer> createState() => SharedDrawerState();
}

class SharedDrawerState extends State<SharedDrawer> {
  final _scrollCtrl = ScrollController();
  final _searchCtrl = TextEditingController();
  bool _searching = false;

  List<String> get _filteredItems {
    final kw = _searchCtrl.text.trim().toLowerCase();
    if (kw.isEmpty) return widget.items;
    return widget.items.where((item) {
      final label = (widget.itemLabel?.call(item) ?? item).toLowerCase();
      return label.contains(kw);
    }).toList();
  }

  void scrollToSelected() {
    WidgetsBinding.instance.addPostFrameCallback((_) => _doScroll());
  }

  void _doScroll() {
    final items = _filteredItems;
    final index = items.indexOf(widget.selectedValue);
    if (index < 0 || !_scrollCtrl.hasClients) {
      WidgetsBinding.instance.addPostFrameCallback((_) => _doScroll());
      return;
    }
    final target = index * 56.0;
    final max = _scrollCtrl.position.maxScrollExtent;
    if (max <= 0) {
      WidgetsBinding.instance.addPostFrameCallback((_) => _doScroll());
      return;
    }
    _scrollCtrl.animateTo(target.clamp(0.0, max),
        duration: const Duration(milliseconds: 260), curve: Curves.easeOutCubic);
  }

  @override
  void dispose() {
    _scrollCtrl.dispose();
    _searchCtrl.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    WidgetsBinding.instance.addPostFrameCallback((_) => _doScroll());
    final cs = Theme.of(context).colorScheme;
    final l = AppLocalizations.of(context);
    final items = _filteredItems;

    return Drawer(
      child: SafeArea(
        child: Column(children: [
          SizedBox(
            height: 64,
            child: Padding(
              padding: const EdgeInsets.fromLTRB(24, 4, 8, 4),
              child: Row(children: [
                Expanded(
                  child: AnimatedSwitcher(
                    duration: const Duration(milliseconds: 220),
                    child: _searching
                        ? TextField(
                            key: const ValueKey('s'), controller: _searchCtrl, autofocus: true,
                            decoration: InputDecoration(hintText: l?.search ?? 'Search', border: InputBorder.none),
                            onChanged: (_) => setState(() {}))
                        : Align(
                            key: const ValueKey('t'), alignment: Alignment.centerLeft,
                            child: Text(widget.title, maxLines: 1, overflow: TextOverflow.ellipsis,
                                style: Theme.of(context).textTheme.titleLarge)),
                  ),
                ),
                if (widget.onRefresh != null)
                  IconButton(icon: const Icon(Icons.refresh, size: 22), onPressed: widget.onRefresh),
                IconButton(
                  icon: AnimatedSwitcher(
                    duration: const Duration(milliseconds: 180),
                    child: Icon(_searching ? Icons.close : Icons.search, key: ValueKey(_searching), size: 22),
                  ),
                  onPressed: () { setState(() { _searching = !_searching; if (!_searching) { _searchCtrl.clear(); setState(() {}); } }); },
                ),
              ]),
            ),
          ),
          if (widget.loading) const LinearProgressIndicator(),
          Expanded(
            child: items.isEmpty
                ? (widget.emptyMessage != null
                    ? Center(child: Padding(padding: const EdgeInsets.all(24),
                        child: Text(widget.emptyMessage!, style: TextStyle(color: cs.outline))))
                    : const SizedBox.shrink())
                : ListView.builder(
                    controller: _scrollCtrl,
                    padding: const EdgeInsets.symmetric(vertical: 8),
                    itemCount: items.length,
                    itemBuilder: (context, index) {
                      final item = items[index];
                      final selected = item == widget.selectedValue;
                      return _DrawerItem(
                        title: widget.itemLabel?.call(item) ?? item,
                        selected: selected,
                        leading: widget.leadingBuilder?.call(item, selected),
                        onTap: () => widget.onSelected(item),
                      );
                    },
                  ),
          ),
        ]),
      ),
    );
  }
}

class _DrawerItem extends StatelessWidget {
  final String title;
  final bool selected;
  final Widget? leading;
  final VoidCallback onTap;

  const _DrawerItem({required this.title, required this.selected, this.leading, required this.onTap});

  @override
  Widget build(BuildContext context) {
    final cs = Theme.of(context).colorScheme;
    return Padding(
      padding: const EdgeInsets.symmetric(horizontal: 12),
      child: SizedBox(
        height: 56,
        child: TextButton(
          style: TextButton.styleFrom(
            padding: EdgeInsets.zero,
            backgroundColor: selected ? cs.secondaryContainer : null,
            foregroundColor: selected ? cs.onSecondaryContainer : cs.onSurfaceVariant,
            shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(100)),
          ),
          onPressed: onTap,
          child: Row(children: [
            const SizedBox(width: 16),
            leading ?? Icon(selected ? Icons.extension : Icons.extension_outlined, size: 22),
            const SizedBox(width: 12),
            Expanded(child: Text(title, maxLines: 1, overflow: TextOverflow.ellipsis, softWrap: false,
                style: Theme.of(context).textTheme.labelLarge)),
            const SizedBox(width: 16),
          ]),
        ),
      ),
    );
  }
}
