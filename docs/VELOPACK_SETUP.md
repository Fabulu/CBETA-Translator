# Velopack Auto-Update Setup

[Velopack](https://velopack.io) ships ReadZen as a real installer (not a zip) and
powers in-app auto-update across Windows, Linux, and macOS. Phase 5 of
`RUN-20260416-1711-ewk-friendly-install` integrated it.

## What you get

| Platform | Installer format | Auto-update | Signing? |
|---|---|---|---|
| Windows | `Setup.exe` | ✅ In-app, silent background download | Unsigned — SmartScreen warns once (see README's "Known friction") |
| Linux | `.AppImage` (single file) | ✅ In-app via Velopack, zsync delta | N/A — not required |
| macOS (x64 + arm64) | `.pkg` | ✅ In-app when unsigned-app is allowed through Gatekeeper | Unsigned — user has to allow via System Settings (see README) |

The **existing zip artifacts keep shipping** from `release.yml` — users on plain
zip installs fall back to the old browser-redirect update banner. Velopack-
installed users get the in-app flow automatically.

## How it's wired

### Runtime (desktop app)

`Program.Main` calls `VelopackApp.Build().Run()` before anything else. On plain
zip builds this is a no-op; on `vpk pack`-produced installs it intercepts
`--veloapp-install` / `--veloapp-update` command-line flags.

`Services/AppUpdateService.cs` wraps Velopack's `UpdateManager` and gates on
`IsInstalled`. If the build wasn't packaged by vpk (i.e. zip install), the
service falls back to a plain GitHub API check and the old "open browser" path.

`App.axaml.cs` calls `AppUpdateService.CheckForUpdatesAsync()` 5 seconds after
startup and shows `MainWindow.ShowUpdateNotification` when an update is found.
The banner's Download button behavior adapts:

- **Velopack install + update available** → "Install & Restart" button → downloads
  in-place via Velopack → restarts the app.
- **Velopack install + Velopack failure** (Avalonia issue [#146](https://github.com/velopack/velopack/issues/146)
  or similar stall) → falls back to opening the release page in the browser.
- **Zip install** → same behavior as before: opens the release page.

### CI (release.yml)

On every `v*` tag push, each platform's build job now runs these steps *after*
the usual zip packaging:

1. `dotnet tool install -g vpk --version 0.0.942` — pinned for reproducibility.
2. Platform-specific `vpk pack` with `--packId Fabulu.ReadZen`, `--mainExe ReadZen.App(.exe)`,
   and `--packDir` pointing at the `publish/` directory.
3. Velopack outputs land in `dist/velopack/` and get copied into `dist/` for
   upload alongside the zip artifacts.

The `release` job (single job that creates the GitHub release) then uploads
everything in `dist/` — zips AND Velopack installers.

## Signing (future work)

Velopack doesn't require signing to function, but unsigned installers still
show OS warnings on first launch. The cost breakdown:

| OS | Option | Cost | Effect |
|---|---|---|---|
| Windows | Azure Trusted Signing | ~$120/yr | SmartScreen never warns, even for new hashes |
| Windows | EV code-signing cert | $300+/yr | Same as ATS but older, more hoops |
| macOS | Apple Developer + notarization | $99/yr | Gatekeeper lets unsigned-but-notarized apps run |
| Linux | — | free | AppImage doesn't need signing |

**Current stance:** unsigned everywhere, friction documented on the landing
page and README. Revisit when download volume data justifies the spend.

### How to add Windows signing later

1. Set up Azure Trusted Signing:
   [learn.microsoft.com/en-us/azure/trusted-signing](https://learn.microsoft.com/en-us/azure/trusted-signing/)
2. Add these secrets to the ReadZen repo:
   - `AZURE_TENANT_ID`
   - `AZURE_CLIENT_ID`
   - `AZURE_CLIENT_SECRET`
   - `AZURE_CERT_PROFILE_NAME`
3. Extend the Windows `vpk pack` step with:
   ```bash
   --signCommand 'azuresigntool sign ...'
   ```
4. Commit the `.github/workflows/release.yml` change; ship a new release;
   SmartScreen warnings disappear for this cert.

### How to add macOS notarization later

1. Get an Apple Developer account ($99/yr).
2. Create a Developer ID Application cert + App-Specific Password.
3. Add repo secrets: `APPLE_ID`, `APPLE_APP_PASSWORD`, `APPLE_TEAM_ID`,
   `APPLE_SIGNING_IDENTITY`.
4. Remove `--noSignatureValidation` from the macOS `vpk pack` step.
5. Add `--signEntitlements` + `--notarize` args per Velopack's macOS docs.

## Troubleshooting

### "Velopack download stalls at 10%"
Known upstream issue
[velopack/velopack#146](https://github.com/velopack/velopack/issues/146).
`AppUpdateService.TryInstallAndRestartAsync` catches the failure and returns
false, which triggers the browser fallback in `MainWindow`. Users see the
release page open instead of getting stuck.

### "vpk pack: main executable not found"
`--mainExe` must match the exact filename inside the `publish/` directory
(with `.exe` on Windows, without on Linux/macOS). Check the publish output
if the build name changes.

### "Version does not satisfy SemVer"
Velopack requires strict 3-part SemVer (X.Y.Z). The workflow strips the `v`
prefix from tags automatically. Pre-release suffixes like `v4.5.0-rc1` will
fail — don't tag pre-releases on Velopack-integrated tags.

### Bumping Velopack
In two places:
- `ReadZen.App.csproj` — the `<PackageReference Include="Velopack" Version="..."/>`
- `.github/workflows/release.yml` — the `dotnet tool install --version ...`

Keep these in sync. Velopack is still 0.0.x and minor version bumps can break
package format, so rev one release at a time and test the update flow end-to-end
before tagging another.
