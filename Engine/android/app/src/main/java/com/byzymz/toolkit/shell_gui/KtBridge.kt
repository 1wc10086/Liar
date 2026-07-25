package com.byzymz.toolkit.shell_gui

import androidx.annotation.Keep
import java.util.concurrent.ConcurrentHashMap
import java.util.concurrent.atomic.AtomicLong
import com.byzymz.toolkit.shell_gui.ktapi.OtherHandler

@Keep
object KtBridge {
    private val nextHandle = AtomicLong(1L)
    private val handleMap = ConcurrentHashMap<Long, Any>()
    private const val SESSION_OUT_CHUNK = 64 * 1024
    private const val STREAM_COPY_CHUNK = 1024 * 1024

    @JvmStatic
    fun call(method: String, args: Array<Any?>): Any = try {
        when (method) {
            "ogg.revorb" -> OtherHandler.revorb(args.requireStr(0, "input"), args.requireStr(1, "output"))
            else -> error("unknown: $method")
        }
    } catch (e: Exception) {
        """{"error":"${e.message?.escapeJson() ?: "unknown"}"}"""
    }
    
    private fun Array<Any?>.str(i: Int): String? = getOrNull(i) as? String
    
    private fun Array<Any?>.requireStr(i: Int, name: String = "arg[$i]"): String =
        when (val v = getOrNull(i)) {
            is String -> v
            is ByteArray -> String(v, Charsets.UTF_8)
            else -> str(i) ?: error("missing $name")
        }
        
        private fun String.escapeJson() =
        replace("\\", "\\\\").replace("\"", "\\\"").replace("\n", "\\n").replace("\r", "\\r")

    @JvmStatic
    fun revorb(input: String, output: String): Boolean = OtherHandler.revorb(input, output)
}
