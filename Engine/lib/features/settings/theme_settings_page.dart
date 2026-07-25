import 'package:flutter/material.dart';

import '../../core/app_settings.dart';
import '../../core/theme_notifier.dart';
import '../../l10n/app_localizations.dart';
import '../../shared/widgets/enhanced_switch.dart';
import 'seed_palette.dart';

class ThemeSettingsPage extends StatefulWidget {
  const ThemeSettingsPage({
    super.key,
    required this.themeNotifier,
    required this.settings,
  });

  final ThemeNotifier themeNotifier;
  final AppSettings settings;

  @override
  State<ThemeSettingsPage> createState() => _ThemeSettingsPageState();
}

class _ThemeSettingsPageState extends State<ThemeSettingsPage> {
  @override
  Widget build(BuildContext context) {
    final l = AppLocalizations.of(context)!;
    final tn = widget.themeNotifier;

    return ListenableBuilder(
      listenable: Listenable.merge([tn, widget.settings]),
      builder: (context, _) {
        final cs = Theme.of(context).colorScheme;
        final modeLabel = switch (tn.mode) {
          ThemeMode.light => l.lightMode,
          ThemeMode.dark => l.darkMode,
          ThemeMode.system => l.systemMode,
        };
        final colorLabel = tn.useDynamicColor ? l.dynamicColor : seedLabel(l, tn.seed);

        return Scaffold(
          appBar: AppBar(title: Text(l.themeTitle), centerTitle: false),
          body: ListView(children: [
            ListTile(
              leading: const Icon(Icons.brightness_6_outlined),
              title: Text(l.themeMode),
              subtitle: Text(modeLabel),
              trailing: const Icon(Icons.chevron_right),
              onTap: () => _showModeSheet(context, tn),
            ),
            ListTile(
              leading: CircleAvatar(
                radius: 14,
                backgroundColor: tn.useDynamicColor ? cs.primary : tn.seed,
              ),
              title: Text(l.themeColor),
              subtitle: Text(colorLabel),
              trailing: const Icon(Icons.chevron_right),
              onTap: () => _showColorSheet(context, tn, widget.settings),
            ),
            EnhancedSwitchListTile(
              leading: const Icon(Icons.palette_outlined),
              title: Text(l.dynamicColor),
              value: tn.useDynamicColor,
              onChanged: (v) => v ? tn.enableDynamicColor() : tn.setSeed(tn.seed),
              style: tn.switchStyle == 'pixel'
                  ? SwitchStyle.pixel
                  : SwitchStyle.material,
            ),
            EnhancedSwitchListTile(
              leading: const Icon(Icons.dark_mode_outlined),
              title: Text(l.amoledMode),
              subtitle: Text(l.amoledModeDesc),
              value: tn.amoledMode,
              onChanged: tn.setAmoledMode,
              style: tn.switchStyle == 'pixel'
                  ? SwitchStyle.pixel
                  : SwitchStyle.material,
            ),
            ListTile(
              leading: const Icon(Icons.text_fields),
              title: Text(l.fontSetting),
              subtitle: Text(tn.fontPath.isEmpty
                  ? l.fontPathDefault
                  : tn.fontPath),
              trailing: const Icon(Icons.chevron_right),
              onTap: _editFontPath,
            ),
            ListTile(
              leading: const Icon(Icons.format_size),
              title: Text(l.textSize),
              subtitle: Text(l.textSizeDesc),
            ),
            Padding(
              padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 4),
              child: _FontScaleSlider(notifier: tn),
            ),
            const SizedBox(height: 16),
          ]),
        );
      },
    );
  }

  void _editFontPath() {
    final l = AppLocalizations.of(context)!;
    final ctrl = TextEditingController(text: widget.themeNotifier.fontPath);
    showDialog(
      context: context,
      builder: (ctx) => AlertDialog(
        title: Text(l.fontDialogTitle),
        content: TextField(
          controller: ctrl,
          autofocus: true,
          decoration: InputDecoration(
            hintText: l.fontPathHint,
            border: const OutlineInputBorder(),
          ),
          onSubmitted: (v) {
            widget.themeNotifier.setFontPath(v.trim());
            Navigator.pop(ctx);
          },
        ),
        actions: [
          TextButton(
            onPressed: () => Navigator.pop(ctx),
            child: Text(l.cancel),
          ),
          FilledButton(
            onPressed: () {
              widget.themeNotifier.setFontPath(ctrl.text.trim());
              Navigator.pop(ctx);
            },
            child: Text(l.save),
          ),
        ],
      ),
    );
  }
}

class _FontScaleSlider extends StatelessWidget {
  const _FontScaleSlider({required this.notifier});
  final ThemeNotifier notifier;

  @override
  Widget build(BuildContext context) {
    return Slider(
      min: 0.7,
      max: 1.6,
      divisions: 18,
      value: notifier.fontScale.clamp(0.7, 1.6),
      label: '${(notifier.fontScale * 100).round()}%',
      onChanged: notifier.setFontScale,
    );
  }
}

