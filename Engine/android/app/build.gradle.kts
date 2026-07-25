import java.util.Properties

plugins {
    id("com.android.application")
    id("kotlin-android")
    id("dev.flutter.flutter-gradle-plugin")
}

val localProperties = Properties()
val localPropertiesFile = rootProject.file("local.properties")
if (localPropertiesFile.exists()) {
    localPropertiesFile.reader(Charsets.UTF_8).use { reader: java.io.Reader ->
        localProperties.load(reader)
    }
}

val flutterVersionCode = localProperties.getProperty("flutter.versionCode") ?: "8"
val flutterVersionName = localProperties.getProperty("flutter.versionName") ?: "1.0.7"

android {
    namespace = "com.byzymz.toolkit.shell_gui"
    compileSdk = 37
    ndkVersion = "29.0.14206865"

    compileOptions {
        sourceCompatibility = JavaVersion.VERSION_25
        targetCompatibility = JavaVersion.VERSION_25
    }

    sourceSets {
        getByName("main") {
            java.srcDirs("src/main/kotlin")
        }
    }

    defaultConfig {
        applicationId = "com.byzymz.toolkit.shell_gui"
        minSdk = 31
        targetSdk = 37
        versionCode = flutterVersionCode.toInt()
        versionName = flutterVersionName

        externalNativeBuild {
            cmake {
                arguments += "-DANDROID_STL=c++_shared"
            }
        }
    }

    buildTypes {
        release {
            signingConfig = signingConfigs.getByName("debug")
            isMinifyEnabled = true
            isShrinkResources = true
            proguardFiles(
                getDefaultProguardFile("proguard-android-optimize.txt"),
                "proguard-rules.pro"
            )
        }
    }

    externalNativeBuild {
        cmake {
            version = "4.3.0"
            path = file("CMakeLists.txt")
        }
    }

    packagingOptions {
        jniLibs {
            pickFirsts += setOf(
                "**/libc++_shared.so",
                "**/libsnappyjava.so",
                "**/liblz4-java.so"
            )
        }
    }
}

flutter {
    source = "../.."
}

dependencies {
    implementation("org.jetbrains.kotlin:kotlin-stdlib-jdk8:2.3.0")
    implementation("dev.mccue:jorbis:2024.04.19")
}

gradle.afterProject {
    if (hasProperty("android")) {
        val android = property("android") as com.android.build.gradle.BaseExtension
        android.compileSdkVersion(37)
    }
}
