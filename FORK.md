# Fork notes

This is a fork of [lippdev/consolemode](https://github.com/lippdev/consolemode) that has become its own
product (the "lean" edition): fewer features, a new interface. Changes and releases happen **here**, never
upstream. Upstream is only a source of ideas.

The dojo's `keeper` agent (see `1r0n51uv/ron1n-dojo`, FLOW-013, `forks/consolemode.yaml` with `mode: review`)
acts **only when upstream publishes a new release**. It merges nothing: it opens a PR from `upstream/<tag>`
with one file, `docs/upstream/<tag>.md`, that sorts every upstream change into **bring**, **rewrite**,
**skip** or **unclear**, using the two tables below. 1ron51uv decides; the chosen work is then done in
separate PRs. Keep the tables current: they are what the agent measures against.

## Versions

Ours is upstream's version plus a fourth number, our own variant: with upstream at 1.7.1 we publish
1.7.1.1, 1.7.1.2, and so on. When upstream releases 1.7.2 and we take something from it, the next one is
1.7.2.1. The number is set by hand in `src/ConsoleMode/ConsoleMode.csproj` (`Version`, `AssemblyVersion`,
`FileVersion`, `InformationalVersion`) with a `## [<version>]` section in both changelogs. The keeper never
changes it. (1.7.2, published earlier with the Italian catalog, is above this line: the first install of
1.7.1.1 is done by hand over it, because the update check does not offer a lower version.)

## Removed (upstream changes here are skipped, unless 1ron51uv asks otherwise)

| What | Where it lived |
|---|---|
| TV control (Google TV / Android TV, Wake-on-LAN, HDMI input, standby on restore) | `Services/Tv/`, `MainViewModel.Tv.cs`, the TV settings tab, `TvControlConfig` |
| RTSS: FPS limit and FPS counter | `Services/RtssService.cs`, `RtssOverlay.cs`, `HardwareMonitor.cs`, `HardwareStats.cs`, `MainViewModel.FpsOverlay.cs`, `rtss-cli` |
| Per-game profiles | `Services/GameProfiles.cs`, `ConsoleEngine.GameProfiles.cs`, `MainViewModel.GameProfiles.cs` |
| Local control API and `consolemode://` links | `Services/ControlPipeService.cs`, `ControlProtocol.cs`, `ProtocolService.cs` |
| DDC/CI panel power-off | `NativeDisplays.SetPower`, `MonitorService.DisableDdc`, the "turn off the panel" hide strategy |
| Languages es-ES, de-DE, fr-FR, ru-RU | `Resources/Strings.<code>.json`, their READMEs and guides, installer entries |

## Rewritten (upstream changes here need the idea redone in our style)

| What | Where | How it is now |
|---|---|---|
| The whole look: liquid glass over a PS5-style home | `App.xaml` (glass resources, `ConsoleSurfaceTemplate`, `ConsoleFlatTemplate`, styles), `Views/ConsoleHomeView.xaml(.cs)`, `SessionMenuWindow.xaml` | Acrylic fill + lit edge, glass top bar with the tabs in a capsule, spring lift on focus, ambient tint of the selected launcher, `CardMaterial` (System → App → Glass) |
| System tab categories | `ConsoleHomeView.xaml` (`SystemSection0..6`) | One category fewer than upstream (TV removed): indexes shift, so upstream's category numbers do not apply |
| Session menu rows | `SessionMenuWindow.xaml`, `MainViewModel.SessionMenu.cs` | No FPS, counter or profile rows; the tour has no FPS stop |

Anything else is "bring" if it merges cleanly (`git merge-tree`) and does not touch the above.

## What else we changed

- **Italian**: `src/ConsoleMode/Resources/Strings.it-IT.json` (every key of `Strings.en-US.json`),
  `README.it-IT.md`, and its wiring (`ConsoleMode.csproj`, `Tests.csproj`, `LocalizationService.cs`,
  `LocalizationTests.cs`, `build/ConsoleMode.iss`). `pt-BR` stays: it is upstream's source language, the
  fallback and the changelog's. Every new key goes in pt-BR, en-US and it-IT.
- **Updates and links point at the fork**: `UpdateService.Repository`, the changelog link in `release.yml`,
  the installer URLs in `build/ConsoleMode.iss`. Never take upstream's value here.
- **No fork-sync workflow**: nothing merges or releases by itself. `winget.yml` and `release.yml`'s winget
  job stay off (no `WINGET_TOKEN`).

## Rules for the keeper's review PR

1. Only for a new upstream release, one PR per release, branch `upstream/<tag>`, base `main`.
2. The only file it adds is `docs/upstream/<tag>.md`: upstream's news, then one table per class
   (item | file | what it does | proposal). The code, versions, translations and changelogs are not touched.
3. The base of the comparison is the highest upstream tag before `<tag>` that is an ancestor of `main` or
   already has an `upstream/<tag>` PR.
4. The PR gets `needs-review`; it is never merged by the agent. A closed PR is not redone.
5. Chosen work is done afterwards, on request, in separate PRs (one topic each), in our style.
