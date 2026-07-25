package com.liar.byzymztools.util

import com.liar.byzymztools.util.native.ShellNative
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.withContext

object PluginExecutor {
    class ParamsBuilder {
        private val params = mutableListOf<String>()
        fun path(key: String, value: String) { params += "$key:$value" }
        fun bool(key: String, value: Boolean) { params += "$key:$value" }
        fun str(key: String, value: String)  { if (value.isNotEmpty()) params += "$key:$value" }
        fun int(key: String, value: Int)     { params += "$key:$value" }
        fun raw(key: String, value: String)  { params += "$key:$value" }
        internal fun build(): Array<String> = params.toTypedArray()
    }

    suspend fun run(
        pluginId: String,
        block: ParamsBuilder.() -> Unit
    ): ShellNative.ExecResult = withContext(Dispatchers.IO) {
        val params = ParamsBuilder().apply(block).build()
        val raw    = ShellNative.executeFunction(pluginId, params)
        ShellNative.parseExecResult(raw)
    }

    fun runSync(
        pluginId: String,
        block: ParamsBuilder.() -> Unit
    ): ShellNative.ExecResult {
        val params = ParamsBuilder().apply(block).build()
        val raw    = ShellNative.executeFunction(pluginId, params)
        return ShellNative.parseExecResult(raw)
    }
}
