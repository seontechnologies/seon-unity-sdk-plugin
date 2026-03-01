# Changelog

All notable changes to the SEON Unity SDK will be documented in this file.

## [1.0.0] - 2026-03-01

### Added

- Initial release of SEON Unity SDK
- iOS bridge via Objective-C++ (`DllImport`)
- Android bridge via `AndroidJavaObject` JNI
- Editor stub with mock fingerprints for in-editor testing
- Main thread dispatcher for native callback marshalling
- EDM4U dependency declarations (CocoaPods for iOS, Gradle/Maven for Android)
- Opt-in iOS post-build processor (`SeonBuildConfig` ScriptableObject)
- ProGuard/R8 rules for Android code minification
- EditMode unit tests (18 tests)
- PlayMode integration tests (4 device tests)
- Sample scene with complete UI integration demo
- UPM package format + `.unitypackage` distribution

### Native SDK Versions

- iOS: SeonSDK 5.6.3 (via CocoaPods)
- Android: io.seon.androidsdk 6.8.3 (via Maven Central)
