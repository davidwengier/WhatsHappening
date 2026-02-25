using Microsoft.Playwright;

var url = args.Length > 0 ? args[0] : "https://wengier.com/WhatsHappening/";
var output = args.Length > 1 ? args[1] : "screenshot.png";
var viewportWidth = args.Length > 2 ? int.Parse(args[2]) : 1280;
var viewportHeight = args.Length > 3 ? int.Parse(args[3]) : 800;

var edgeUserData = Path.Combine(
    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
    "Microsoft", "Edge", "User Data");
var tempDir = Path.Combine(Path.GetTempPath(), "playwright-edge-profile");

// Clean stale temp profile to avoid corrupted state
if (Directory.Exists(tempDir))
    Directory.Delete(tempDir, recursive: true);

// Copy the full Default profile (skip lock files and large caches)
var skipDirs = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    { "Cache", "Code Cache", "GPUCache", "Service Worker", "DawnGraphiteCache",
      "DawnWebGPUBlobCache", "GrShaderCache", "ShaderCache", "component_crx_cache" };
CopyDirectory(Path.Combine(edgeUserData, "Default"),
              Path.Combine(tempDir, "Default"), skipDirs);

var stateFile = Path.Combine(edgeUserData, "Local State");
if (File.Exists(stateFile))
    File.Copy(stateFile, Path.Combine(tempDir, "Local State"), overwrite: true);

using var playwright = await Playwright.CreateAsync();
await using var context = await playwright.Chromium.LaunchPersistentContextAsync(tempDir, new()
{
    ExecutablePath = @"C:\Program Files (x86)\Microsoft\Edge\Application\msedge.exe",
    Headless = true,
    ViewportSize = new() { Width = viewportWidth, Height = viewportHeight },
    Args = ["--disable-extensions"],
});

var page = await context.NewPageAsync();
await page.GotoAsync(url, new() { WaitUntil = WaitUntilState.DOMContentLoaded });
await page.WaitForTimeoutAsync(8000);
await page.ScreenshotAsync(new() { Path = output, FullPage = true });

Console.WriteLine($"Screenshot saved to {output}");

static void CopyDirectory(string source, string destination, HashSet<string>? skipDirs = null)
{
    if (!Directory.Exists(source)) return;
    Directory.CreateDirectory(destination);

    foreach (var file in Directory.GetFiles(source))
    {
        var name = Path.GetFileName(file);
        if (name.Equals("LOCK", StringComparison.OrdinalIgnoreCase)) continue;
        try { File.Copy(file, Path.Combine(destination, name), overwrite: true); }
        catch (IOException) { /* skip locked files */ }
    }

    foreach (var dir in Directory.GetDirectories(source))
    {
        var name = Path.GetFileName(dir);
        if (skipDirs?.Contains(name) == true) continue;
        CopyDirectory(dir, Path.Combine(destination, name), skipDirs: null);
    }
}
