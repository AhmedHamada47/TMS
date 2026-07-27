# TaskFlow TMS

A multi-user task management system built with **ASP.NET Core 9.0 MVC**. TaskFlow supports multi-tenant organizations with role-based access, Kanban boards, team management, productivity reporting, and real-time notifications.

![TaskFlow Dashboard](docs/dashboard.png)

## Features

- **Multi-tenant organizations** with role-based access (Admin, Manager, TeamLead, Employee)
- **Task CRUD** with search, filter, sort, and pagination
- **Kanban board** with drag-and-drop column positioning
- **Teams** and cross-user task assignment with primary assignee support
- **Threaded comments** on tasks with reply nesting
- **Activity audit log** tracking all field changes on tasks
- **In-app notifications** with unread badge and live polling
- **Manager reports dashboard** with per-employee completion rates, on-time rates, and cycle hours
- **User profiles** with 30-day activity streak chart
- **Dark mode** (persisted to localStorage, follows system preference)
- **Fully responsive** layout with collapsible sidebar

## Tech Stack

| Layer | Technology |
|---|---|
| **Backend** | ASP.NET Core 9.0, C# 12, EF Core 9.0 |
| **Database** | SQLite (dev), EF Core InMemory (test) |
| **Auth** | Cookie authentication, custom `OrganizationRole` claims, policy-based authorization |
| **Frontend** | Bootstrap 5.3, FontAwesome 6, Chart.js 4, SortableJS |
| **Testing** | xUnit, Moq, FluentAssertions, Microsoft.AspNetCore.Mvc.Testing, Coverlet |
| **CI/CD** | GitHub Actions (build, test, code coverage, CodeQL, Dependabot) |
| **API Docs** | Swashbuckle / Swagger UI |

## Prerequisites

- [.NET 9.0 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- (Optional) [SQLite](https://www.sqlite.org/download.html) CLI — useful for inspecting the database file

## Local Setup

```bash
# 1. Clone the repository
git clone <repo-url>
cd TMS

# 2. Restore dependencies
dotnet restore

# 3. Apply EF Core migrations (auto-runs on first startup)
dotnet run --project TMS

# 4. Open the app
open http://localhost:5213
```

The database file `TMS/tms.db` is created automatically on first run via `db.Database.Migrate()` in `Program.cs`. Seed data (users, categories, sample tasks, teams) is included in the initial migration.

### Registering

1. Navigate to `/Account/Register`
2. The first user to register in an organization is automatically assigned the **Admin** role
3. Subsequent users can be added by the Admin from the organization management pages

## Project Structure

```
TMS/
├── Components/          # ViewComponents (SidebarViewComponent)
├── Constants/           # Claim type constants
├── Controllers/         # MVC controllers
│   ├── AccountController.cs       # Registration, login, logout
│   ├── BaseController.cs          # Shared user/org context
│   ├── CategoriesController.cs    # Category CRUD
│   ├── HomeController.cs          # Dashboard with caching
│   ├── NotificationsController.cs # Bell + unread count
│   ├── ProfileController.cs       # User profile + activity chart
│   ├── ReportsController.cs       # Manager efficiency reports
│   └── TasksController.cs         # Task CRUD + Kanban board
├── Data/
│   ├── AppDbContext.cs            # EF Core context + seed data
│   └── Migrations/                # 7 EF Core migrations
├── Helpers/
│   └── PaginatedList.cs           # Generic pagination helper
├── Models/              # Entity models (User, TaskItem, Category, ...)
├── Services/            # Business logic layer
│   ├── ITaskService / TaskService         # Task CRUD, board, comments, activity logs
│   ├── ICategoryService / CategoryService # Category management
│   ├── INotificationService / NotificationService  # Create, list, mark-read
│   ├── IReportService / ReportService     # Team aggregation reports
│   ├── ITeamService / TeamService         # Team member queries
│   └── ISidebarService / SidebarService   # Navigation sidebar model
├── ViewModels/          # View-specific models
├── Views/               # Razor views + partials
└── wwwroot/
    └── css/
        ├── partials/            # Split CSS files
        └── site.css             # Entry point (imports partials)
```

## Running Tests

```bash
dotnet test TMS.Tests/TMS.Tests.csproj
```

The test suite contains:

- **36 unit tests** across 5 service test files (CategoryService, NotificationService, ReportService, TaskService, TeamService)
- **14 integration tests** across 3 controller test files (AccountController, HomeController, TasksController)
- InMemory databases keyed by unique `Guid` per test class to prevent cross-test pollution
- Integration tests use `WebApplicationFactory<Program>` with `"Testing"` environment (no migrations, InMemory provider)
- Antiforgery tokens extracted automatically via `IntegrationTestHelper`

### Code Coverage

```bash
dotnet test TMS.Tests/TMS.Tests.csproj --collect:"XPlat Code Coverage"
reportgenerator -reports:*/coverage.cobertura.xml -targetdir:coverage -reporttypes:Html
```

## Authentication & Multi-Tenancy

### How It Works

1. **Cookie Authentication** — users log in via `/Account/Login`. A cookie is issued containing the user's identity.
2. **Organization Membership** — every user belongs to one or more organizations via the `OrganizationMemberships` join table. Each membership carries a `Role` (Admin, Manager, TeamLead, Employee).
3. **Claim Injection** — `BaseController.OnActionExecutionAsync` reads the current user's organization membership from the database and sets `CurrentUserId`, `CurrentOrganizationId`, and `IsManagerOrAbove` on every request.
4. **Query Scoping** — all data queries filter by `OrganizationId` to ensure strict tenant isolation. E.g., `Context.Tasks.Where(t => t.OrganizationId == orgId)`.
5. **Authorization Policies** — defined in `Program.cs`:
   - `"AdminOnly"` — requires `OrganizationRole` claim = `"Admin"`
   - `"ManagerOrAbove"` — requires `"Manager"` or `"Admin"`
   - `"TeamLeadOrAbove"` — requires `"TeamLead"`, `"Manager"`, or `"Admin"`

### Password Hashing

Passwords are hashed using **BCrypt.Net-Next** with a work factor of 12.

## API Documentation (Swagger)

With the app running in Development mode:

```
GET http://localhost:5213/swagger
```

Swagger UI includes XML documentation comments from all controllers and services. Enable the `Development` environment to access it:

```bash
ASPNETCORE_ENVIRONMENT=Development dotnet run --project TMS
```

## Coding Standards

This project enforces code style through:

- `.editorconfig` at the repository root (UTF-8, LF line endings, space indentation)
- `<EnforceCodeStyleInBuild>true</EnforceCodeStyleInBuild>` — style violations fail the build
- `<GenerateDocumentationFile>true</GenerateDocumentationFile>` — all public API members require XML doc comments

Refer to `CONTRIBUTING.md` for detailed contribution guidelines.

## CI/CD

| Workflow | Trigger |
|---|---|
| **CI** (`.github/workflows/ci.yml`) | Push / PR to any branch — builds, runs tests, reports coverage, checks formatting |
| **CodeQL** (`.github/workflows/codeql.yml`) | Weekly (Mon 06:00) + push/PR to default branch |
| **Dependabot** (`.github/dependabot.yml`) | Weekly NuGet + GitHub Actions dependency updates |

## License

MIT

## Acknowledgements

- [Chart.js](https://www.chartjs.org/) for interactive charts
- [SortableJS](https://sortablejs.github.io/Sortable/) for Kanban drag-and-drop
- [FontAwesome](https://fontawesome.com/) for icons
- [Bootstrap](https://getbootstrap.com/) for layout and components
