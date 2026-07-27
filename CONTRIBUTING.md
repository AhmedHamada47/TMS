# Contributing to TaskFlow TMS

Thank you for your interest in contributing! This document outlines the coding standards, workflow, and expectations for pull requests.

## Code of Conduct

This project follows a **no-tolerance** policy for harassment, discrimination, or disrespectful behavior. Be kind, be constructive, and assume good faith.

## Coding Standards

### Style

All code must conform to the rules defined in `.editorconfig` at the repository root:

| Rule | Value |
|---|---|
| Indentation | Spaces (4 per level for C#, 2 for HTML/JSON/YAML) |
| Encoding | UTF-8 |
| Line endings | LF |
| Trailing whitespace | Trimmed |
| Final newline | Required |
| `var` usage | Explicit types preferred (IDE0008 enforced as warning) |

These rules are enforced at build time via `<EnforceCodeStyleInBuild>true</EnforceCodeStyleInBuild>`.

### XML Documentation

All public classes, interfaces, methods, and properties **must** include XML doc comments (`/// <summary>`, `/// <param>`, `/// <returns>`). The build fails if any public member is undocumented (`<GenerateDocumentationFile>true</GenerateDocumentationFile>`).

### Naming Conventions

| Element | Convention | Example |
|---|---|---|
| Classes / Interfaces | PascalCase | `TaskService`, `ITaskService` |
| Methods | PascalCase | `GetFilteredTasksAsync` |
| Parameters | camelCase | `orgId`, `isManagerOrAbove` |
| Private fields | `_camelCase` | `_context`, `_cache` |
| Local variables | camelCase | `totalTasksTask` |
| Constants | PascalCase | `DefaultPageSize` |

### Async Patterns

- Async methods should be suffixed with `Async`
- Use `Task.WhenAll` for parallel independent queries, then access results via `.Result` (not redundant `await`)
- Avoid `.Result` or `.Wait()` on uncompleted tasks

### Architecture

- **Controllers** — thin, delegate to services, set `ViewBag` / return views
- **Services** — contain business logic, query `AppDbContext`, return DTOs/view models
- **Repositories** — not used; services query EF Core directly (minimal abstraction)
- **ViewModels** — shaped specifically for each view; not reused across unrelated views

## Pull Request Process

1. **Fork** the repository and create a feature branch from `main`
2. **Run the full test suite** before opening a PR:
   ```bash
   dotnet test TMS.Tests/TMS.Tests.csproj
   ```
3. **Ensure CI passes** — all workflows in `.github/workflows/` must be green:
   - `ci.yml` — builds both projects, runs all tests with code coverage, runs `dotnet format --verify-no-changes`
   - `codeql.yml` — security analysis (triggers automatically on PRs to `main`)
4. **Keep PRs focused** — one feature or bug fix per PR. Refactoring should be in a separate PR
5. **Write tests** — new features should include unit tests for service methods and/or integration tests for controller actions
6. **Update documentation** if your change affects the API surface, setup, or architecture

### Before Submitting

- [ ] `dotnet build` succeeds with 0 errors
- [ ] `dotnet test` passes all 51+ tests
- [ ] `dotnet format --verify-no-changes` passes (or `dotnet format` applied)
- [ ] XML doc comments added for any new public members
- [ ] No secrets, connection strings, or credentials committed

## Setting Up Locally

```bash
git clone <repo-url>
cd TMS
dotnet restore
dotnet run --project TMS
```

See `README.md` for detailed setup and configuration instructions.

## Questions?

Open a [GitHub Issue](https://github.com/anomalyco/opencode/issues) for questions, feature requests, or bug reports.
