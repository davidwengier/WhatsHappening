# What's Happening?

A supercharged, GitHub-aware productivity and task management app built with Blazor WebAssembly and backed by Firebase.

Initial project template from https://github.com/Netonia/FluentBlazor

## Mission
Replace ad-hoc OneNote / plain lists with a focused, fast, and insightful task workspace that keeps personal tasks and linked GitHub issues in one coherent flow.

## Core Features (MVP)
- Task creation, editing, deletion
- Drag-and-drop reordering
- Complete & archive tasks (separate active vs historical list)
- Tags / filtering / search
- GitHub SSO (Firebase Authentication with GitHub provider)
- Link tasks to GitHub issues (display status, quick open link)
- Firestore persistence (sync across devices)
- Basic activity history (added, updated, completed, archived)

## Planned Enhancements
- Optional: Closing linked GitHub issue when marking task complete
- Activity feed including GitHub issue state changes
- Status prompts & gentle reminders for stale tasks
- Kanban board view (To Do / In Progress / Done columns)
- Calendar / timeline visualization
- Per-task notes / comments
- Statistics & trends (tasks completed per week, streaks, aging tasks)

## Architecture Overview
- Frontend: Blazor WebAssembly (client-only) hosted on GitHub Pages
- Auth: Firebase Authentication (GitHub OAuth) – only public config embedded client-side
- Data: Cloud Firestore (document model per user)
- Optional GitHub API calls (REST / GraphQL) for enriched issue metadata

### Data Model (Early Sketch)
```
users/{userId}
  profile: { displayName, createdAt }
  settings: { reminderFrequency, boardColumns }
users/{userId}/tasks/{taskId}
  title: string
  description: string
  tags: [string]
  order: number (for manual ordering)
  status: enum (active|in_progress|completed|archived)
  linkedIssue: { repo: string, number: int, state: string, title: string? }
  createdAt: timestamp
  updatedAt: timestamp
  completedAt: timestamp?
  archivedAt: timestamp?
users/{userId}/activity/{activityId}
  type: enum (create|update|complete|archive|linkIssue|githubUpdate)
  taskId: string
  timestamp: timestamp
  details: map
```
(Subject to iteration as features evolve.)

## Security Notes
Firebase web API keys are intentionally public identifiers, not secrets. Real protection is enforced via Firestore Security Rules:
- Read/write restricted to authenticated user on their own task documents
- Validation of allowed fields & size limits to mitigate abuse
Do NOT commit service account keys or admin credentials.
