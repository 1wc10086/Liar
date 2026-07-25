import 'dart:convert';
import 'dart:io';

import 'package:archive/archive_io.dart';
import 'package:flutter/material.dart';
import 'package:http/http.dart' as http;
import 'package:open_file/open_file.dart';
import 'package:path_provider/path_provider.dart';
import 'package:path/path.dart' as p;
import 'package:url_launcher/url_launcher.dart';
import 'jsonc.dart';
import '../l10n/app_localizations.dart';

class AppUpdater {
  AppUpdater._();

  static const String currentVersion = '1.0.7';
  static const String _apiUrl =
      'https://api.github.com/repos/1wc10086/Liar/releases/latest';
  static const List<String> repoMirrors = [
    'https://cdn.gh-proxy.org/',
    'https://v6.gh-proxy.org/',
    'https://v4.gh-proxy.org/',
    'https://gh-proxy.org/',
  ];

  static String _soPath = '';
  static String _scriptDir = '';
  static Directory? _tempDir;

  static Future<void> checkForUpdates(
    BuildContext context, {
    required String soPath,
    required String scriptDir,
    String mirror = '',
  }) async {
    _soPath = soPath;
    _scriptDir = scriptDir;

    try {
      final response = await http
          .get(Uri.parse(_apiUrl),
              headers: {'Accept': 'application/vnd.github+json'})
          .timeout(const Duration(seconds: 12));
      if (response.statusCode != 200) return;

      final data = jsonDecode(response.body) as Map<String, dynamic>;
      final tagName = (data['tag_name'] as String? ?? '').trim();
      final htmlUrl = data['html_url'] as String? ?? '';
      final body = data['body'] as String? ?? '';
      final createdAt = data['created_at'] as String? ?? '';
      final prerelease = data['prerelease'] as bool? ?? false;

      final assets = data['assets'] as List<dynamic>? ?? [];
      String downloadUrl = '';
      for (final asset in assets) {
        final name = (asset['name'] as String? ?? '').toLowerCase();
        if (name.endsWith('.zip')) {
          downloadUrl = asset['browser_download_url'] as String? ?? '';
          break;
        }
      }

      final remoteClean = tagName.replaceFirst(RegExp(r'^v'), '');
      if (!_isNewer(remoteClean, currentVersion)) return;
      if (downloadUrl.isEmpty) return;
      if (!context.mounted) return;

      final effectiveUrl =
          mirror.isNotEmpty ? '$mirror$downloadUrl' : downloadUrl;

      _showUpdateDialog(
        context,
        tagName: tagName,
        htmlUrl: htmlUrl,
        body: body,
        createdAt: createdAt,
        downloadUrl: effectiveUrl,
        prerelease: prerelease,
      );
    } catch (_) {}
  }

  static bool _isNewer(String remote, String current) {
    List<int> parse(String v) {
      final numeric = v.split('-').first;
      return numeric.split('.').map((e) => int.tryParse(e) ?? 0).toList();
    }

    final r = parse(remote);
    final c = parse(current);
    for (int i = 0; i < 3; i++) {
      final rv = i < r.length ? r[i] : 0;
      final cv = i < c.length ? c[i] : 0;
      if (rv > cv) return true;
      if (rv < cv) return false;
    }
    return false;
  }

  static String _formatDate(String iso) {
    try {
      final dt = DateTime.parse(iso);
      return '${dt.year}-${dt.month.toString().padLeft(2, '0')}-${dt.day.toString().padLeft(2, '0')}';
    } catch (_) {
      return iso.length >= 10 ? iso.substring(0, 10) : iso;
    }
  }

