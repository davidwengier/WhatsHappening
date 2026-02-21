# Screenshot Skill

Take a screenshot of the live deployed site using the C# Playwright-based screenshot tool.

## Usage

```
dotnet run --project tools/Screenshot -- [url] [output-path]
```

### Defaults
- **URL**: `https://wengier.com/WhatsHappening/`
- **Output**: `screenshot.png`

## How it works

The tool copies the user's Edge browser profile (localStorage, IndexedDB, preferences) to a temp directory, then launches Edge in headless mode with that profile. This means the screenshot includes the user's authenticated session — Firebase config and login state are preserved.

## When to use

- After making UI changes, take a screenshot to verify the result visually
- When debugging layout issues
- When the user asks you to "look at" or "see" the site

## Example

```powershell
cd C:\Code\whatshappening
dotnet run --project tools/Screenshot -- "https://wengier.com/WhatsHappening/" "screenshot.png"
```

Then use the `view` tool to look at the resulting image file.

## Prerequisites

- Microsoft Edge must be installed at `C:\Program Files (x86)\Microsoft\Edge\Application\msedge.exe`
- The user must have previously logged in to the site in Edge (for auth state to be available)
- Playwright browsers do NOT need to be installed separately — the tool uses the local Edge installation directly

## Notes

- Screenshots are gitignored — don't commit them
- The temp profile copy is at `%TEMP%\playwright-edge-profile`
- Clean up screenshot files after use with `Remove-Item screenshot.png`
