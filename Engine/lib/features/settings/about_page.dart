import 'package:flutter/material.dart';
import 'package:url_launcher/url_launcher.dart';
import '../../l10n/app_localizations.dart';

class AboutPage extends StatelessWidget {
  const AboutPage({super.key});

  Future<void> _launchURL(BuildContext context, String urlString) async {
    final l = AppLocalizations.of(context)!;
    final uri = Uri.parse(urlString);
    try {
      if (await canLaunchUrl(uri)) {
        await launchUrl(uri, mode: LaunchMode.externalApplication);
      } else {
        throw Exception();
      }
    } catch (_) {
      if (context.mounted) {
        ScaffoldMessenger.of(context).showSnackBar(SnackBar(content: Text(l.cannotOpenLink(urlString))));
      }
    }
  }

  @override
  Widget build(BuildContext context) {
    final l = AppLocalizations.of(context)!;
    final cs = Theme.of(context).colorScheme;

    final authors = [(name: 'Liar', role: l.mainDeveloper, link: 'https://github.com/1wc10086', avatar: 'https://avatars.githubusercontent.com/u/163117507?v=4')];

    final credits = [
      (name: 'YingFengTingYu', desc: l.creditDesc1, avatar: 'https://avatars.githubusercontent.com/u/97348874?v=4', link: 'https://github.com/YingFengTingYu'),
      (name: 'twinstar6980', desc: l.creditDesc2, avatar: 'https://avatars.githubusercontent.com/u/37923060?v=4', link: 'https://github.com'),
    ];

    final projects = [
      (title: l.githubSource, subtitle: l.githubSourceDesc, link: 'https://github.com/1wc10086/Liar'),
      (title: l.issueTracker, subtitle: l.issueTrackerDesc, link: 'https://github.com/1wc10086/Liar/issues'),
    ];

    return Scaffold(
      appBar: AppBar(title: Text(l.aboutTitle), centerTitle: false),
      body: ListView(physics: const BouncingScrollPhysics(), children: [
        const SizedBox(height: 24),
        Center(child: Column(children: [
          ClipRRect(
            borderRadius: BorderRadius.circular(20),
            child: Image.asset('assets/logo.png', width: 80, height: 80, fit: BoxFit.cover,
                errorBuilder: (_, __, ___) => Container(width: 80, height: 80, color: cs.primaryContainer,
                    child: Icon(Icons.handyman_outlined, size: 40, color: cs.onPrimaryContainer))),
          ),
          const SizedBox(height: 16),
          Text('Liar Hub', style: Theme.of(context).textTheme.titleLarge?.copyWith(fontWeight: FontWeight.bold)),
          const SizedBox(height: 6),
          Text(l.version, style: Theme.of(context).textTheme.bodySmall?.copyWith(color: cs.outline)),
          const SizedBox(height: 20),
          Padding(
            padding: const EdgeInsets.symmetric(horizontal: 32.0),
            child: Text(l.aboutTagline, textAlign: TextAlign.center,
                style: Theme.of(context).textTheme.bodyMedium?.copyWith(color: cs.onSurfaceVariant, height: 1.5)),
          ),
        ])),
        const SizedBox(height: 16),
        _section(context, l.sectionProject),
        for (final proj in projects)
          ListTile(title: Text(proj.title), subtitle: Text(proj.subtitle),
              trailing: Icon(Icons.open_in_new, size: 16, color: cs.outline), onTap: () => _launchURL(context, proj.link)),
        _section(context, l.sectionDevelopers),
        for (final a in authors)
          ListTile(
            leading: ClipOval(child: Image.network(a.avatar, width: 40, height: 40, fit: BoxFit.cover,
                loadingBuilder: (_, child, progress) => progress == null ? child : CircleAvatar(backgroundColor: cs.surfaceContainerHighest,
                    child: const SizedBox(width: 20, height: 20, child: CircularProgressIndicator(strokeWidth: 2))),
                errorBuilder: (_, __, ___) => CircleAvatar(backgroundColor: cs.secondaryContainer, child: Icon(Icons.code, color: cs.onSecondaryContainer, size: 18)))),
            title: Text(a.name), subtitle: Text(a.role),
            trailing: Icon(Icons.chevron_right, size: 18, color: cs.outlineVariant), onTap: () => _launchURL(context, a.link),
          ),
        _section(context, l.sectionCredits),
        for (final c in credits)
          ListTile(
            leading: ClipOval(child: Image.network(c.avatar, width: 40, height: 40, fit: BoxFit.cover,
                loadingBuilder: (_, child, progress) => progress == null ? child : CircleAvatar(backgroundColor: cs.surfaceContainerHighest,
                    child: const SizedBox(width: 20, height: 20, child: CircularProgressIndicator(strokeWidth: 2))),
                errorBuilder: (_, __, ___) => CircleAvatar(backgroundColor: cs.surfaceContainerHighest, child: Icon(Icons.person_outline, color: cs.outline)))),
            title: Text(c.name), subtitle: Text(c.desc),
            trailing: Icon(Icons.chevron_right, size: 18, color: cs.outlineVariant), onTap: () => _launchURL(context, c.link),
          ),
        const SizedBox(height: 32),
        Center(child: Column(children: [
          Container(padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 4),
              decoration: BoxDecoration(color: cs.errorContainer.withOpacity(0.6), borderRadius: BorderRadius.circular(6)),
              child: Text(l.license, style: Theme.of(context).textTheme.labelSmall?.copyWith(color: cs.onErrorContainer, fontWeight: FontWeight.bold))),
          const SizedBox(height: 12),
          Text(l.copyright, style: Theme.of(context).textTheme.labelSmall?.copyWith(color: cs.outlineVariant)),
          const SizedBox(height: 24),
        ])),
      ]),
    );
  }

  Widget _section(BuildContext context, String label) {
    return Padding(
      padding: const EdgeInsets.fromLTRB(16, 24, 16, 8),
      child: Text(label, style: Theme.of(context).textTheme.labelMedium?.copyWith(
          color: Theme.of(context).colorScheme.primary, fontWeight: FontWeight.bold, letterSpacing: 1.1)),
    );
  }
}
