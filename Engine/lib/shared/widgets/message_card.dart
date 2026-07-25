import 'package:flutter/material.dart';

enum MessageType { verbosity, information, warning, error, success, input }

extension MessageTypeStyle on MessageType {
  Color color(ColorScheme cs) => switch (this) {
        MessageType.verbosity => cs.outline,
        MessageType.information => cs.primary,
        MessageType.warning => Colors.orange,
        MessageType.error => cs.error,
        MessageType.success => Colors.green,
        MessageType.input => cs.tertiary,
      };

  IconData get icon => switch (this) {
        MessageType.verbosity => Icons.notes,
        MessageType.information => Icons.info_outline,
        MessageType.warning => Icons.warning_amber_outlined,
        MessageType.error => Icons.error_outline,
        MessageType.success => Icons.check_circle_outline,
        MessageType.input => Icons.edit_note,
      };
}

class MessageCard extends StatelessWidget {
  const MessageCard({super.key, required this.type, required this.title, required this.lines});

  final MessageType type;
  final String title;
  final List<String> lines;

  @override
  Widget build(BuildContext context) {
    final cs = Theme.of(context).colorScheme;
    final tt = Theme.of(context).textTheme;
    final accent = type.color(cs);

    return Card(
      margin: EdgeInsets.zero,
      elevation: 0,
      color: Color.alphaBlend(accent.withOpacity(0.06), cs.surfaceContainerLow),
      shape: RoundedRectangleBorder(
        borderRadius: BorderRadius.circular(12),
        side: BorderSide(color: accent.withOpacity(0.2)),
      ),
      child: Padding(
        padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 10),
        child: Row(crossAxisAlignment: CrossAxisAlignment.start, children: [
          Padding(
            padding: const EdgeInsets.only(top: 3),
            child: Icon(Icons.circle, size: 8, color: Color.alphaBlend(accent.withOpacity(0.7), cs.onSurface)),
          ),
          const SizedBox(width: 10),
          Expanded(
            child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
              Row(crossAxisAlignment: CrossAxisAlignment.start, children: [
                Padding(padding: const EdgeInsets.only(top: 2), child: Icon(type.icon, size: 15, color: accent)),
                const SizedBox(width: 6),
                Expanded(child: Text(title, style: tt.titleSmall?.copyWith(
                    color: Color.alphaBlend(accent.withOpacity(0.25), cs.onSurface), height: 1.3))),
              ]),
              for (final line in lines)
                Padding(padding: const EdgeInsets.only(top: 4),
                    child: Text(line, style: tt.bodySmall?.copyWith(color: cs.onSurfaceVariant))),
            ]),
          ),
        ]),
      ),
    );
  }
}
