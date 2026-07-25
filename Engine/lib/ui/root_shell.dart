import 'package:flutter/material.dart';
import '../l10n/app_localizations.dart';
import '../core/theme_notifier.dart';
import '../core/app_settings.dart';
import '../core/locale_notifier.dart';
import '../core/app_updater.dart';
import '../features/home/console_page.dart';
import '../features/settings/settings_page.dart';

class RootShell extends StatefulWidget {
  const RootShell({
    super.key,
    required this.themeNotifier,
    required this.settings,
    required this.localeNotifier,
  });

  final ThemeNotifier themeNotifier;
  final AppSettings settings;
  final LocaleNotifier localeNotifier;

  @override
  State<RootShell> createState() => _RootShellState();
}

class _RootShellState extends State<RootShell> {
  int _navIndex = 0;
  bool _checkedUpdate = false;

  @override
  void initState() {
    super.initState();
    if (widget.settings.isConfigured && widget.settings.checkUpdateOnStartup) {
      WidgetsBinding.instance.addPostFrameCallback((_) {
        if (mounted && !_checkedUpdate) {
          _checkedUpdate = true;
          AppUpdater.checkForUpdates(context,
              soPath: widget.settings.soPath,
              scriptDir: widget.settings.scriptDir,
              mirror: widget.settings.repoMirror);
        }
      });
    }
  }

  @override
  Widget build(BuildContext context) {
    final l = AppLocalizations.of(context)!;

    return Scaffold(
      body: _FadeIndexedStack(
        index: _navIndex,
        children: [
          ConsolePage(
            settings: widget.settings,
            localeNotifier: widget.localeNotifier,
          ),
          SettingsPage(
            themeNotifier: widget.themeNotifier,
            settings: widget.settings,
            localeNotifier: widget.localeNotifier,
          ),
        ],
      ),
      bottomNavigationBar: NavigationBar(
        selectedIndex: _navIndex,
        onDestinationSelected: (index) => setState(() => _navIndex = index),
        destinations: [
          NavigationDestination(
            icon: const Icon(Icons.terminal),
            selectedIcon: const Icon(Icons.terminal),
            label: l.navConsole,
          ),
          NavigationDestination(
            icon: const Icon(Icons.settings_outlined),
            selectedIcon: const Icon(Icons.settings),
            label: l.navSettings,
          ),
        ],
      ),
    );
  }
}

class _FadeIndexedStack extends StatefulWidget {
  const _FadeIndexedStack({required this.index, required this.children});
  final int index;
  final List<Widget> children;

  @override
  State<_FadeIndexedStack> createState() => _FadeIndexedStackState();
}

class _FadeIndexedStackState extends State<_FadeIndexedStack>
    with SingleTickerProviderStateMixin {
  late final AnimationController _ctrl;
  late final Animation<double> _fade;

  @override
  void initState() {
    super.initState();
    _ctrl = AnimationController(
        vsync: this, duration: const Duration(milliseconds: 220));
    _fade = CurvedAnimation(parent: _ctrl, curve: Curves.easeIn);
    _ctrl.value = 1.0;
  }

  @override
  void didUpdateWidget(_FadeIndexedStack oldWidget) {
    super.didUpdateWidget(oldWidget);
    if (oldWidget.index != widget.index) {
      _ctrl.forward(from: 0.0);
    }
  }

  @override
  void dispose() {
    _ctrl.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    return FadeTransition(
      opacity: _fade,
      child: IndexedStack(index: widget.index, children: widget.children),
    );
  }
}
