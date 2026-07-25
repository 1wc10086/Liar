import 'package:flutter/material.dart';
import 'package:dynamic_color/dynamic_color.dart';
import 'package:flutter_localizations/flutter_localizations.dart';
import 'l10n/app_localizations.dart';
import 'core/theme_notifier.dart';
import 'core/app_settings.dart';
import 'core/locale_notifier.dart';
import 'core/error_handler.dart';
import 'ui/root_shell.dart';

class App extends StatelessWidget {
  const App({
    super.key,
    required this.themeNotifier,
    required this.settings,
    required this.localeNotifier,
  });

  final ThemeNotifier themeNotifier;
  final AppSettings settings;
  final LocaleNotifier localeNotifier;

  @override
  Widget build(BuildContext context) {
    return ListenableBuilder(
      listenable: Listenable.merge([themeNotifier, localeNotifier]),
      builder: (context, _) {
        return DynamicColorBuilder(
          builder: (lightDynamic, darkDynamic) {
            final lightTheme = themeNotifier.useDynamicColor &&
                    lightDynamic != null &&
                    darkDynamic != null
                ? ThemeData(useMaterial3: true, colorScheme: lightDynamic)
                : themeNotifier.buildTheme(Brightness.light);

            final darkTheme = themeNotifier.useDynamicColor &&
                    lightDynamic != null &&
                    darkDynamic != null
                ? ThemeData(useMaterial3: true, colorScheme: darkDynamic)
                : themeNotifier.buildTheme(Brightness.dark);

            return MaterialApp(
              debugShowCheckedModeBanner: false,
              title: 'Liar Hub',
              theme: lightTheme,
              darkTheme: darkTheme,
              themeMode: themeNotifier.mode,
              locale: localeNotifier.locale,
              localizationsDelegates: const [
                AppLocalizations.delegate,
                GlobalMaterialLocalizations.delegate,
                GlobalCupertinoLocalizations.delegate,
                GlobalWidgetsLocalizations.delegate,
              ],
              supportedLocales: AppLocalizations.supportedLocales,
              builder: (context, child) {
                ErrorWidget.builder = (details) {
                  return Material(
                    child: Center(
                      child: Padding(
                        padding: const EdgeInsets.all(24),
                        child: Column(mainAxisSize: MainAxisSize.min, children: [
                          Icon(Icons.error_outline, size: 48,
                              color: Theme.of(context).colorScheme.error),
                          const SizedBox(height: 16),
                          SelectableText(details.exceptionAsString(),
                              style: const TextStyle(
                                  fontSize: 12, fontFamily: 'monospace')),
                          const SizedBox(height: 16),
                          FilledButton.icon(
                            icon: const Icon(Icons.copy, size: 16),
                            label: const Text('Copy'),
                            onPressed: () {
                              ErrorHandler.copyLog();
                              ScaffoldMessenger.of(context).showSnackBar(
                                  const SnackBar(
                                      content: Text('Log copied'),
                                      duration: Duration(seconds: 1)));
                            },
                          ),
                        ]),
                      ),
                    ),
                  );
                };

                final scale = themeNotifier.fontScale;
                final mq = MediaQuery.of(context);
                final effective = scale == 1.0
                    ? mq
                    : mq.copyWith(
                        textScaler: mq.textScaler.clamp(
                        minScaleFactor: scale,
                        maxScaleFactor: scale,
                      ));
                return MediaQuery(
                  data: effective,
                  child: child ?? const SizedBox.shrink(),
                );
              },
              home: RootShell(
                themeNotifier: themeNotifier,
                settings: settings,
                localeNotifier: localeNotifier,
              ),
            );
          },
        );
      },
    );
  }
}
