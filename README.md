# TaskFlow TMS

A multi-user task management system built with ASP.NET Core 9.0 MVC.

## Features

- Multi-tenant organizations with role-based access (Admin, Manager, Employee)
- Task CRUD with search, filter, sort, and pagination
- Kanban board with drag-and-drop
- Teams and cross-user task assignment
- Threaded comments and activity audit log
- In-app notifications (bell with live polling)
- Manager efficiency dashboard with Chart.js
- Dark mode (persisted, follows system preference)
- User profiles with 30-day activity chart

## Tech Stack

- **Backend:** ASP.NET Core 9.0, EF Core 9.0, SQLite, BCrypt
- **Frontend:** Bootstrap 5, FontAwesome 6, Chart.js, SortableJS
- **Auth:** Cookie authentication with custom claims + policies

## Quick Start

```bash
git clone <repo-url>
cd src/TMS
dotnet restore
dotnet run
```

Open `http://localhost:5213` and register a new account to get started.

## Project Structure

```
Controllers/    — MVC controllers
Models/         — Entity models
ViewModels/     — View-specific models
Views/          — Razor views
Data/           — DbContext + migrations
wwwroot/        — Static assets (CSS, JS)
docs/           — Project analysis documents
```

## License

MIT
