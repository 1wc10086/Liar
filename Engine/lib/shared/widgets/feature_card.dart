import 'package:flutter/material.dart';

class FeatureCard extends StatelessWidget {
  const FeatureCard({
    super.key,
    required this.icon,
    required this.title,
    required this.subtitle,
    required this.cardColor,
    required this.iconColor,
    required this.onTap,
    this.onSettingsTap,
  });

  final IconData icon;
  final String title;
  final String subtitle;
  final Color cardColor;
  final Color iconColor;
  final VoidCallback onTap;
  final VoidCallback? onSettingsTap;

  @override
  Widget build(BuildContext context) {
    return Card(
      elevation: 0,
      color: cardColor,
      shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(20)),
      clipBehavior: Clip.antiAlias,
      child: Stack(
        fit: StackFit.expand,
        children: [
          InkWell(
            onTap: onTap,
            child: Padding(
              padding: const EdgeInsets.all(18),
              child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
                Icon(icon, size: 32, color: iconColor),
                const Spacer(),
                Text(title, maxLines: 1, overflow: TextOverflow.ellipsis,
                    style: Theme.of(context).textTheme.titleMedium?.copyWith(color: iconColor, fontWeight: FontWeight.bold)),
                const SizedBox(height: 4),
                Text(subtitle, maxLines: 2, overflow: TextOverflow.ellipsis,
                    style: Theme.of(context).textTheme.bodySmall?.copyWith(color: iconColor.withOpacity(0.65))),
              ]),
            ),
          ),
          if (onSettingsTap != null)
            Positioned(
              top: 4, right: 4,
              child: IconButton(
                icon: Icon(Icons.settings, color: iconColor.withOpacity(0.6), size: 20),
                onPressed: onSettingsTap, visualDensity: VisualDensity.compact, splashRadius: 18,
              ),
            ),
        ],
      ),
    );
  }
}
