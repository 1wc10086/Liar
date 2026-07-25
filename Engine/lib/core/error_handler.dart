import 'dart:async';
import 'dart:ui';
import 'package:flutter/material.dart';
import 'package:flutter/services.dart';

class ErrorHandler {
  static final _errors = <String>[];
  static bool _initialized = false;

  static void init() {
    if (_initialized) return;
    _initialized = true;
    FlutterError.onError = (details) {
      FlutterError.presentError(details);
      _add('FlutterError: ${details.exception}\n${details.stack?.toString() ?? ''}');
    };
    PlatformDispatcher.instance.onError = (error, stack) {
      _add('PlatformError: $error\n${stack?.toString() ?? ''}');
      return true;
    };
  }

  static void _add(String entry) {
    _errors.add('[${DateTime.now()}] $entry');
  }

  static String get log => _errors.join('\n\n');

  static void copyLog() {
    Clipboard.setData(ClipboardData(text: log));
  }

  static void showCrashScreen(BuildContext context, Object error, StackTrace? stack) {
    final cs = Theme.of(context).colorScheme;
    final text = '${error.toString()}\n\n${stack?.toString() ?? ''}';

    showDialog(
      context: context,
      barrierDismissible: false,
      builder: (ctx) => AlertDialog(
        title: Row(children: [
          Icon(Icons.error_outline, color: cs.error),
          const SizedBox(width: 8),
          Text('Crash', style: TextStyle(color: cs.error)),
        ]),
        content: SizedBox(
          width: double.maxFinite,
          child: SingleChildScrollView(
            child: SelectableText(text, style: const TextStyle(fontSize: 12, fontFamily: 'monospace')),
          ),
        ),
        actions: [
          TextButton.icon(
            icon: const Icon(Icons.copy, size: 16),
            label: const Text('Copy'),
            onPressed: () {
              Clipboard.setData(ClipboardData(text: text));
              ScaffoldMessenger.of(context).showSnackBar(
                  const SnackBar(content: Text('Log copied'), duration: Duration(seconds: 1)));
            },
          ),
          FilledButton(
            onPressed: () => Navigator.pop(ctx),
            child: const Text('Dismiss'),
          ),
        ],
      ),
    );
  }
}
