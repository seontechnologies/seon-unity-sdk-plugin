# SEON Unity SDK — Internal Developer Guide

> For developers who don't work with Unity daily. This covers environment setup, development workflow, testing, CI/CD, and release process.

## Repository Structure

```
seon-unity-sdk-plugin/
├── io.seon.unitysdk/              # The SDK package (what gets distributed)
│   ├── package.json               # UPM manifest — name, version, dependencies
│   ├── .npmignore                  # Controls what goes into the UPM .tgz tarball
│   ├── Runtime/                   # C# source — ships to customers
│   │   ├── SeonSDK.cs             # Main public API (static class)
│   │   ├── Internal/              # Platform bridges (iOS, Android, Editor stub)
│   │   └── Plugins/iOS/           # Native Objective-C bridge (.mm)
│   ├── Editor/                    # Editor-only code (build processors, deps XML)
│   └── Tests/                     # EditMode + PlayMode tests
│       ├── Editor/                # Runs without Play mode (fast, synchronous)
│       └── Runtime/               # Runs in simulated Play mode (async, coroutines)
├── seon-unity-sdk-sample/         # Sample Unity project used for CI testing
│   └── Packages/manifest.json     # References the SDK via local path
└── .github/workflows/ci.yml       # CI/CD pipeline
```

## Environment Setup

### 1. Install Unity Hub + Unity Editor