void _showModeSheet(BuildContext context, ThemeNotifier tn) {
  final l = AppLocalizations.of(context)!;
  final items = [
    (mode: ThemeMode.light, icon: Icons.light_mode_outlined, label: l.lightMode),
    (mode: ThemeMode.system, icon: Icons.brightness_auto, label: l.systemMode),
    (mode: ThemeMode.dark, icon: Icons.dark_mode_outlined, label: l.darkMode),
  ];

  showModalBottomSheet(
    context: context,
    shape: const RoundedRectangleBorder(
        borderRadius: BorderRadius.vertical(top: Radius.circular(16))),
    builder: (ctx) => SafeArea(
      child: Column(mainAxisSize: MainAxisSize.min, children: [
        const SizedBox(height: 8),
        Container(
            width: 40,
            height: 4,
            decoration: BoxDecoration(
                color: Theme.of(ctx).colorScheme.outlineVariant,
                borderRadius: BorderRadius.circular(2))),
        const SizedBox(height: 8),
        for (final it in items)
          ListTile(
            leading: Icon(it.icon),
            title: Text(it.label),
            selected: tn.mode == it.mode,
            trailing: tn.mode == it.mode
                ? Icon(Icons.check, color: Theme.of(ctx).colorScheme.primary)
                : null,
            onTap: () {
              tn.setMode(it.mode);
              Navigator.pop(ctx);
            },
          ),
        const SizedBox(height: 8),
      ]),
    ),
  );
}

void _showColorSheet(
    BuildContext context, ThemeNotifier tn, AppSettings settings) {
  final l = AppLocalizations.of(context)!;
  showModalBottomSheet(
    context: context,
    shape: const RoundedRectangleBorder(
        borderRadius: BorderRadius.vertical(top: Radius.circular(16))),
    builder: (ctx) => SafeArea(
      child: Padding(
        padding: const EdgeInsets.fromLTRB(20, 12, 20, 20),
        child: Column(
            mainAxisSize: MainAxisSize.min,
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Center(
                child: Container(
                    width: 40,
                    height: 4,
                    decoration: BoxDecoration(
                        color: Theme.of(ctx).colorScheme.outlineVariant,
                        borderRadius: BorderRadius.circular(2))),
              ),
              const SizedBox(height: 16),
              Text(l.themeColor, style: Theme.of(ctx).textTheme.titleMedium),
              const SizedBox(height: 16),
              Wrap(spacing: 12, runSpacing: 12, children: [
                Tooltip(
                  message: l.dynamicColor,
                  child: GestureDetector(
                    onTap: () {
                      tn.enableDynamicColor();
                      Navigator.pop(ctx);
                    },
                    child: AnimatedContainer(
                      duration: const Duration(milliseconds: 200),
                      width: 44,
                      height: 44,
                      decoration: BoxDecoration(
                        color: Theme.of(ctx).colorScheme.secondaryContainer,
                        shape: BoxShape.circle,
                        border: Border.all(
                            color: tn.useDynamicColor
                                ? Theme.of(ctx).colorScheme.onSurface
                                : Colors.transparent,
                            width: 3),
                        boxShadow: tn.useDynamicColor
                            ? [
                                BoxShadow(
                                    color: Theme.of(ctx)
                                        .colorScheme
                                        .primary
                                        .withOpacity(0.5),
                                    blurRadius: 8)
                              ]
                            : null,
                      ),
                      child: Icon(
                        tn.useDynamicColor
                            ? Icons.check
                            : Icons.palette_outlined,
                        color: tn.useDynamicColor
                            ? Theme.of(ctx).colorScheme.onSecondaryContainer
                            : Theme.of(ctx).colorScheme.primary,
                        size: 20,
                      ),
                    ),
                  ),
                ),
                for (final s in kColorSeeds)
                  _SeedDot(
                    seed: s.color,
                    selected:
                        !tn.useDynamicColor && tn.seed.value == s.color.value,
                    label: seedLabel(l, s.color),
                    onTap: () {
                      tn.setSeed(s.color);
                      Navigator.pop(ctx);
                    },
                  ),
              ]),
            ]),
      ),
    ),
  );
}

class _SeedDot extends StatelessWidget {
  const _SeedDot({
    required this.seed,
    required this.selected,
    required this.label,
    required this.onTap,
  });

  final Color seed;
  final bool selected;
  final String label;
  final VoidCallback onTap;

  @override
  Widget build(BuildContext context) {
    return Tooltip(
      message: label,
      child: GestureDetector(
        onTap: onTap,
        child: AnimatedContainer(
          duration: const Duration(milliseconds: 200),
          width: 44,
          height: 44,
          decoration: BoxDecoration(
            color: seed,
            shape: BoxShape.circle,
            border: Border.all(
                color: selected
                    ? Theme.of(context).colorScheme.onSurface
                    : Colors.transparent,
                width: 3),
            boxShadow: selected
                ? [BoxShadow(color: seed.withOpacity(0.5), blurRadius: 8)]
                : null,
          ),
          child: selected
              ? const Icon(Icons.check, color: Colors.white, size: 18)
              : null,
        ),
      ),
    );
  }
}
