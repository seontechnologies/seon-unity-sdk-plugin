# ──────────────────────────────────────────────────────
# SEON SDK ProGuard / R8 Rules
# Include these rules when code minification is enabled.
# ──────────────────────────────────────────────────────

# Keep all SEON SDK classes (required for JNI and reflection)
-keep class io.seon.androidsdk.** { *; }

# Keep JNI handler native methods
-keepclasseswithmembers,includedescriptorclasses class io.seon.androidsdk.service.JNIHandler {
    native <methods>;
}

# Keep callback interfaces used by Unity's AndroidJavaProxy
-keep interface io.seon.androidsdk.service.SeonCallback { *; }
-keep interface io.seon.androidsdk.service.SeonCallbackWithGeo { *; }

# Keep builder and config classes
-keep class io.seon.androidsdk.service.SeonBuilder { *; }
-keep class io.seon.androidsdk.dto.SeonGeolocationConfig { *; }
-keep class io.seon.androidsdk.dto.SeonGeolocationConfigBuilder { *; }
-keep class io.seon.androidsdk.exception.SeonException { *; }
-keep class io.seon.androidsdk.exception.BehaviouralMonitoringException { *; }
