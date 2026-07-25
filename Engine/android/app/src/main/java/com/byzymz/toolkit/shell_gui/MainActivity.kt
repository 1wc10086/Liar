package com.byzymz.toolkit.shell_gui

import io.flutter.embedding.android.FlutterActivity
import io.flutter.embedding.engine.FlutterEngine
import io.flutter.plugin.common.MethodChannel
import kotlinx.coroutines.CoroutineScope
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.launch
import kotlinx.coroutines.withContext
import java.io.File
import android.content.Intent
import android.os.Build
import androidx.core.content.FileProvider
import com.liar.byzymztools.util.native.ShellNative
import org.json.JSONObject

class MainActivity : FlutterActivity() {
    private val CHANNEL = "com.liar.byzymztools/shell"
    private val scope = CoroutineScope(Dispatchers.Main)
    private var isSoLoaded = false
    private var loadedSoPath = ""
    private var loadedLibraryDir = ""

    private fun stripJsonComments(source: String): String {
        val output = StringBuilder(source.length)
        var inString = false
        var escaped = false
        var index = 0
        while (index < source.length) {
            val char = source[index]
            if (inString) {
                output.append(char)
                when {
                    escaped -> escaped = false
                    char == '\\' -> escaped = true
                    char == '"' -> inString = false
                }
            } else if (char == '"') {
                inString = true
                output.append(char)
            } else if (char == '/' && index + 1 < source.length && source[index + 1] == '/') {
                output.append(' ')
                index += 2
                while (index < source.length && source[index] != '\n' && source[index] != '\r') index++
                if (index < source.length) output.append(source[index])
            } else if (char == '/' && index + 1 < source.length && source[index + 1] == '*') {
                output.append(' ')
                index += 2
                while (index + 1 < source.length && !(source[index] == '*' && source[index + 1] == '/')) {
                    if (source[index] == '\n' || source[index] == '\r') output.append(source[index])
                    index++
                }
                if (index + 1 >= source.length) throw IllegalArgumentException("Unterminated JSONC comment")
                index++
            } else {
                output.append(char)
            }
            index++
        }
        return output.toString()
    }

    private fun resolveLibraryDir(scriptDir: String, libraryDir: String): File? {
        if (libraryDir.isNotBlank()) {
            val direct = File(libraryDir)
            if (direct.exists()) return direct
        }

        val mainFile = File(scriptDir, "main.json")
        if (!mainFile.exists()) return null

        return try {
            val main = JSONObject(stripJsonComments(mainFile.readText()))
            val paths = main.optJSONObject("paths") ?: return null
            val settingsPath = paths.optString("settings", "")
            if (settingsPath.isBlank()) return null

            val settingsFile = File(scriptDir, settingsPath)
            if (!settingsFile.exists()) return null

            val settings = JSONObject(stripJsonComments(settingsFile.readText()))
            val library = settings.optString("library", "")
            if (library.isBlank()) null else File(scriptDir, library)
        } catch (_: Exception) {
            null
        }
    }

    private fun copySharedLibraries(sourceDir: File, targetDir: File): List<File> {
        if (!sourceDir.exists()) return emptyList()

        val sources = sourceDir.walkTopDown()
            .filter { it.isFile && it.extension.equals("so", ignoreCase = true) }
            .sortedBy { it.relativeTo(sourceDir).path.replace(File.separatorChar, '/') }
            .toList()

        targetDir.walkTopDown()
            .filter { it.isFile && it.extension.equals("so", ignoreCase = true) }
            .forEach { it.delete() }

        val copied = ArrayList<File>(sources.size)
        for (src in sources) {
            val dest = File(targetDir, src.relativeTo(sourceDir).path)
            dest.parentFile?.mkdirs()
            src.copyTo(dest, overwrite = true)
            copied += dest
        }
        return copied
    }