1. Download [Unity Hub](https://unity.com/download)
2. In Unity Hub → Installs → Install Editor → select **Unity 6000.3.x LTS**
   - Match the version in `ci.yml` (`UNITY_VERSION: "6000.3.7f1"`)
   - Under Modules, add: **iOS Build Support**, **Android Build Support** (with NDK/SDK)
3. Open `seon-unity-sdk-sample` as the project in Unity Hub

### 2. IDE Setup

- **Rider** (recommended): Unity Hub → Preferences → External Tools → set Rider
- **VS Code**: Install the [C# Dev Kit](https://marketplace.visualstudio.com/items?itemName=ms-dotnettools.csdevkit) extension

### 3. Accounts & Licenses

The CI uses a **dedicated Unity account** for license activation:

| Secret           | Purpose                                         |
| ---------------- | ----------------------------------------------- |
| `UNITY_LICENSE`  | The `.ulf` license file content (base64 or XML) |
| `UNITY_EMAIL`    | Unity account email (required by game-ci v4)    |
| `UNITY_PASSWORD` | Unity account password                          |

These are stored in **GitHub → Settings → Secrets and variables → Actions**.

#### Generating a New License File

If you upgrade the Unity version, the `.ulf` file must be regenerated:

```bash
# 1. On your machine, activate and generate the .ulf
/Applications/Unity/Hub/Editor/<VERSION>/Unity.app/Contents/MacOS/Unity \
  -batchmode -nographics -quit \
  -createManualActivationFile

# 2. This creates Unity_v<VERSION>.alf — upload it at:
#    https://license.unity3d.com/manual
# 3. Download the resulting .ulf file
# 4. Copy the ENTIRE .ulf contents into the UNITY_LICENSE GitHub secret
```

> **Note:** The account has 2FA enabled. The `.ulf` flow bypasses 2FA since it doesn't require interactive login.

## Development Workflow

### Making SDK Changes

1. Edit files under `io.seon.unitysdk/Runtime/` or `io.seon.unitysdk/Editor/`
2. The sample project (`seon-unity-sdk-sample`) references the SDK via `file:../../io.seon.unitysdk` in its `manifest.json`, so changes are reflected immediately
3. Run tests locally before pushing (see Testing section)

### Adding New Files

When adding new `.cs` files or folders:

1. Let Unity generate the `.meta` file (open the project in the editor after adding)
2. **Always commit the `.meta` file** alongside the new file
3. If adding a new folder, add the folder's `.meta` file to the `"files"` array in `io.seon.unitysdk/package.json` AND add a `!FolderName.meta` entry in `.npmignore`

### Updating the Version

1. Edit `version` in `io.seon.unitysdk/package.json`
2. Update `CHANGELOG.md`
3. After merging to master, create a GitHub Release with tag `v<VERSION>` (e.g., `v1.1.0`)

### Updating the Unity Version

1. Install the new version in Unity Hub
2. Open `seon-unity-sdk-sample` with the new editor — it will upgrade the project
3. Run tests locally to verify
4. Update `UNITY_VERSION` in `.github/workflows/ci.yml`
5. **Regenerate the `.ulf` license** (see above) and update the `UNITY_LICENSE` secret
6. Verify the Docker image exists: `docker manifest inspect unityci/editor:ubuntu-<VERSION>-linux-il2cpp-3`

## Testing

### Running Tests Locally

```bash
# EditMode tests (fast, ~1s)
/Applications/Unity/Hub/Editor/6000.3.7f1/Unity.app/Contents/MacOS/Unity \
  -batchmode -nographics \
  -projectPath seon-unity-sdk-sample \
  -runTests -testPlatform EditMode \
  -testResults test-results-editmode.xml \
  -logFile -

# PlayMode tests (simulated, ~3s)
/Applications/Unity/Hub/Editor/6000.3.7f1/Unity.app/Contents/MacOS/Unity \
  -batchmode -nographics \
  -projectPath seon-unity-sdk-sample \
  -runTests -testPlatform PlayMode \
  -testResults test-results-playmode.xml \
  -logFile -
```

Or in the Unity Editor: **Window → General → Test Runner** → EditMode/PlayMode tab → Run All.

### Test Structure

| Suite               | Location         | What it tests                                                   |
| ------------------- | ---------------- | --------------------------------------------------------------- |
| EditMode (16 tests) | `Tests/Editor/`  | API contracts, error handling, session management — synchronous |
| PlayMode (8 tests)  | `Tests/Runtime/` | Async callbacks, coroutine flow, thread dispatching — simulated |

### Writing New Tests

- **EditMode**: Standard NUnit. If your code calls `Debug.LogError`, add `LogAssert.Expect(LogType.Error, ...)` before the call or the test will fail
- **PlayMode**: Use `[UnityTest]` + `IEnumerator` for async. Tests run in simulated mode (no device needed)

## CI/CD Pipeline

### Workflow Triggers

| Trigger                        | What runs                                            |
| ------------------------------ | ---------------------------------------------------- |
| **Pull Request → master**      | Tests + Package Assembly                             |
| **Push to master**             | Tests + Package Assembly + iOS/Android Sample Builds |
| **GitHub Release published**   | Tests + Package Assembly + Release Asset Upload      |
| **Manual (workflow_dispatch)** | Tests + Package Assembly + iOS/Android Sample Builds |

### Jobs

```
┌──────────────────────────┐
│  EditMode + PlayMode     │
│  Tests                   │
│  (medium-runner)         │
└──────────┬───────────────┘
           │
     ┌─────┼──────────────────────┐
     │     │                      │
     ▼     ▼                      ▼
┌────────┐ ┌────────────┐  ┌────────────┐
│Package │ │Build iOS   │  │Build       │
│Assembly│ │Sample      │  │Android     │
│        │ │(macos)     │  │Sample      │
└───┬────┘ └────────────┘  └────────────┘
    │
    ▼
┌────────────────┐
│Publish Release │
│Assets          │
│(on release     │
│ only)          │
└────────────────┘
```

### Artifacts Produced

| Artifact                | Contents                          | When                      |
| ----------------------- | --------------------------------- | ------------------------- |
| `test-results-editmode` | NUnit XML results                 | Always                    |
| `test-results-playmode` | NUnit XML results                 | Always                    |
| `seon-upm-package`      | `.tgz` tarball for UPM            | After tests pass          |
| `seon-unity-package`    | `.unitypackage` for legacy import | After tests pass          |
| `ios-build`             | Xcode project                     | master / manual / release |
| `android-build`         | APK/AAB                           | master / manual / release |

### Runner Configuration

- Ubuntu jobs use `medium-runner` (4-core, 16GB RAM org runner)
- iOS build uses `macos-latest` (GitHub-hosted, required for Xcode)

### Manually Triggering CI

Go to **Actions → SEON Unity SDK CI → Run workflow** → select your branch → **Run workflow**. This runs everything including iOS/Android sample builds.

## Distribution Channels

### UPM via Git URL (primary)

Customers add to their `manifest.json`:

```json
{
  "dependencies": {
    "io.seon.unitysdk": "https://github.com/seontechnologies/seon-unity-sdk-plugin.git?path=io.seon.unitysdk"
  },
  "scopedRegistries": [
    {
      "name": "OpenUPM",
      "url": "https://package.openupm.com",
      "scopes": ["com.google.external-dependency-manager"]
    }
  ]
}
```

### UPM Tarball (.tgz)

Downloaded from GitHub Release assets. Installed via Package Manager → Add from tarball.

### .unitypackage (legacy)

Downloaded from GitHub Release assets. Installed via Assets → Import Package → Custom Package. Requires EDM4U installed separately.

### OpenUPM (future)

Not yet set up. Would allow `"io.seon.unitysdk": "1.0.0"` via scoped registry.

## Release Checklist

1. Ensure all tests pass on CI
2. Bump `version` in `io.seon.unitysdk/package.json`
3. Update `CHANGELOG.md`
4. Merge PR to `master`
5. Go to GitHub → Releases → **Draft a new release**
6. Create tag `v<VERSION>` (e.g., `v1.1.0`) targeting `master`
7. Click **Publish release**
8. CI automatically uploads `.tgz` and `.unitypackage` as release assets

## Troubleshooting

| Issue                                       | Fix                                                                        |
| ------------------------------------------- | -------------------------------------------------------------------------- |
| "Missing Signature" popup in Unity 6        | Normal for third-party packages — click Close                              |
| CI fails with "UNITY_LICENSE is empty"      | Check GitHub Secrets → `UNITY_LICENSE` is set                              |
| CI fails with package resolution            | Verify `UNITY_VERSION` in `ci.yml` matches `ProjectVersion.txt`            |
| Tests fail with unexpected `Debug.LogError` | Add `LogAssert.Expect()` before the call                                   |
| `.meta` file warnings in UPM tarball        | Ensure new folders are listed in `package.json` `"files"` and `.npmignore` |
| `npm pack` includes unwanted files          | Check `.npmignore` and `"files"` in `package.json`                         |
