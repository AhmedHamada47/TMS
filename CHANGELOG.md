# Changelog

All notable schema changes to the TaskFlow TMS database are documented here.

## Migration 7 — `AddCommentsActivityLogsAndNotifications` (2026-07-23)

- **Added** `TaskComments` table (id, task FK, user FK, parent comment FK for threaded replies, content, created-at)
- **Added** `TaskActivityLogs` table (id, task FK, user FK, field-name, old/new value, created-at) — audit trail for all field changes
- **Added** `Notifications` table (id, user FK, message, optional link, is-read flag, created-at)
- **Created** indexes on all FK columns and common query columns (`IsRead`, `CreatedAt`)

## Migration 6 — `AddEstimatedAndActualHours` (2026-07-23)

- **Added** `EstimatedHours` (decimal, nullable) and `ActualHours` (decimal, nullable) columns to the `Tasks` table
- **Updated** seed data with estimated hours for all existing tasks

## Migration 5 — `AddProjectsBoardsAndKanban` (2026-07-23)

- **Added** `BoardColumnId` (nullable FK) and `BoardOrder` (integer) columns to the `Tasks` table
- **Added** `Projects` table (id, name, description, organization FK, created-at)
- **Added** `Boards` table (id, name, project FK, created-at)
- **Added** `BoardColumns` table (id, board FK, name, display order, created-at)
- **Seeded** one project, one board, and four board columns (To Do, In Progress, Review, Done)
- **Updated** all existing tasks with board column assignments and order positions

## Migration 4 — `AddTeamsAndAssignees` (2026-07-23)

- **Added** `TaskAssignees` table (id, task FK, user FK, is-primary flag) — supports multiple assignees per task with one primary
- **Added** `Teams` table (id, name, organization FK, manager FK, created-at)
- **Added** `TeamMemberships` table (id, team FK, user FK, role, joined-at)
- **Created** unique composite indexes to prevent duplicate assignments/memberships
- **Seeded** one team (Engineering) with all three users as members

## Migration 3 — `AddOrganizationSupport` (2026-07-23)

> **Major:** Introduces multi-tenancy.

- **Added** `Organizations` table (id, name, created-at)
- **Added** `OrganizationMemberships` table (id, organization FK, user FK, role, joined-at)
- **Added** `OrganizationId` column to the `Tasks` and `Categories` tables
- **Replaced** the per-user unique category index with a per-organization unique index (`IX_Categories_Name_OrganizationId`)
- **Seeded** one organization (TaskFlow Demo) with all three users as members (Ahmed as Admin, others as Members)
- **Updated** all existing tasks and categories to belong to the demo organization
- **Added** foreign keys with cascade delete from org to tasks/categories

## Migration 2 — `MakeCategoryNameUniquePerUser` (2026-07-23)

- **Dropped** the old global unique index on `Categories.Name`
- **Created** a composite unique index on `(Name, UserId)` — allowing different users to have categories with the same name

## Migration 1 — `InitialCreate` (2026-07-22)

- **Added** `Users` table (id, name, email, password hash, avatar URL, created-at)
- **Added** `Categories` table (id, name, description, color, user FK, created-at)
- **Added** `Tasks` table (id, title, description, status, priority, created-at, updated-at, due-date, category FK, user FK)
- **Created** unique indexes on `Users.Email` and `Categories.Name`
- **Created** performance indexes on all task filter/sort columns (status, priority, due-date, created-at, category, user)
- **Seeded** three sample users (Ahmed, Sara, Mohamed), five categories, and eight tasks spanning various statuses and priorities
