# Contributing to aqua-backend-ng

## Branching

- `main` is always green and deployable.
- Feature branches: `feat/<short-description>` or `fix/<short-description>`.
- Open a PR to `main`. CI must pass and 1 review must approve.

## Commit messages

Follow Conventional Commits:

| Prefix | Example |
|---|---|
| `feat:` | `feat(data): add OutboxWriter` |
| `fix:` | `fix(contracts): TenantId rejects whitespace-only` |
| `docs:` | `docs: update DEVELOPMENT.md` |
| `chore:` | `chore: bump xunit to 2.9.2` |
| `test:` | `test(data): cover SessionScope edge cases` |
| `ci:` | `ci: add docker-build workflow` |
| `refactor:` | `refactor(data): extract SessionFactoryRegistry` |

Scopes match the project name: `contracts`, `data`, `devstack`, `identity`, `requirement`, etc.

## Code style

- `Directory.Build.props` enforces `TreatWarningsAsErrors=true`, `Nullable=enable`.
- 4-space indentation (see `.editorconfig`).
- TDD: write the test first whenever practical.
- Frequent small commits.

## Pull request checklist

- [ ] Tests added or updated
- [ ] `dotnet build` passes locally
- [ ] `dotnet test` passes locally
- [ ] No new warnings
- [ ] Conventional-commit messages
- [ ] PR description references relevant Sub-Spec (SS-NN) and Master-Spec section
