package com.liar.byzymztools.util.native

object ShellNative {
    external fun initKernel(scriptDir: String): String
    external fun matchFunctions(filePath: String): String
    external fun executeFunction(funcName: String, params: Array<String>): String
    external fun executeBatchFunction(
        funcName: String,
        inputFolder: String,
        outputFolder: String,
        extraParams: Array<String>
    ): String
    external fun queryParamOptions(funcName: String, paramName: String): String

    data class ExecResult(val code: Int, val time: Double, val output: String) {
        val isSuccess get() = code == 0
    }

    data class BatchExecResult(val code: Int, val time: Double, val successCount: Int, val failCount: Int) {
        val isSuccess get() = code == 0 && successCount > 0
    }

    fun parseExecResult(raw: String): ExecResult {
        val parts = raw.split(":", limit = 3)
        return ExecResult(
            code = parts.getOrNull(0)?.toIntOrNull() ?: 3,
            time = parts.getOrNull(1)?.toDoubleOrNull() ?: 0.0,
            output = parts.getOrElse(2) { "" }
        )
    }

    fun parseBatchExecResult(raw: String): BatchExecResult {
        val parts = raw.split(":")
        return BatchExecResult(
            code = parts.getOrNull(0)?.toIntOrNull() ?: 3,
            time = parts.getOrNull(1)?.toDoubleOrNull() ?: 0.0,
            successCount = parts.getOrNull(2)?.toIntOrNull() ?: 0,
            failCount = parts.getOrNull(3)?.toIntOrNull() ?: 0
        )
    }
}
