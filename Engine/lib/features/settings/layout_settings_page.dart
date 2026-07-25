import 'package:flutter/material.dart';

import '../../core/app_settings.dart';
import '../../core/theme_notifier.dart';
import '../../l10n/app_localizations.dart';
import '../../shared/widgets/enhanced_switch.dart';

class LayoutSettingsPage extends StatelessWidget {
  const LayoutSettingsPage({
    super.key,
    required this.themeNotifier,
    required this.settings,
  });

  final ThemeNotifier themeNotifier;
  final AppSettings settings;

  @override
  Widget build(BuildContext context) {
    final l = AppLocalizations.of(context)!;
    final tn = themeNotifier;
    final current = tn.switchStyle;

    return ListenableBuilder(
      listenable: Listenable.merge([tn, settings]),
      builder: (context, _) {
        return Scaffold(
          appBar: AppBar(title: Text(l.layoutSettings), centerTitle: false),
          body: ListView(children: [
            ListTile(
              leading: const Icon(Icons.toggle_on_outlined),
              title: Text(l.switchStyleTitle),
              subtitle: Text(current == 'pixel'
                  ? l.switchStylePixel
                  : l.switchStyleMaterial),
              trailing: const Icon(Icons.chevron_right),
              onTap: () => _showSwitchStyleSheet(context, tn),
            ),
          ]),
        );
      },
    );
  }

  void _showSwitchStyleSheet(BuildContext context, ThemeNotifier tn) {
    final l = AppLocalizations.of(context)!;
    final options = [
      (style: 'material', icon: Icons.style_outlined, label: l.switchStyleMaterial),
      (style: 'pixel', icon: Icons.phone_android_outlined, label: l.switchStylePixel),
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
          for (final it in options)
            ListTile(
              leading: Icon(it.icon),
              title: Text(it.label),
              selected: tn.switchStyle == it.style,
              trailing: tn.switchStyle == it.style
                  ? Icon(Icons.check, color: Theme.of(ctx).colorScheme.primary)
                  : null,
              onTap: () {
                settings.setSwitchStyle(it.style);
                Navigator.pop(ctx);
              },
            ),
          const SizedBox(height: 8),
        ]),
      ),
    );
  }
}
