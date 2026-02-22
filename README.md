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
- Data: Cloud Firestore ("bring your own Firebase" – each user owns their entire database)
- Optional GitHub API calls (REST / GraphQL) for enriched issue metadata

### Data Model
```
todos/{docId}
  title: string
  isComplete: boolean
  order: number
  createdAt: timestamp
  updatedAt: timestamp
```
(Subject to iteration as features evolve.)

## Security Notes
Each user provides their own Firebase project ("bring your own Firebase"), so there is no shared backend. The entire database belongs to the user.

Recommended Firestore Security Rules:
```
rules_version = '2';
service cloud.firestore {
  match /databases/{database}/documents {
    match /{document=**} {
      allow read, write: if request.auth != null;
    }
  }
}
```
Firebase web API keys are intentionally public identifiers, not secrets.
Do NOT commit service account keys or admin credentials.
