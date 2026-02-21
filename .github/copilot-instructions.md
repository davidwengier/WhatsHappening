# Copilot Instructions for WhatsHappening

## Language & Framework Preferences

- **Always prefer .NET/C#** for all code, including utility scripts, tools, and automation. Do not use Node.js, Python, or other runtimes for tooling when .NET can do the job.
- The main app is **Blazor WebAssembly** (.NET 9) with **FluentUI** components.
- The solution uses `.slnx` format. Add new projects to the solution file.

## Architecture

- **Firebase Authentication** with GitHub OAuth, using direct JS interop (not a NuGet wrapper). See `wwwroot/js/firebase-interop.js` and `Services/FirebaseAuthService.cs`.
- **"Bring your own Firebase"** model — each user provides their own Firebase project config, stored in `localStorage`. There is no shared backend.
- The app is hosted on **GitHub Pages** under a subpath (`/WhatsHappening/`). Always use relative URLs in `NavigateTo()` calls (e.g., `"setup"` not `"/setup"`).

## Conventions

- Keep Razor pages self-contained where practical (markup + code in one `.razor` file).
- Use `[Authorize]` on pages that require authentication. Setup, login, and help pages should remain anonymous.
- Firebase web API keys are public identifiers — safe to commit. Never commit service account keys or GitHub tokens.

## Tooling

- **Screenshot tool**: `dotnet run --project tools/Screenshot` — takes screenshots of the live site using Edge with the user's profile. See `.github/prompts/screenshot.prompt.md` for details.
- **Build**: `dotnet build` from repo root.
- **Deploy**: Push to `main` triggers GitHub Actions → builds and deploys to GitHub Pages.

## Cache Busting

- The deploy workflow auto-appends `?v=<git-sha>` to local CSS/JS references in `index.html`.
- A `version.json` file is stamped with the full git SHA at deploy time. The `UpdateChecker` component polls this to detect new versions.
