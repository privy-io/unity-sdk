# Releasing

Releases are cut by [release-please](https://github.com/googleapis/release-please) in this
repo (`privy-io/unity-sdk`). Tags are `vX.Y.Z`. The tag is the release: git URL and OpenUPM
consumers install from it. See [docs/releasing.md](./docs/releasing.md) for packaging and
versioning.

## Stable releases (from `main`)

1. 👤 Merge changes into `main`.
2. 🤖 On every push to `main`, release-please opens or updates the release PR
   (`chore(main): release X.Y.Z`). It bumps `version.txt`, `SDK/package.json` and
   `SDK/Runtime/Utils/SdkVersion.cs` and updates `SDK/CHANGELOG.md`.
3. 🤖 CI adds a **release proposal** comment to the release PR: the source commit and a
   compare link from the last published tag. Use it to review exactly what ships.
4. 👤 Approve and merge the release PR.
5. 🤖 CI runs the [release workflow](./.github/workflows/release-please.yml), which tags
   `vX.Y.Z` and creates the GitHub Release.
6. 👤 Update the changelog in [our public docs](https://docs.privy.io).
