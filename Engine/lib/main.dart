import 'dart:io';
import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:permission_handler/permission_handler.dart';
import 'package:flutter_native_splash/flutter_native_splash.dart';

import 'app.dart';
import 'core/theme_notifier.dart';
import 'core/app_settings.dart';
import 'core/locale_notifier.dart';
import 'core/error_handler.dart';

void main() async {
  WidgetsBinding widgetsBinding = WidgetsFlutterBinding.ensureInitialized();
  FlutterNativeSplash.preserve(widgetsBinding: widgetsBinding);
  ErrorHandler.init();

  await SystemChrome.setEnabledSystemUIMode(SystemUiMode.edgeToEdge);

  if (Platform.isAndroid) {
    if (!await Permission.manageExternalStorage.isGranted) {
      await Permission.manageExternalStorage.request();
    }
    if (!await Permission.storage.isGranted) {
      await Permission.storage.request();
    }
  }

  final settings = await AppSettings.load();
  final localeNotifier = LocaleNotifier(settings);

  FlutterNativeSplash.remove();

  runApp(App(
    themeNotifier: ThemeNotifier(settings),
    settings: settings,
    localeNotifier: localeNotifier,
  ));
}