  static void _showUpdateDialog(
    BuildContext context, {
    required String tagName,
    required String htmlUrl,
    required String body,
    required String createdAt,
    required String downloadUrl,
    required bool prerelease,
  }) {
    final l = AppLocalizations.of(context)!;
    showDialog<void>(
      context: context,
      barrierDismissible: false,
      builder: (ctx) => AlertDialog(
        title: Text(
            '${l.newVersionFound(tagName)}${prerelease ? l.prereleaseSuffix : ''}'),
        content: SingleChildScrollView(
          child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              mainAxisSize: MainAxisSize.min,
              children: [
                Text(body, style: Theme.of(ctx).textTheme.bodyMedium),
                const SizedBox(height: 14),
                Text(l.publishDate(_formatDate(createdAt)),
                    style: Theme.of(ctx)
                        .textTheme
                        .bodySmall
                        ?.copyWith(color: Theme.of(ctx).colorScheme.outline)),
              ]),
        ),
        actions: [
          TextButton(
              onPressed: () => Navigator.of(ctx).pop(),
              child: Text(l.remindLater)),
          TextButton(
            onPressed: () async {
              Navigator.of(ctx).pop();
              final uri = Uri.parse(htmlUrl);
              if (await canLaunchUrl(uri)) {
                await launchUrl(uri, mode: LaunchMode.externalApplication);
              }
            },
            child: Text(l.viewDetails),
          ),
          FilledButton(
            onPressed: () {
              Navigator.of(ctx).pop();
              _showDownloadDialog(context, downloadUrl);
            },
            child: Text(l.updateNow),
          ),
        ],
      ),
    );
  }

  static void _showDownloadDialog(BuildContext context, String downloadUrl) {
    final l = AppLocalizations.of(context)!;
    final progress = ValueNotifier<double>(0.0);
    final cancelled = ValueNotifier<bool>(false);
    final statusText = ValueNotifier<String>(l.connecting);

    showDialog<void>(
      context: context,
      barrierDismissible: false,
      builder: (ctx) => PopScope(
        canPop: false,
        child: AlertDialog(
          title: Text(l.downloadingUpdate),
          content: ValueListenableBuilder<double>(
            valueListenable: progress,
            builder: (_, value, __) => Column(
                mainAxisSize: MainAxisSize.min,
                children: [
                  LinearProgressIndicator(value: value > 0 ? value : null),
                  const SizedBox(height: 10),
                  ValueListenableBuilder<String>(
                      valueListenable: statusText,
                      builder: (_, s, __) =>
                          Text(s, style: Theme.of(ctx).textTheme.bodySmall)),
                ]),
          ),
          actions: [
            TextButton(
                onPressed: () {
                  cancelled.value = true;
                  Navigator.of(ctx).pop();
                },
                child: Text(l.cancel)),
          ],
        ),
      ),
    );

    _downloadAndUpdate(
        context: context,
        url: downloadUrl,
        progress: progress,
        cancelled: cancelled,
        statusText: statusText);
  }

  static Future<void> _downloadAndUpdate({
    required BuildContext context,
    required String url,
    required ValueNotifier<double> progress,
    required ValueNotifier<bool> cancelled,
    required ValueNotifier<String> statusText,
  }) async {
    final l = AppLocalizations.of(context)!;
    File? zipFile;
    try {
      _tempDir = Directory.systemTemp.createTempSync('liar_update_');
      zipFile = File('${_tempDir!.path}/update.zip');

      final request = http.Request('GET', Uri.parse(url));
      final streamedResponse = await http.Client().send(request);
      final total = streamedResponse.contentLength ?? 0;
      int received = 0;

      final sink = zipFile.openWrite();
      await for (final chunk in streamedResponse.stream) {
        if (cancelled.value) {
          await sink.close();
          _cleanup();
          return;
        }
        sink.add(chunk);
        received += chunk.length;
        if (total > 0) progress.value = received / total;
      }
      await sink.close();
      if (cancelled.value) {
        _cleanup();
        return;
      }

      statusText.value = l.extracting;
      if (!context.mounted) return;

      final extractDir = Directory('${_tempDir!.path}/extracted');
      await extractDir.create(recursive: true);

      final bytes = await zipFile.readAsBytes();
      final archive = ZipDecoder().decodeBytes(bytes);
      for (final file in archive) {
        if (file.isFile) {
          final outFile = File('${extractDir.path}/${file.name}');
          await outFile.create(recursive: true);
          await outFile.writeAsBytes(file.content as List<int>);
        }
      }

      statusText.value = l.installingFiles;
      await _updateSo(extractDir);
      await _updateLibrary(extractDir);
      await _updateScript(extractDir);

      String? apkPath;
      await for (final entity in extractDir.list(recursive: false)) {
        if (entity is File && entity.path.toLowerCase().endsWith('.apk')) {
          apkPath = entity.path;
          break;
        }
      }

      if (!context.mounted) return;
      if (Navigator.of(context).canPop()) Navigator.of(context).pop();

      if (apkPath != null) {
        _showDoneDialog(context, apkPath);
      } else {
        _cleanup();
      }
    } catch (e) {
      _cleanup();
      if (!context.mounted) return;
      if (Navigator.of(context).canPop()) Navigator.of(context).pop();
      ScaffoldMessenger.of(context)
          .showSnackBar(SnackBar(content: Text(l.updateFailed('$e'))));
    }
  }

  static Future<void> _updateSo(Directory extractDir) async {
    if (_soPath.isEmpty) return;
    try {
      final src = File('${extractDir.path}/libkernel.so');
      if (await src.exists()) {
        final dest = File(_soPath);
        if (await dest.exists()) await dest.delete();
        await src.copy(dest.path);
      }
    } catch (_) {}
  }

  static Future<void> _updateLibrary(Directory extractDir) async {
    try {
      if (_scriptDir.isEmpty) return;

      final scriptDir = Directory(_scriptDir);
      if (!await scriptDir.exists()) return;

      final mainFile = File(p.join(scriptDir.path, 'main.json'));
      if (!await mainFile.exists()) return;

      final mainDecoded = decodeJsonc(await mainFile.readAsString());
      if (mainDecoded is! Map) return;
      final main = Map<String, dynamic>.from(mainDecoded);
      final pathsValue = main['paths'];
      if (pathsValue != null && pathsValue is! Map) return;
      final paths = pathsValue is Map
          ? Map<String, dynamic>.from(pathsValue)
          : <String, dynamic>{};
      final settingsPath = (paths['settings'] as String?) ?? '';
      if (settingsPath.isEmpty) return;

      final settingsFile = File(p.join(scriptDir.path, settingsPath));
      if (!await settingsFile.exists()) return;

      final settingsDecoded = decodeJsonc(await settingsFile.readAsString());
      if (settingsDecoded is! Map) return;
      final settings = Map<String, dynamic>.from(settingsDecoded);
      final libraryPath = (settings['library'] as String?) ?? '';
      if (libraryPath.isEmpty) return;

      final srcDir = Directory(p.join(scriptDir.path, libraryPath));
      if (!await srcDir.exists()) return;

      final destRoot = Directory(p.join(extractDir.path, 'app_dynamic_libs'));
      await destRoot.create(recursive: true);

      await for (final entity in srcDir.list(recursive: true)) {
        if (entity is! File || !entity.path.toLowerCase().endsWith('.so')) {
          continue;
        }
        final relative = p.relative(entity.path, from: srcDir.path);
        final destPath = p.join(destRoot.path, relative);
        await File(destPath).parent.create(recursive: true);
        await entity.copy(destPath);
      }
    } catch (_) {}
  }

  static Future<void> _updateScript(Directory extractDir) async {
    if (_scriptDir.isEmpty) return;
    try {
      final srcDir = Directory('${extractDir.path}/script');
      if (!await srcDir.exists()) return;
      final destDir = Directory(_scriptDir);
      if (await destDir.exists()) await destDir.delete(recursive: true);
      await destDir.create(recursive: true);
      await for (final entity in srcDir.list(recursive: true)) {
        final relative = entity.path.substring(srcDir.path.length + 1);
        final destPath = '${destDir.path}/$relative';
        if (entity is File) {
          await File(destPath).create(recursive: true);
          await entity.copy(destPath);
        } else if (entity is Directory) {
          await Directory(destPath).create(recursive: true);
        }
      }
    } catch (_) {}
  }

  static void _showDoneDialog(BuildContext context, String apkTempPath) {
    final l = AppLocalizations.of(context)!;
    showDialog<void>(
      context: context,
      barrierDismissible: false,
      builder: (ctx) => AlertDialog(
        title: Text(l.updateComplete),
        content: Text(l.updateCompleteMessage),
        actions: [
          FilledButton(
            onPressed: () async {
              Navigator.of(ctx).pop();
              await _installAndCleanup(apkTempPath, context);
            },
            child: Text(l.installNow),
          ),
        ],
      ),
    );
  }

  static Future<void> _installAndCleanup(
      String apkTempPath, BuildContext context) async {
    final l = AppLocalizations.of(context)!;
    try {
      final dir = await getExternalStorageDirectory() ??
          await getApplicationDocumentsDirectory();
      final pubApk = File('${dir.path}/liar_update.apk');
      await File(apkTempPath).copy(pubApk.path);

      final result = await OpenFile.open(pubApk.path,
          type: 'application/vnd.android.package-archive');

      if (result.type != ResultType.done && context.mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
            SnackBar(content: Text(l.cannotOpenApk(result.message))));
      }
    } catch (_) {
    } finally {
      _cleanup();
    }
  }

  static void _cleanup() {
    try {
      if (_tempDir != null && _tempDir!.existsSync()) {
        _tempDir!.deleteSync(recursive: true);
      }
    } catch (_) {}
    _tempDir = null;
  }
}
