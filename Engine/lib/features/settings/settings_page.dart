import 'package:flutter/material.dart';
import 'package:permission_handler/permission_handler.dart';

import '../../core/app_settings.dart';
import '../../core/app_updater.dart';
import '../../core/locale_notifier.dart';
import '../../core/script_config.dart';
import '../../core/theme_notifier.dart';
import '../../l10n/app_localizations.dart';
import '../../shared/widgets/enhanced_switch.dart';
import 'about_page.dart';
import 'layout_settings_page.dart';
import 'seed_palette.dart';
import 'theme_settings_page.dart';

class SettingsPage extends StatefulWidget {
  const SettingsPage({
    super.key,
    required this.themeNotifier,
    required this.settings,
    required this.localeNotifier,
  });

  final ThemeNotifier themeNotifier;
  final AppSettings settings;
  final LocaleNotifier localeNotifier;

  @override
  State<SettingsPage> createState() => _SettingsPageState();
}

class _SettingsPageState extends State<SettingsPage> {
  List<String> _executors = [];
  String _currentExecutor = '';
  bool _loadingExecutors = false;

  @override
  void initState() {
    super.initState();
    if (widget.settings.isConfigured) _loadExecutors();
  }

  Future<void> _loadExecutors() async {
    setState(() => _loadingExecutors = true);
    _executors = await ScriptConfig.listExecutors(widget.settings.scriptDir);
    _currentExecutor =
        await ScriptConfig.readCurrentExecutor(widget.settings.scriptDir);
    if (mounted) setState(() => _loadingExecutors = false);
  }

  void _editPath(
      String label, String current, Future<void> Function(String) onSave) {
    final l = AppLocalizations.of(context)!;
    final ctrl = TextEditingController(text: current);
    showDialog(
      context: context,
      builder: (ctx) => AlertDialog(
        title: Text(l.editDialogTitle(label)),
        content: TextField(
          controller: ctrl,
          autofocus: true,
          decoration: InputDecoration(
              hintText: l.inputPathHint, border: const OutlineInputBorder()),
          onSubmitted: (v) {
            onSave(v);
            Navigator.pop(ctx);
          },
        ),
        actions: [
          TextButton(
              onPressed: () => Navigator.pop(ctx), child: Text(l.cancel)),
          FilledButton(
            onPressed: () {
              onSave(ctrl.text);
              Navigator.pop(ctx);
            },
            child: Text(l.save),
          ),
        ],
      ),
    );
  }

  void _showLanguageDialog() {
    final l = AppLocalizations.of(context)!;
    final currentCode = widget.settings.locale;
    showDialog(
      context: context,
      builder: (ctx) => AlertDialog(
        title: Text(l.languageDialogTitle),
        content: SizedBox(
          width: double.minPositive,
          child: ListView(
            shrinkWrap: true,
            children: [
              for (final opt in kLanguageOptions)
                RadioListTile<String>(
                  value: opt.code,
                  groupValue: currentCode,
                  title: Text(opt.label(l)),
                  onChanged: (v) async {
                    if (v == null) return;
                    Navigator.pop(ctx);
                    if (v.isEmpty) {
                      await widget.localeNotifier
                          .setFollowSystem(widget.settings.scriptDir);
                    } else {
                      await widget.localeNotifier.setLocale(
                          Locale(v), widget.settings.scriptDir);
                    }
                    setState(() {});
                  },
                ),
            ],
          ),
        ),
        actions: [
          TextButton(
              onPressed: () => Navigator.pop(ctx), child: Text(l.cancel)),
        ],
      ),
    );
  }

  void _showExecutorDialog() {
    final l = AppLocalizations.of(context)!;
    showDialog(
      context: context,
      builder: (ctx) {
        if (_loadingExecutors) {
          return AlertDialog(
            title: Text(l.executorDialogTitle),
            content: const SizedBox(
                height: 80, child: Center(child: CircularProgressIndicator())),
          );
        }
        if (_executors.isEmpty) {
          return AlertDialog(
            title: Text(l.executorDialogTitle),
            content: Text(l.noExecutors),
            actions: [
              TextButton(
                  onPressed: () => Navigator.pop(ctx), child: Text(l.cancel)),
            ],
          );
        }
        return AlertDialog(
          title: Text(l.executorDialogTitle),
          content: SizedBox(
            width: double.minPositive,
            child: ListView(
              shrinkWrap: true,
              children: [
                for (final exec in _executors)
                  RadioListTile<String>(
                    value: exec,
                    groupValue: _currentExecutor,
                    title: Text(exec),
                    onChanged: (v) async {
                      if (v == null) return;
                      Navigator.pop(ctx);
                      await ScriptConfig.writeExecutor(
                          widget.settings.scriptDir, v);
                      setState(() => _currentExecutor = v);
                    },
                  ),
              ],
            ),
          ),
          actions: [
            TextButton(
                onPressed: () => Navigator.pop(ctx), child: Text(l.cancel)),
          ],
        );
      },
    );
  }

  void _toggleRepoMirror(bool enabled) {
    if (enabled) {
      widget.settings.setRepoMirror(AppUpdater.repoMirrors.first);
    } else {
      widget.settings.setRepoMirror('');
    }
  }

  String get _currentLanguageLabel {
    final l = AppLocalizations.of(context)!;
    final code = widget.settings.locale;
    for (final opt in kLanguageOptions) {
      if (opt.code == code) return opt.label(l);
    }
    return l.langFollowSystem;
  }

  Future<void> _openStorageSettings() async {
    await openAppSettings();
  }

