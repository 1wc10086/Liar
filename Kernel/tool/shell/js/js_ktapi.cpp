module;
#include <jni.h>
#include <string>
#include "lib/quickjs_ng/quickjs.h"

module tool.shell.js_engine;

namespace {
jclass gKtBridge = nullptr;
jmethodID gRevorb = nullptr;
JavaVM* gJvm = nullptr;
thread_local JNIEnv* tEnv = nullptr;

JNIEnv* currentEnv() {
    if (tEnv) return tEnv;
    if (!gJvm) return nullptr;
    if (gJvm->GetEnv(reinterpret_cast<void**>(&tEnv), JNI_VERSION_1_6) == JNI_OK) return tEnv;
    return gJvm->AttachCurrentThread(&tEnv, nullptr) == JNI_OK ? tEnv : nullptr;
}
}

void JsEngine::initJni(JavaVM* vm) {
    gJvm = vm;
    JNIEnv* env = nullptr;
    if (!vm || vm->GetEnv(reinterpret_cast<void**>(&env), JNI_VERSION_1_6) != JNI_OK) return;
    jclass local = env->FindClass("com/byzymz/toolkit/shell_gui/KtBridge");
    if (!local) {
        if (env->ExceptionCheck()) env->ExceptionClear();
        return;
    }
    gKtBridge = static_cast<jclass>(env->NewGlobalRef(local));
    env->DeleteLocalRef(local);
    gRevorb = env->GetStaticMethodID(gKtBridge, "revorb", "(Ljava/lang/String;Ljava/lang/String;)Z");
    if (!gRevorb && env->ExceptionCheck()) env->ExceptionClear();
}

bool JsEngine::revorb(const std::string& inputPath, const std::string& outputPath) const {
    JNIEnv* env = currentEnv();
    if (!env || !gKtBridge || !gRevorb || env->PushLocalFrame(4) < 0) return false;
    jstring input = env->NewStringUTF(inputPath.c_str());
    jstring output = env->NewStringUTF(outputPath.c_str());
    const bool ok = input && output && env->CallStaticBooleanMethod(gKtBridge, gRevorb, input, output) == JNI_TRUE && !env->ExceptionCheck();
    if (!ok && env->ExceptionCheck()) env->ExceptionClear();
    env->PopLocalFrame(nullptr);
    return ok;
}

void JsEngine::registerKtApi(JSContext*, JsEngine*) {}
