# Privy Unity SDK

Unity SDK for Privy authentication and embedded wallet functionality. Distributed as a UPM package (`com.privy.unity-sdk`), targeting Unity 2022.3+.

## Repository Structure

```
SDK/            # UPM package — runtime code, editor tools, native plugins
SampleApp/      # Unity project demonstrating SDK usage
docs/           # Developer documentation (releasing.md, native-code.md)
agent_docs/     # AI assistant reference docs (code conventions, PR review rules)
Format.csproj   # Used for dotnet format (covers SDK/, excludes ExternalDependencies/)
version.txt     # Canonical version (managed by release-please — do not edit manually)
```

For detailed SDK architecture, conventions, and patterns, see **[SDK/CLAUDE.md](SDK/CLAUDE.md)**.

## Essential Commands

```bash
# Format all SDK source (run before committing)
dotnet format Format.csproj
```

## Commit Conventions

Use [Conventional Commits](https://www.conventionalcommits.org/) — `feat:`, `fix:`, `chore:`, `docs:`, etc. Release-please uses these to generate the changelog and determine the next version bump automatically.

## Release Process

See [RELEASING.md](RELEASING.md).

## GitHub Actions

| Workflow             | Trigger      | What it does                        |
| -------------------- | ------------ | ----------------------------------- |
| `claude.yml`         | PR           | Automated code review               |
| `format-check.yml`   | PR           | Verifies `dotnet format` was run    |
| `pr-title.yml`       | PR           | Enforces conventional commit format |
| `release-please.yml` | Push to main | Manages release PRs and tags        |
