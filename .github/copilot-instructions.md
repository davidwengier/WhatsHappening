# Copilot Instructions for WhatsHappening

## Language & Framework Preferences

- **Always prefer .NET/C#** for all code, including utility scripts, tools, and automation. Do not use Node.js, Python, or other runtimes for tooling when .NET can do the job.
- The main app is **Blazor WebAssembly** (.NET 10) with **FluentUI** components.
- The solution uses `.slnx` format. Add new projects to the solution file.

## Architecture

- **Firebase Authentication** with GitHub OAuth, using direct JS interop (not a NuGet wrapper). See `wwwroot/js/firebase-interop.js` and `Services/FirebaseAuthService.cs`.
- **"Bring your own Firebase"** model — each user provides their own Firebase project config, stored in `localStorage`. There is no shared backend. No user-scoping needed — each user owns their entire Firebase project.
- **Firestore** is the data store, using the compat SDK (`firebase-*-compat.js`). Collections: `todos`, `groups`, `settings`.
- The app is hosted on **GitHub Pages** under a subpath (`/WhatsHappening/`). Always use relative URLs in `NavigateTo()` calls (e.g., `"setup"` not `"/setup"`).

## Real-time Listeners & State Management

This is the most common source of bugs. The app uses Firestore `onSnapshot` listeners that push state changes to C# via `JSInvokable` callbacks (`OnTodosChanged`, `OnGroupsChanged`).

- **Never manually add/remove items from `_todoList` or `_groups` after a Firestore write.** The `onSnapshot` listener fires immediately on local writes and replaces the entire list. Manually adding creates duplicates; manually removing causes flickering.
- **Only modify local state for operations the listener doesn't cover** (e.g., clearing input fields, UI flags).
- **Guard listener callbacks during interactive operations.** `OnTodosChanged` already skips updates during drag operations (`_draggedItem is not null`) and preserves items being edited (`_editingItemIds`). Follow this pattern for any new interactive state.
- **Fire-and-forget async calls** (`_ = SomeAsync()`) swallow exceptions silently. Add `try/catch` with logging inside the method, or await the call. Current offenders: `RefreshAllGitHubStatuses()`.

## FluentUI Web Components — Known Pitfalls

FluentUI Blazor uses web components with shadow DOM. This has caused repeated layout bugs:

- **Do NOT use FluentGrid or FluentSortableList for custom layouts.** Their shadow DOM containers break CSS class selectors and flex layout. Use plain HTML `div` elements with inline styles and `@foreach` loops instead.
- **CSS `gap` doesn't pass through shadow DOM** on components like FluentBadge. Use explicit `margin` on child elements.
- **`margin-left: auto` doesn't work** inside `fluent-stack` web components. Use plain `div` with `display:flex` for layout control.
- **FluentPersona shadow DOM** prevents reliable CSS media query targeting. Use plain `<img>` for avatar and `<span>` for name when responsive hiding is needed.
- **FluentAccordionItem HeadingTemplate** constrains layout — put interactive elements in `ChildContent`, not the heading.
- **FluentTextField.FocusAsync()** returns `void`, not `Task` — it cannot be awaited.

## Component Design

- **Always factor shared UI into reusable components.** Rendering the same data (e.g., a todo item) in multiple places with different code paths causes styling inconsistencies. `TodoItemRow.razor` and `TodoGroupSection.razor` exist for this reason.
- **When adding a new display variant** (e.g., bordered vs non-bordered groups), add a parameter to the existing component rather than creating a parallel code path.
- Keep Razor pages self-contained where practical (markup + code in one `.razor` file).

## Razor Syntax Gotchas

- **Adjacent `@()` expressions in attribute strings** (e.g., `style="@(a)@(b)"`) cause CS1073 parser errors. Use a computed string property instead.
- Use `[Authorize]` on pages that require authentication. Setup, login, and help pages should remain anonymous.
- Firebase web API keys are public identifiers — safe to commit. Never commit service account keys or GitHub tokens.

## External API Integration

- **GitHub OAuth token in `sessionStorage`** is lost on page reload. Firebase restores auth state but not the OAuth credential. Always handle `null` token gracefully — public repo API calls work without auth.
- **GitHub API `state` field** returns `"closed"` for both closed and merged PRs. Check the `merged` boolean to distinguish: `if (type == "pull" && state == "closed" && merged == true) state = "merged"`.
- **Azure DevOps** uses PAT-based auth stored in Firestore `settings` collection. Detect 401 responses and prompt for re-auth.

## Tooling

- **Screenshot tool**: `dotnet run --project tools/Screenshot -- <url> <output> [width] [height]` — takes screenshots using Edge with the user's profile. Defaults to 1280×800. See `.github/prompts/screenshot.prompt.md` for details.
- **Build**: `dotnet build` from repo root.
- **Deploy**: Push to `main` triggers GitHub Actions → builds and deploys to GitHub Pages.

## Cache Busting

- The deploy workflow auto-appends `?v=<git-sha>` to local CSS/JS references in `index.html`.
- A `version.json` file is stamped with the full git SHA at deploy time. The `UpdateChecker` component polls this to detect new versions.