  @override
  Widget build(BuildContext context) {
    final l = AppLocalizations.of(context)!;
    final cs = Theme.of(context).colorScheme;

    return ListenableBuilder(
      listenable: Listenable.merge([widget.themeNotifier, widget.settings]),
      builder: (context, _) {
        return Scaffold(
          appBar: AppBar(title: Text(l.navSettings), centerTitle: false),
          body: ListView(children: [
            _section(l.sectionShell),
            ListTile(
              leading: const Icon(Icons.memory),
              title: Text(l.kernelPath),
              subtitle: Text(
                widget.settings.soPath.isEmpty
                    ? l.notConfigured
                    : widget.settings.soPath,
                maxLines: 1,
                overflow: TextOverflow.ellipsis,
                style: TextStyle(
                    color: widget.settings.soPath.isEmpty ? cs.error : null),
              ),
              trailing: widget.settings.soPath.isNotEmpty
                  ? IconButton(
                      icon: const Icon(Icons.close, size: 18),
                      onPressed: () => widget.settings.setSoPath(''))
                  : const Icon(Icons.chevron_right),
              onTap: () => _editPath(
                  l.kernelPath, widget.settings.soPath, widget.settings.setSoPath),
            ),
            ListTile(
              leading: const Icon(Icons.folder_outlined),
              title: Text(l.scriptDir),
              subtitle: Text(
                widget.settings.scriptDir.isEmpty
                    ? l.notConfigured
                    : widget.settings.scriptDir,
                maxLines: 1,
                overflow: TextOverflow.ellipsis,
                style: TextStyle(
                    color:
                        widget.settings.scriptDir.isEmpty ? cs.error : null),
              ),
              trailing: widget.settings.scriptDir.isNotEmpty
                  ? IconButton(
                      icon: const Icon(Icons.close, size: 18),
                      onPressed: () {
                        widget.settings.setScriptDir('');
                        setState(() {
                          _executors = [];
                          _currentExecutor = '';
                        });
                      })
                  : const Icon(Icons.chevron_right),
              onTap: () => _editPath(l.scriptDir, widget.settings.scriptDir,
                  (v) async {
                await widget.settings.setScriptDir(v);
                if (v.isNotEmpty) await _loadExecutors();
              }),
            ),
            ListTile(
              leading: const Icon(Icons.translate),
              title: Text(l.language),
              subtitle: Text(_currentLanguageLabel),
              trailing: const Icon(Icons.chevron_right),
              onTap: _showLanguageDialog,
            ),
            ListTile(
              leading: const Icon(Icons.developer_board_outlined),
              title: Text(l.executorImpl),
              subtitle: Text(_loadingExecutors
                  ? l.loadingExecutors
                  : _currentExecutor.isEmpty
                      ? l.notConfigured
                      : _currentExecutor),
              trailing: const Icon(Icons.chevron_right),
              onTap:
                  widget.settings.isConfigured ? _showExecutorDialog : null,
            ),
            const Divider(height: 1),
            _section(l.sectionAppearance),
            ListTile(
              leading: const Icon(Icons.color_lens_outlined),
              title: Text(l.themeTitle),
              trailing: const Icon(Icons.chevron_right),
              onTap: () => Navigator.push(
                  context,
                  MaterialPageRoute(
                      builder: (_) => ThemeSettingsPage(
                          themeNotifier: widget.themeNotifier,
                          settings: widget.settings))),
            ),
            ListTile(
              leading: const Icon(Icons.view_quilt_outlined),
              title: Text(l.layoutSettings),
              trailing: const Icon(Icons.chevron_right),
              onTap: () => Navigator.push(
                  context,
                  MaterialPageRoute(
                      builder: (_) => LayoutSettingsPage(
                          themeNotifier: widget.themeNotifier,
                          settings: widget.settings))),
            ),
            const Divider(height: 1),
            _section(l.sectionPermissions),
            ListTile(
              leading: const Icon(Icons.storage),
              title: Text(l.storagePermission),
              subtitle: Text(l.storagePermissionDesc),
              trailing: const Icon(Icons.chevron_right),
              onTap: _openStorageSettings,
            ),
            const Divider(height: 1),
            _section(l.sectionUpdate),
            EnhancedSwitchListTile(
              leading: const Icon(Icons.system_update_outlined),
              title: Text(l.checkUpdateOnStartup),
              subtitle: Text(l.checkUpdateOnStartupDesc),
              value: widget.settings.checkUpdateOnStartup,
              onChanged: widget.settings.setCheckUpdateOnStartup,
              style: widget.themeNotifier.switchStyle == 'pixel'
                  ? SwitchStyle.pixel
                  : SwitchStyle.material,
            ),
            EnhancedSwitchListTile(
              leading: const Icon(Icons.cloud_sync_outlined),
              title: Text(l.repoMirror),
              subtitle: Text(l.repoMirrorDesc),
              value: widget.settings.repoMirror.isNotEmpty,
              onChanged: _toggleRepoMirror,
              style: widget.themeNotifier.switchStyle == 'pixel'
                  ? SwitchStyle.pixel
                  : SwitchStyle.material,
            ),
            const Divider(height: 1),
            _section(l.sectionAbout),
            ListTile(
              leading: const Icon(Icons.info_outline),
              title: Text(l.aboutApp),
              subtitle: Text(l.version),
              trailing: const Icon(Icons.chevron_right),
              onTap: () => Navigator.push(context,
                  MaterialPageRoute(builder: (_) => const AboutPage())),
            ),
          ]),
        );
      },
    );
  }

  Widget _section(String label) {
    return Padding(
      padding: const EdgeInsets.fromLTRB(16, 20, 16, 4),
      child: Text(label,
          style: Theme.of(context).textTheme.labelMedium?.copyWith(
              color: Theme.of(context).colorScheme.primary,
              letterSpacing: 1.2)),
    );
  }
}