    override fun configureFlutterEngine(flutterEngine: FlutterEngine) {
        super.configureFlutterEngine(flutterEngine)

        MethodChannel(flutterEngine.dartExecutor.binaryMessenger, CHANNEL)
            .setMethodCallHandler { call, result ->
                when (call.method) {
                    "initKernel" -> {
                        val soPath = call.argument<String>("soPath") ?: ""
                        val scriptDir = call.argument<String>("scriptDir") ?: ""
                        val libraryDir = call.argument<String>("libraryDir") ?: ""
                        val forceReload = call.argument<Boolean>("forceReload") ?: false

                        scope.launch {
                            try {
                                val jsonRes = withContext(Dispatchers.IO) {
                                    val dir = getDir("dynamic_libs", android.content.Context.MODE_PRIVATE)
                                    val resolvedLibraryDir = resolveLibraryDir(scriptDir, libraryDir)

                                    if (resolvedLibraryDir != null && resolvedLibraryDir.exists() && loadedLibraryDir != resolvedLibraryDir.absolutePath) {
                                        val loaded = copySharedLibraries(resolvedLibraryDir, dir)
                                        for (lib in loaded) {
                                            System.load(lib.absolutePath)
                                        }
                                        loadedLibraryDir = resolvedLibraryDir.absolutePath
                                    }

                                    if (
                                        (!isSoLoaded || loadedSoPath != soPath) &&
                                        soPath.isNotEmpty() &&
                                        File(soPath).exists()
                                    ) {
                                        val src = File(soPath)
                                        val dest = File(dir, src.name)
                                        src.copyTo(dest, overwrite = true)
                                        System.load(dest.absolutePath)
                                        isSoLoaded = true
                                        loadedSoPath = soPath
                                    }

                                    ShellNative.initKernel(scriptDir)
                                }

                                result.success(jsonRes)
                            } catch (_: Exception) {
                                result.success("{\"isSuccess\":false}")
                            }
                        }
                    }

                    "matchFunctions" -> {
                        val path = call.argument<String>("path") ?: ""

                        scope.launch {
                            try {
                                val payload = withContext(Dispatchers.IO) {
                                    ShellNative.matchFunctions(path)
                                }

                                result.success(
                                    mapOf(
                                        "isSuccess" to true,
                                        "payload" to payload,
                                        "isFolder" to File(path).isDirectory
                                    )
                                )
                            } catch (_: Exception) {
                                result.success(mapOf("isSuccess" to false))
                            }
                        }
                    }

                    "runFunction" -> {
                        val funcName = call.argument<String>("funcName") ?: ""
                        val params = call.argument<List<String>>("params") ?: emptyList()

                        scope.launch {
                            try {
                                val execMap = withContext(Dispatchers.IO) {
                                    val raw = ShellNative.executeFunction(funcName, params.toTypedArray())
                                    val res = ShellNative.parseExecResult(raw)

                                    mapOf(
                                        "isSuccess" to res.isSuccess,
                                        "code" to res.code,
                                        "time" to res.time,
                                        "output" to res.output
                                    )
                                }

                                result.success(execMap)
                            } catch (e: Exception) {
                                result.success(
                                    mapOf(
                                        "isSuccess" to false,
                                        "code" to 3,
                                        "output" to e.message
                                    )
                                )
                            }
                        }
                    }

                    "runBatchFunction" -> {
                        val funcName = call.argument<String>("funcName") ?: ""
                        val inputFolder = call.argument<String>("inputFolder") ?: ""
                        val outFolder = call.argument<String>("outputFolder") ?: ""
                        val extraParams = call.argument<List<String>>("extraParams") ?: emptyList()

                        scope.launch {
                            try {
                                val execMap = withContext(Dispatchers.IO) {
                                    val raw = ShellNative.executeBatchFunction(
                                        funcName,
                                        inputFolder,
                                        outFolder,
                                        extraParams.toTypedArray()
                                    )
                                    val res = ShellNative.parseBatchExecResult(raw)

                                    mapOf(
                                        "isSuccess" to res.isSuccess,
                                        "code" to res.code,
                                        "time" to res.time,
                                        "successCount" to res.successCount,
                                        "failCount" to res.failCount
                                    )
                                }

                                result.success(execMap)
                            } catch (e: Exception) {
                                result.success(
                                    mapOf(
                                        "isSuccess" to false,
                                        "code" to 3,
                                        "output" to e.message
                                    )
                                )
                            }
                        }
                    }

                    "queryParamOptions" -> {
                        val funcName = call.argument<String>("funcName") ?: ""
                        val paramName = call.argument<String>("paramName") ?: ""

                        scope.launch {
                            try {
                                val payload = withContext(Dispatchers.IO) {
                                    ShellNative.queryParamOptions(funcName, paramName)
                                }

                                result.success(payload)
                            } catch (_: Exception) {
                                result.success("[]")
                            }
                        }
                    }

                    "installApk" -> {
                        val path = call.argument<String>("path") ?: ""
                        try {
                            val file = File(path)
                            if (!file.exists()) { result.success(false); return@setMethodCallHandler }

                            val intent = Intent(Intent.ACTION_VIEW)
                            intent.addFlags(Intent.FLAG_ACTIVITY_NEW_TASK)
                            intent.addFlags(Intent.FLAG_GRANT_READ_URI_PERMISSION)

                            if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.N) {
                                val apkUri = FileProvider.getUriForFile(
                                    this@MainActivity,
                                    "${packageName}.fileprovider",
                                    file
                                )
                                intent.setDataAndType(apkUri, "application/vnd.android.package-archive")
                            } else {
                                intent.setDataAndType(
                                    android.net.Uri.fromFile(file),
                                    "application/vnd.android.package-archive"
                                )
                            }
                            startActivity(intent)
                            result.success(true)
                        } catch (e: Exception) {
                            result.success(false)
                        }
                    }

                    else -> result.notImplemented()
                }
            }
    }
}
