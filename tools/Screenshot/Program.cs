using Microsoft.Playwright;

var url = args.Length > 0 ? args[0] : "https://wengier.com/WhatsHappening/";
var output = args.Length > 1 ? args[1] : "screenshot.png";

var edgeUserData = Path.Combine(
    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
    "Microsoft", "Edge", "User Data");
var tempDir = Path.Combine(Path.GetTempPath(), "playwright-edge-profile");

// Copy localStorage and IndexedDB from the Edge profile so we get auth state
CopyDirectory(Path.Combine(edgeUserData, "Default", "Local Storage"),
              Path.Combine(tempDir, "Default", "Local Storage"));
CopyDirectory(Path.Combine(edgeUserData, "Default", "IndexedDB"),
              Path.Combine(tempDir, "Default", "IndexedDB"));

foreach (var file in new[] { "Preferences", "Secure Preferences" })
{
    var src = Path.Combine(edgeUserData, "Default", file);
    if (File.Exists(src))
    {
        Directory.CreateDirectory(Path.Combine(tempDir, "Default"));
        File.Copy(src, Path.Combine(tempDir, "Default", file), overwrite: true);
    }
}

var stateFile = Path.Combine(edgeUserData, "Local State");
if (File.Exists(stateFile))
    File.Copy(stateFile, Path.Combine(tempDir, "Local State"), overwrite: true);

using var playwright = await Playwright.CreateAsync();
await using var context = await playwright.Chromium.LaunchPersistentContextAsync(tempDir, new()
{
    ExecutablePath = @"C:\Program Files (x86)\Microsoft\Edge\Application\msedge.exe",
    Headless = true,
    ViewportSize = new() { Width = 1280, Height = 800 },
    Args = ["--disable-extensions"],
});

var page = await context.NewPageAsync();
await page.GotoAsync(url, new() { WaitUntil = WaitUntilState.NetworkIdle });
await page.WaitForTimeoutAsync(4000);
await page.ScreenshotAsync(new() { Path = output, FullPage = true });

Console.WriteLine($"Screenshot saved to {output}");

static void CopyDirectory(string source, string destination)
{
    if (!Directory.Exists(source)) return;
    Directory.CreateDirectory(destination);
    foreach (var file in Directory.GetFiles(source, "*", SearchOption.AllDirectories))
    {
        var relative = Path.GetRelativePath(source, file);
        var dest = Path.Combine(destination, relative);
        Directory.CreateDirectory(Path.GetDirectoryName(dest)!);
        File.Copy(file, dest, overwrite: true);
    }
}
