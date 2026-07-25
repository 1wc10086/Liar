import 'package:flutter/material.dart';

import '../../l10n/app_localizations.dart';

const kColorSeeds = <_SeedEntry>[
  _SeedEntry(labelKey: 'deepPurple', color: Colors.deepPurple),
  _SeedEntry(labelKey: 'indigo', color: Colors.indigo),
  _SeedEntry(labelKey: 'blue', color: Colors.blue),
  _SeedEntry(labelKey: 'teal', color: Colors.teal),
  _SeedEntry(labelKey: 'green', color: Colors.green),
  _SeedEntry(labelKey: 'orange', color: Colors.orange),
  _SeedEntry(labelKey: 'pink', color: Colors.pink),
  _SeedEntry(labelKey: 'red', color: Colors.red),
];

class _SeedEntry {
  final String labelKey;
  final Color color;
  const _SeedEntry({required this.labelKey, required this.color});
}

String seedLabel(AppLocalizations l, Color seed) {
  for (final s in kColorSeeds) {
    if (s.color.value == seed.value) {
      return switch (s.labelKey) {
        'deepPurple' => l.seedDeepPurple,
        'indigo' => l.seedIndigo,
        'blue' => l.seedBlue,
        'teal' => l.seedTeal,
        'green' => l.seedGreen,
        'orange' => l.seedOrange,
        'pink' => l.seedPink,
        'red' => l.seedRed,
        _ => s.labelKey,
      };
    }
  }
  return l.seedCustom;
}

class LanguageOption {
  final String code;
  final String Function(AppLocalizations) label;
  const LanguageOption({required this.code, required this.label});
}

const kLanguageOptions = <LanguageOption>[
  LanguageOption(code: '', label: _followSystem),
  LanguageOption(code: 'en', label: _en),
  LanguageOption(code: 'zh', label: _zh),
  LanguageOption(code: 'ja', label: _ja),
  LanguageOption(code: 'ko', label: _ko),
  LanguageOption(code: 'vi', label: _vi),
  LanguageOption(code: 'ru', label: _ru),
];

String _followSystem(AppLocalizations l) => l.langFollowSystem;
String _en(AppLocalizations _) => 'English';
String _zh(AppLocalizations _) => '简体中文';
String _ja(AppLocalizations _) => '日本語';
String _ko(AppLocalizations _) => '한국어';
String _vi(AppLocalizations _) => 'Tiếng Việt';
String _ru(AppLocalizations _) => 'Русский';
