module;
#include <jni.h>
#include <cstdint>
#include <functional>
#include <map>
#include <string>
#include <vector>
export module utility.io.jni;

export class JniHelper {
public:
    inline static JavaVM* g_vm = nullptr;

    static void cacheVM(JNIEnv* env) {
        if (!g_vm && env) env->GetJavaVM(&g_vm);
    }

    static std::string jstringToString(JNIEnv* env, jstring s);
    static jstring     stringToJstring(JNIEnv* env, const std::string& s);

    static std::vector<std::string> jstringArrayToVector(JNIEnv* env, jobjectArray arr);
    static jobjectArray vectorToJstringArray(JNIEnv* env, const std::vector<std::string>& v);

    static std::vector<uint8_t> jbyteArrayToVector(JNIEnv* env, jbyteArray arr);
    static jbyteArray vectorToJbyteArray(JNIEnv* env, const std::vector<uint8_t>& v);
    static std::vector<char> jbyteArrayToCharVector(JNIEnv* env, jbyteArray arr);
    static jbyteArray charVectorToJbyteArray(JNIEnv* env, const std::vector<char>& v);

    static jintArray createIntArray(JNIEnv* env, const std::vector<int>& vals);

    static std::map<std::string, std::string> jstringArrayToMap(JNIEnv* env, jobjectArray arr);

    static bool checkAndClearException(JNIEnv* env);
    static void throwRuntimeException(JNIEnv* env, const std::string& msg);
    static void throwIOException(JNIEnv* env, const std::string& msg);

    static JNIEnv* getEnv(bool& attached);
    static void detachEnv(bool attached);
    static std::string decodeString(JNIEnv* env, const std::vector<uint8_t>& bytes, const std::string& charset);
    static std::vector<uint8_t> encodeString(JNIEnv* env, const std::string& str, const std::string& charset);

    struct JStringGuard {
        JStringGuard(JNIEnv* e, jstring s) : env(e), js(s),
            chars(s ? e->GetStringUTFChars(s, nullptr) : nullptr) {}
        ~JStringGuard() { if (chars && js) env->ReleaseStringUTFChars(js, chars); }
        const char* c_str() const { return chars; }
        std::string str()   const { return chars ? chars : ""; }
        JStringGuard(const JStringGuard&) = delete;
        JStringGuard& operator=(const JStringGuard&) = delete;
    private:
        JNIEnv* env; jstring js; const char* chars;
    };

    struct JByteArrayGuard {
        JByteArrayGuard(JNIEnv* e, jbyteArray a) : env(e), ja(a),
            bytes(a ? e->GetByteArrayElements(a, nullptr) : nullptr),
            length(a ? e->GetArrayLength(a) : 0) {}
        ~JByteArrayGuard() { if (bytes && ja) env->ReleaseByteArrayElements(ja, bytes, JNI_ABORT); }
        jbyte* data()     const { return bytes; }
        jsize  size()     const { return length; }
        JByteArrayGuard(const JByteArrayGuard&) = delete;
        JByteArrayGuard& operator=(const JByteArrayGuard&) = delete;
    private:
        JNIEnv* env; jbyteArray ja; jbyte* bytes; jsize length;
    };

    template<typename Func>
    static bool safeCall(JNIEnv* env, Func&& fn) {
        try { fn(); return !checkAndClearException(env); }
        catch (...) { checkAndClearException(env); return false; }
    }
};

inline std::string JniHelper::jstringToString(JNIEnv* env, jstring s) {
    cacheVM(env);
    if (!s) return {};
    JStringGuard g(env, s);
    return g.str();
}

inline jstring JniHelper::stringToJstring(JNIEnv* env, const std::string& s) {
    cacheVM(env);
    return env->NewStringUTF(s.c_str());
}

inline std::vector<std::string> JniHelper::jstringArrayToVector(JNIEnv* env, jobjectArray arr) {
    cacheVM(env);
    if (!arr) return {};
    jsize len = env->GetArrayLength(arr);
    std::vector<std::string> v;
    v.reserve(len);
    for (jsize i = 0; i < len; ++i) {
        auto js = (jstring)env->GetObjectArrayElement(arr, i);
        if (js) { v.push_back(jstringToString(env, js)); env->DeleteLocalRef(js); }
    }
    return v;
}

inline jobjectArray JniHelper::vectorToJstringArray(JNIEnv* env, const std::vector<std::string>& v) {
    cacheVM(env);
    auto cls = env->FindClass("java/lang/String");
    if (!cls) return nullptr;
    auto arr = env->NewObjectArray(v.size(), cls, nullptr);
    env->DeleteLocalRef(cls);
    if (!arr) return nullptr;
    for (size_t i = 0; i < v.size(); ++i) {
        auto js = stringToJstring(env, v[i]);
        if (js) { env->SetObjectArrayElement(arr, i, js); env->DeleteLocalRef(js); }
    }
    return arr;
}

inline std::vector<uint8_t> JniHelper::jbyteArrayToVector(JNIEnv* env, jbyteArray arr) {
    cacheVM(env);
    if (!arr) return {};
    JByteArrayGuard g(env, arr);
    return std::vector<uint8_t>(
        reinterpret_cast<uint8_t*>(g.data()),
        reinterpret_cast<uint8_t*>(g.data()) + g.size());
}

