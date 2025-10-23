# What's Happening?

A supercharged, GitHub-aware productivity and task management app built with Blazor WebAssembly and backed by Firebase.

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

## Development Roadmap (High Level)
1. Project scaffold & GitHub Pages deployment
2. Firebase integration (Auth + Firestore) & secure rules baseline
3. Core task CRUD + ordering + local UI state management
4. Completion & archiving flows + history logging
5. GitHub issue linking (manual URL input first, then search enhancement)
6. Filtering / tag management UX
7. Reminders & stale task prompts
8. Board view & extended visualizations
9. Metrics / charts & polish
10. Documentation refinement & onboarding experience

## Getting Started (to be expanded)
1. Clone repo & open in IDE supporting .NET.
2. Create Firebase project; enable GitHub provider; add Firestore.
3. Insert Firebase config into appsettings or JS interop bootstrap.
4. Run locally: `dotnet run` (details coming once scaffold exists).
5. Deploy to GitHub Pages (workflow setup forthcoming).

## Contributing / Direction
Currently focused on personal use and learning. Issues / ideas welcome once initial scaffold lands.

## License
(TBD)

---
"What's Happening?" – keeping tasks and code happenings aligned.