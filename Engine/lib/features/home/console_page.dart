import 'package:flutter/material.dart';
import '../../l10n/app_localizations.dart';
import '../../core/app_settings.dart';
import '../../core/locale_notifier.dart';
import '../../shared/widgets/feature_card.dart';
import 'shell_page.dart';
import 'lab_page.dart';

class ConsolePage extends StatelessWidget {
  const ConsolePage({super.key, required this.settings, required this.localeNotifier});

  final AppSettings settings;
  final LocaleNotifier localeNotifier;

  @override
  Widget build(BuildContext context) {
    final l = AppLocalizations.of(context)!;
    final cs = Theme.of(context).colorScheme;

    return Scaffold(
      appBar: AppBar(centerTitle: false, title: Text(l.appTitle)),
      body: LayoutBuilder(
        builder: (context, constraints) {
          final compact = constraints.maxWidth < 520;
          return GridView(
            padding: const EdgeInsets.fromLTRB(16, 12, 16, 96),
            gridDelegate: SliverGridDelegateWithMaxCrossAxisExtent(
              maxCrossAxisExtent: compact ? 520 : 280,
              mainAxisSpacing: 12,
              crossAxisSpacing: 12,
              childAspectRatio: compact ? 1.85 : 1.18,
            ),
            children: [
              FeatureCard(
                icon: Icons.terminal,
                title: l.shellCardTitle,
                subtitle: l.shellCardSubtitle,
                cardColor: cs.primaryContainer,
                iconColor: cs.onPrimaryContainer,
                onTap: () => Navigator.push(context,
                    MaterialPageRoute(builder: (_) => ShellPage(settings: settings, localeNotifier: localeNotifier))),
              ),
              FeatureCard(
                icon: Icons.science_outlined,
                title: l.labTitle,
                subtitle: l.labCardSubtitle,
                cardColor: cs.secondaryContainer,
                iconColor: cs.onSecondaryContainer,
                onTap: () => Navigator.push(context,
                    MaterialPageRoute(builder: (_) => LabPage(settings: settings))),
              ),
            ],
          );
        },
      ),
    );
  }
}
