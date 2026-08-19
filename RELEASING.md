# Releasing

This describes the actual, current release process for TDesu.FSharp — verified against
`.github/workflows/release.yml`, `src/TDesu.FSharp/TDesu.FSharp.fsproj`, and the git tag
history. If this file and `release.yml` ever disagree, `release.yml` is right; fix this
file to match.

## How the version is derived

The package version comes entirely from [MinVer](https://github.com/adamralph/minver),
wired in via `<PackageReference Include="MinVer" PrivateAssets="all" />` and
`<MinVerTagPrefix>v</MinVerTagPrefix>` in `src/TDesu.FSharp/TDesu.FSharp.fsproj`. There is
no version number hardcoded anywhere in the project files.

- MinVer walks back from `HEAD` to the nearest reachable annotated tag matching `v*`
  (`v1.4.0`, `v1.3.0`, ...) and uses that as the version.
- On the tagged commit itself, the version is exactly the tag with the `v` stripped
  (`v1.4.1` → `1.4.1`).
- On any other commit (a tagless build — a normal PR build, a push to `master` between
  releases, a local `dotnet build`), MinVer falls back to `0.0.0-alpha.0.<height>`, where
  `<height>` is the commit count since the last tag (or since the repo root if there is no
  tag reachable). This fallback build is *not* meant to be published — see
  [Known issue](#known-issue-stray-alpha-versions-on-nugetorg) below.
- `git describe --tags --always` in the release workflow's "Verify tag is reachable" step
  is a sanity echo only; it does not feed MinVer directly, but if it can't see the tag
  neither can MinVer, so a red flag there means the release will ship the alpha fallback
  instead of the real version.

## Versioning policy

Semantic versioning, applied by what actually changed in the library surface:

- **Patch** (`1.4.0` → `1.4.1`) — docs, CI/build infrastructure, test-only changes,
  internal refactors with no observable API change. This is the release this backlog
  produces: layout, infrastructure, docs, tests — no public API changes.
- **Minor** (`1.4.x` → `1.5.0`) — additive, backward-compatible API changes (new
  functions/modules/types; nothing existing removed or changed in a breaking way).
- **Major** (`1.x` → `2.0.0`) — breaking API changes (renames, removals, signature or
  semantic changes to existing public members).

## Cutting a release

1. Update `RELEASE_NOTES.md`: add a new `## X.Y.Z` section at the top, following the
   existing format (`### Added` / `### Changed` / `### Fixed` / `### Removed` as needed).
2. Commit the release notes (and anything else the release includes):
   ```
   git commit -am "docs: add X.Y.Z release notes"
   ```
3. Tag the commit — **annotated**, with the `v` prefix MinVer is configured for:
   ```
   git tag -a vX.Y.Z -m "vX.Y.Z"
   ```
   (Every existing tag except `v1.1.0` is annotated; keep it that way — MinVer works
   either way, but `git describe` and GitHub's release UI treat annotated tags as the
   real thing.)
4. Push the branch first, then the tag:
   ```
   git push origin master
   git push origin vX.Y.Z
   ```
   The tag push is what triggers the release — pushing the branch alone does not.

## What the tag push triggers

Pushing a `v*` tag runs `.github/workflows/release.yml` (`publish` job, `ubuntu-latest`):

1. Checks out with `fetch-depth: 0` and `fetch-tags: true` (needed so MinVer and
   `git describe` can actually see the tag history), then fetches tags again and echoes
   `git describe` and the latest `v*` tag as a sanity check.
2. Sets up .NET 10.
3. `dotnet restore`
4. `dotnet build -c Release --no-restore`
5. `dotnet test -c Release --no-build --verbosity normal` — the release does not ship if
   the test suite doesn't pass.
6. `dotnet pack -c Release --no-build -o ./artifacts`
7. Pushes every `./artifacts/*.nupkg` to **nuget.org**
   (`https://api.nuget.org/v3/index.json`, `--skip-duplicate`), authenticated with the
   `NUGET_API_KEY` repository secret.
8. Pushes the same packages to **GitHub Packages**
   (`https://nuget.pkg.github.com/techiedesu/index.json`, `--skip-duplicate`),
   authenticated with the workflow's own `GITHUB_TOKEN` (the job has `packages: write`
   permission for this; no separate secret to manage).

A tagless push to `master` never runs this workflow — only `CI` (`ci.yml`) does, and it
never packs or publishes.

### Secrets required

- `NUGET_API_KEY` — a nuget.org API key scoped to the `TDesu.FSharp` package, stored as a
  repository secret. Nothing else is needed for GitHub Packages: `GITHUB_TOKEN` is
  minted automatically per workflow run.

## Verifying it landed

- GitHub Actions: the `Release` workflow run for the pushed tag should be green through
  "Push to NuGet" and "Push to GitHub Packages".
- nuget.org: `https://www.nuget.org/packages/TDesu.FSharp/X.Y.Z` should exist within a
  few minutes (indexing lag). `--skip-duplicate` means a re-run after a partial failure
  is safe — it won't error on a version that already made it through.
- GitHub Packages: the package appears under the repository's Packages tab
  (`https://github.com/techiedesu/TDesu.FSharp/pkgs/nuget/TDesu.FSharp`).
- `docs.yml` / fsdocs' `FsDocsReleaseNotesLink` point at `RELEASE_NOTES.md` on `master`,
  so the published docs pick up the new section automatically once `master` has it —
  no separate action needed there.

## Known issue: stray alpha versions on nuget.org

A tagless CI run once ended up pushing MinVer's fallback version — `0.0.0-alpha.0.*` —
to nuget.org. That version number is never something anyone should install; it exists
only because a build ran without a reachable tag. If you find one of these listed,
unlist it (nuget.org → package → that version → "Unlist"); do not delete it (NuGet
package deletion is otherwise irreversible and can break anyone who already resolved it).
Going forward, `release.yml` only runs on `v*` tag pushes with `fetch-tags: true`, so a
correctly tagged release should never reproduce this.
