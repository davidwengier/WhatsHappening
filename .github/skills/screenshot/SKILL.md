---
name: screenshot
description: Take a screenshot of the live deployed site to visually verify UI changes or debug layout issues. Use this when asked to look at the site, verify visual changes, or debug CSS/layout problems.
---

# Taking Screenshots

Use the C# Playwright-based screenshot tool to capture the live site with the user's authenticated session.

## Running the tool

```powershell
cd C:\Code\whatshappening
dotnet run --project tools/Screenshot -- [url] [output-path]
```

### Defaults
- **URL**: `https://wengier.com/WhatsHappening/`
- **Output**: `screenshot.png`

## How it works

The tool copies the user's Edge browser profile (localStorage, IndexedDB, preferences) to a temp directory, then launches Edge in headless mode with that profile. This preserves the user's Firebase config and login state so you see the authenticated app, not just the setup page.

## Steps

1. Run the screenshot tool:
   ```powershell
   dotnet run --project tools/Screenshot -- "https://wengier.com/WhatsHappening/" "screenshot.png"
   ```
2. View the resulting image using the `view` tool on the output file
3. Clean up after use:
   ```powershell
   Remove-Item screenshot.png
   ```

## Prerequisites

- Microsoft Edge installed at `C:\Program Files (x86)\Microsoft\Edge\Application\msedge.exe`
- The user must have previously logged in to the site in Edge
- Screenshot files are gitignored — do not commit them
