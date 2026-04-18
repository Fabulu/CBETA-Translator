# WinGet Publishing Setup

ReadZen publishes to the [WinGet package manager](https://learn.microsoft.com/en-us/windows/package-manager/) so Windows users can install with one command:

```powershell
winget install ReadZen
```

WinGet hosts its manifests at [microsoft/winget-pkgs](https://github.com/microsoft/winget-pkgs). When Microsoft serves the binary through its CDN, **SmartScreen no longer warns** — that's the main reason this path matters.

This doc covers the **one-time bootstrap** (initial manifest submission) and the **ongoing CI automation** for every release.

---

## One-time bootstrap (manual, ~20 minutes)

The CI workflow in `.github/workflows/winget-publish.yml` can only *update* an existing WinGet package. The very first manifest has to be hand-submitted. Do this once per package identifier.

### 1. Create a GitHub PAT

1. Visit [github.com/settings/tokens](https://github.com/settings/tokens) → **Generate new token (classic)**
2. Scope: **`public_repo`** (only — this is all the winget-releaser action needs)
3. Expiration: 1 year (renew calendar reminder)
4. Copy the token — you'll need it twice (local `wingetcreate` + GitHub Actions secret)

### 2. Add the PAT as a repository secret

1. ReadZen repo → **Settings → Secrets and variables → Actions → New repository secret**
2. Name: `WINGET_PAT`
3. Value: the PAT from step 1
4. Save

### 3. Install WingetCreate locally (Windows only)

```powershell
winget install wingetcreate
```

### 4. Generate + submit the initial manifest

Replace the URL and version with the latest release once it's live on GitHub:

```powershell
# Create the manifest interactively from a published release
wingetcreate new https://github.com/Fabulu/ReadZen/releases/download/v4.4.0/ReadZen-win-x64-v4.4.0.zip
```

WingetCreate will prompt for:

| Field | Value |
|---|---|
| Package identifier | `Fabulu.ReadZen` |
| Publisher | `Fabulu` |
| Package name | `Read Zen` |
| Moniker (optional) | `readzen` |
| Publisher URL | `https://github.com/Fabulu/ReadZen` |
| Publisher support URL | `https://github.com/Fabulu/ReadZen/issues` |
| Author | `Fabulu` |
| Package URL | `https://github.com/Fabulu/ReadZen` |
| License | `MIT` |
| License URL | `https://github.com/Fabulu/ReadZen/blob/main/LICENSE` |
| Copyright | `Copyright (c) Fabulu` |
| Short description | `Read, translate, and research classical Chinese Zen texts across CBETA and OpenZen.` |
| Description | `A free desktop workspace for Chinese Zen literature with side-by-side translation, hover dictionary, full-text search, tagging, scholar collections, and community sharing.` |
| Tags | `zen buddhism cbeta openzen chinese translation tei scholarly` |
| Installer type | `zip` |
| Nested installer type | `portable` |
| Relative file path (inside zip) | `ReadZen.App.exe` |

When prompted, let WingetCreate submit the PR directly to `microsoft/winget-pkgs`. It will use the PAT from `wingetcreate settings` (run `wingetcreate settings` once first and paste in the PAT).

### 5. Wait for Microsoft to merge

The PR goes through automated validation bots + a human moderator. Typical turnaround: **1–3 days**. You'll get email notifications. If validators flag issues, they comment on the PR; fix and push to your fork branch.

Once merged, `winget install ReadZen` works immediately for anyone.

---

## Ongoing CI automation (zero manual work after bootstrap)

After the initial manifest is merged, the CI workflow in `.github/workflows/winget-publish.yml` handles every subsequent release:

1. Push a version tag (e.g. `v4.5.0`) — triggers `release.yml`, which builds and publishes the GitHub release.
2. When the release transitions to "published" (not draft), `winget-publish.yml` fires.
3. It runs [vedantmgoyal2009/winget-releaser@v2](https://github.com/vedantmgoyal2009/winget-releaser) which:
   - Finds the Windows zip artifact from the release (by the regex `ReadZen-win-x64-v[\d.]+\.zip$`)
   - Computes its SHA256
   - Forks microsoft/winget-pkgs
   - Bumps the version in the existing manifest
   - Opens a PR
4. Microsoft's moderators merge. Done.

### Manual re-run

If the automation fails for a given release, fire it again:

```
GitHub → ReadZen → Actions → WinGet Publish → Run workflow → tag: v4.5.0
```

(The workflow accepts a `tag` input via `workflow_dispatch`.)

---

## Troubleshooting

### "Secret `WINGET_PAT` not found"
Add the secret (step 2 above) and re-run the workflow.

### "Package identifier does not exist"
The bootstrap PR hasn't merged yet. Wait or check status at [microsoft/winget-pkgs/pulls](https://github.com/microsoft/winget-pkgs/pulls?q=ReadZen).

### "Installers regex matched 0 artifacts"
The zip naming in `release.yml` changed. Update `installers-regex` in `winget-publish.yml` accordingly.

### Validation bot flags the manifest
Most common: checksum mismatch (rebuild + re-PR), incorrect `InstallerType` (must be `zip` for our portable distribution), missing `NestedInstallerFiles` (must point at `ReadZen.App.exe`).

### PAT expired
Regenerate (step 1), update the `WINGET_PAT` secret (step 2), re-run the latest failed run.

---

## Costs + caveats

- **$0 recurring.** GitHub PAT is free, microsoft/winget-pkgs hosting is free, Microsoft signs the CDN-served binaries on its end.
- **Human gate.** Every release waits 1–3 days for Microsoft moderator review. Don't count on same-day Windows distribution.
- **Only Windows benefits.** macOS / Linux users still download from GitHub Releases directly.