inline jbyteArray JniHelper::vectorToJbyteArray(JNIEnv* env, const std::vector<uint8_t>& v) {
    cacheVM(env);
    auto arr = env->NewByteArray(v.size());
    if (!arr) return nullptr;
    env->SetByteArrayRegion(arr, 0, v.size(), reinterpret_cast<const jbyte*>(v.data()));
    return arr;
}

inline std::vector<char> JniHelper::jbyteArrayToCharVector(JNIEnv* env, jbyteArray arr) {
    cacheVM(env);
    if (!arr) return {};
    JByteArrayGuard g(env, arr);
    return std::vector<char>(g.data(), g.data() + g.size());
}

inline jbyteArray JniHelper::charVectorToJbyteArray(JNIEnv* env, const std::vector<char>& v) {
    cacheVM(env);
    auto arr = env->NewByteArray(v.size());
    if (!arr) return nullptr;
    env->SetByteArrayRegion(arr, 0, v.size(), reinterpret_cast<const jbyte*>(v.data()));
    return arr;
}

inline jintArray JniHelper::createIntArray(JNIEnv* env, const std::vector<int>& vals) {
    cacheVM(env);
    auto arr = env->NewIntArray(vals.size());
    if (!arr) return nullptr;
    env->SetIntArrayRegion(arr, 0, vals.size(), vals.data());
    return arr;
}

inline std::map<std::string, std::string> JniHelper::jstringArrayToMap(JNIEnv* env, jobjectArray arr) {
    cacheVM(env);
    std::map<std::string, std::string> m;
    if (!arr) return m;
    jsize len = env->GetArrayLength(arr);
    for (jsize i = 0; i + 1 < len; i += 2) {
        auto jk = (jstring)env->GetObjectArrayElement(arr, i);
        auto jv = (jstring)env->GetObjectArrayElement(arr, i + 1);
        if (jk && jv) m[jstringToString(env, jk)] = jstringToString(env, jv);
        if (jk) env->DeleteLocalRef(jk);
        if (jv) env->DeleteLocalRef(jv);
    }
    return m;
}

inline bool JniHelper::checkAndClearException(JNIEnv* env) {
    if (env->ExceptionCheck()) { env->ExceptionClear(); return true; }
    return false;
}

inline void JniHelper::throwRuntimeException(JNIEnv* env, const std::string& msg) {
    auto cls = env->FindClass("java/lang/RuntimeException");
    if (cls) { env->ThrowNew(cls, msg.c_str()); env->DeleteLocalRef(cls); }
}

inline void JniHelper::throwIOException(JNIEnv* env, const std::string& msg) {
    auto cls = env->FindClass("java/io/IOException");
    if (cls) { env->ThrowNew(cls, msg.c_str()); env->DeleteLocalRef(cls); }
}

inline JNIEnv* JniHelper::getEnv(bool& attached) {
    attached = false;
    if (!g_vm) return nullptr;
    JNIEnv* env = nullptr;
    jint res = g_vm->GetEnv(reinterpret_cast<void**>(&env), JNI_VERSION_1_6);
    if (res == JNI_EDETACHED) {
        if (g_vm->AttachCurrentThread(&env, nullptr) == JNI_OK) attached = true;
        else return nullptr;
    }
    return env;
}

inline void JniHelper::detachEnv(bool attached) {
    if (attached && g_vm) {
        g_vm->DetachCurrentThread();
    }
}

inline std::string JniHelper::decodeString(JNIEnv* env, const std::vector<uint8_t>& bytes, const std::string& charset) {
    if (bytes.empty()) return "";
    jbyteArray jBytes = vectorToJbyteArray(env, bytes);
    jstring jCharset = stringToJstring(env, charset);

    jclass strClass = env->FindClass("java/lang/String");
    jmethodID ctor = env->GetMethodID(strClass, "<init>", "([BLjava/lang/String;)V");
    jstring jStr = (jstring)env->NewObject(strClass, ctor, jBytes, jCharset);

    std::string result = jstringToString(env, jStr);

    env->DeleteLocalRef(jStr);
    env->DeleteLocalRef(strClass);
    env->DeleteLocalRef(jCharset);
    env->DeleteLocalRef(jBytes);
    return result;
}

inline std::vector<uint8_t> JniHelper::encodeString(JNIEnv* env, const std::string& str, const std::string& charset) {
    if (str.empty()) return {};
    jstring jStr = stringToJstring(env, str);
    jstring jCharset = stringToJstring(env, charset);

    jclass strClass = env->FindClass("java/lang/String");
    jmethodID getBytes = env->GetMethodID(strClass, "getBytes", "(Ljava/lang/String;)[B");
    jbyteArray jBytes = (jbyteArray)env->CallObjectMethod(jStr, getBytes, jCharset);

    std::vector<uint8_t> result = jbyteArrayToVector(env, jBytes);

    env->DeleteLocalRef(jBytes);
    env->DeleteLocalRef(strClass);
    env->DeleteLocalRef(jCharset);
    env->DeleteLocalRef(jStr);
    return result;
}
