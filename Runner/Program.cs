using System.Diagnostics;

var root = AppContext.BaseDirectory;
var debugDir = new DirectoryInfo(root);
while (debugDir is not null && !File.Exists(Path.Combine(debugDir.FullName, "Data-Technologies-Groep-4.sln")))
{
    debugDir = debugDir.Parent;
}

var workspaceRoot = debugDir?.FullName ?? Directory.GetCurrentDirectory();
var apiProject = Path.Combine(workspaceRoot, "WebShop.Api", "WebShop.Api.csproj");
var desktopProject = Path.Combine(workspaceRoot, "WebShop.Desktop", "WebShop.Desktop.csproj");
var webProject = Path.Combine(workspaceRoot, "WebShop.Web", "WebShop.Web.csproj");

if (!File.Exists(apiProject) || !File.Exists(desktopProject) || !File.Exists(webProject))
{
    Console.Error.WriteLine("Could not locate WebShop.Api, WebShop.Desktop, or WebShop.Web project files from repository root.");
    Console.Error.WriteLine($"Resolved root: {workspaceRoot}");
    return 1;
}

if (args.Contains("--help", StringComparer.OrdinalIgnoreCase) || args.Contains("-h", StringComparer.OrdinalIgnoreCase))
{
    Console.WriteLine("Data-Technologies root launcher");
    Console.WriteLine();
    Console.WriteLine("Usage:");
    Console.WriteLine("  dotnet run");
    Console.WriteLine("    Starts API in background, then starts Website frontend.");
    Console.WriteLine();
    Console.WriteLine("  dotnet run -- --web-only");
    Console.WriteLine("    Starts Website frontend only.");
    Console.WriteLine();
    Console.WriteLine("  dotnet run -- --desktop-only");
    Console.WriteLine("    Starts Desktop frontend only.");
    Console.WriteLine();
    Console.WriteLine("  dotnet run -- --api-only");
    Console.WriteLine("    Starts API only.");
    return 0;
}

var desktopOnly = args.Contains("--desktop-only", StringComparer.OrdinalIgnoreCase);
var webOnly = args.Contains("--web-only", StringComparer.OrdinalIgnoreCase);
var apiOnly = args.Contains("--api-only", StringComparer.OrdinalIgnoreCase);

Process? apiProcess = null;

try
{
    if (!desktopOnly && !webOnly)
    {
        apiProcess = StartDotnetRun(apiProject, workspaceRoot);
        Console.WriteLine("API started.");

        // Give the API a brief moment to bind before opening the desktop UI.
        await Task.Delay(TimeSpan.FromSeconds(2));

        if (apiOnly)
        {
            Console.WriteLine("API-only mode active. Press Ctrl+C to stop.");
            await apiProcess.WaitForExitAsync();
            return apiProcess.ExitCode;
        }
    }

    if (webOnly)
    {
        var webProcessOnly = StartDotnetRun(webProject, workspaceRoot);
        Console.WriteLine("Website started.");
        await webProcessOnly.WaitForExitAsync();
        return webProcessOnly.ExitCode;
    }

    if (desktopOnly)
    {
        var desktopProcessOnly = StartDotnetRun(desktopProject, workspaceRoot);
        Console.WriteLine("Desktop started.");
        await desktopProcessOnly.WaitForExitAsync();
        return desktopProcessOnly.ExitCode;
    }

    var webProcess = StartDotnetRun(webProject, workspaceRoot);
    Console.WriteLine("Website started.");

    await webProcess.WaitForExitAsync();
    return webProcess.ExitCode;
}
finally
{
    if (apiProcess is { HasExited: false })
    {
        try
        {
            apiProcess.Kill(entireProcessTree: true);
            Console.WriteLine("API stopped.");
        }
        catch
        {
            // Best-effort shutdown.
        }
    }
}

static Process StartDotnetRun(string projectPath, string workingDirectory)
{
    var psi = new ProcessStartInfo
    {
        FileName = "dotnet",
        Arguments = $"run --project \"{projectPath}\"",
        WorkingDirectory = workingDirectory,
        UseShellExecute = false,
    };

    var process = Process.Start(psi);
    if (process is null)
    {
        throw new InvalidOperationException($"Failed to start dotnet run for project: {projectPath}");
    }

    return process;
}
