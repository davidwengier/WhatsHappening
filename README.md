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

## Setup Guide

What's Happening uses your own Firebase project for authentication and data storage. Follow these steps to get everything configured.

### Step 1: Create a Firebase Project
1. Go to the [Firebase Console](https://console.firebase.google.com)
2. Click **Add project** (or select an existing one)
3. Follow the prompts to create your project (you can disable Google Analytics if you like)

### Step 2: Add a Web App
1. In your Firebase project, go to **Project Settings** (gear icon in the top-left sidebar)
2. Scroll down to **"Your apps"**
3. Click **Add app** and choose the **Web** icon (`</>`)
4. Enter a nickname (e.g. "WhatsHappening") and click **Register app**
5. Firebase will display a config snippet containing your **API Key**, **Auth Domain**, and **Project ID** — you'll need these for the setup page

### Step 3: Register a GitHub OAuth App
1. Go to [GitHub Developer Settings](https://github.com/settings/developers)
2. Click **New OAuth App**
3. Fill in the form:
   - **Application name:** anything you like (e.g. "WhatsHappening")
   - **Homepage URL:** your app's URL
   - **Authorization callback URL:** copy this from Firebase (see Step 4 — you may need to come back and fill this in)
4. Click **Register application**
5. Note the **Client ID**, then click **Generate a new client secret** and copy it

### Step 4: Enable GitHub Sign-In in Firebase
1. In the Firebase Console, go to **Authentication** (left sidebar) → **Sign-in method**
2. Click **Add new provider** → **GitHub**
3. Toggle it to **Enabled**
4. Paste the **Client ID** and **Client Secret** from your GitHub OAuth App
5. Copy the **Authorization callback URL** shown by Firebase and paste it into your GitHub OAuth App's callback URL field (from Step 3)
6. Click **Save**

### Step 5: Enable Cloud Firestore
1. In the Firebase Console, go to **Firestore Database** (left sidebar under "Build")
2. Click **Create database**
3. Choose a location close to you, then click **Next**
4. Select **Start in production mode**, then click **Create**
5. Once created, go to the **Rules** tab and replace the default rules with the security rules above
6. Click **Publish** to save the rules

> Since this is your own Firebase project, these rules simply require you to be signed in. All data in the database is yours.

### Step 6: Enter Your Config
1. Go to the app's Setup page
2. Enter the three values from your Firebase web app config:
   - **API Key** — starts with `AIzaSy...`
   - **Auth Domain** — looks like `your-project.firebaseapp.com`
   - **Project ID** — your Firebase project ID
3. Click **Save & Continue**, then sign in with GitHub

> **Setting up another device?** After saving your config, the setup page shows a shareable link you can open on another browser or device to skip these steps.
