using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http;
using Atelier.Web.Application.Auth;
using Atelier.Web.Data.Seed;
using Microsoft.Playwright;
using Xunit;

namespace Atelier.Web.Playwright.Fixtures;

public sealed class AuthenticatedBrowserContext : IAsyncLifetime, IDisposable
{
    private readonly string _databasePath = Path.Combine(Path.GetTempPath(), $"atelier-playwright-{Guid.NewGuid():N}.db");
    private readonly int _port = GetFreePort();
    private readonly List<string> _output = [];
    private Process? _serverProcess;
    private IPlaywright? _playwright;
    private IBrowser? _browser;

    public IBrowserContext Context { get; private set; } = null!;

    public string BaseUrl => $"http://127.0.0.1:{_port}";

    public string? BrowserChannel { get; private set; }

    public bool BrowserInstallRequired { get; private set; }

    public async Task InitializeAsync()
    {
        var workspaceRoot = GetWorkspaceRoot();
        var arguments = $"run --no-build --project \"src/Atelier.Web/Atelier.Web.csproj\" --urls {BaseUrl}";
        var startInfo = new ProcessStartInfo("dotnet", arguments)
        {
            WorkingDirectory = workspaceRoot,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };

        startInfo.Environment["ATELIER_SQLITE_CONNECTION_STRING"] = $"Data Source={_databasePath}";
        startInfo.Environment["ASPNETCORE_ENVIRONMENT"] = "Development";

        _serverProcess = Process.Start(startInfo)
            ?? throw new InvalidOperationException("Failed to start Atelier.Web for Playwright tests.");

        _serverProcess.OutputDataReceived += CaptureOutput;
        _serverProcess.ErrorDataReceived += CaptureOutput;
        _serverProcess.BeginOutputReadLine();
        _serverProcess.BeginErrorReadLine();

        await WaitForServerAsync();

        _playwright = await Microsoft.Playwright.Playwright.CreateAsync();
        try
        {
            _browser = await _playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
            {
                Headless = true,
            });
        }
        catch (PlaywrightException exception) when (exception.Message.Contains("Executable doesn't exist", StringComparison.Ordinal))
        {
            BrowserInstallRequired = true;
            throw new InvalidOperationException(
                "Playwright Chromium browser is not installed for this environment. Run 'pwsh bin/Debug/net10.0/playwright.ps1 install' from tests/Atelier.Web.Playwright.",
                exception);
        }

        Context = await _browser.NewContextAsync(new BrowserNewContextOptions
        {
            BaseURL = BaseUrl,
        });

        await Context.AddCookiesAsync(
        [
            new Cookie
            {
                Name = EnterpriseWeChatAuthenticationHandler.EnterpriseWeChatCookieName,
                Value = SeedData.BuildBlueprint().BootstrapAdminEnterpriseWeChatUserId,
                Domain = "127.0.0.1",
                Path = "/",
                Expires = DateTimeOffset.UtcNow.AddHours(1).ToUnixTimeSeconds(),
                HttpOnly = false,
                Secure = false,
                SameSite = SameSiteAttribute.Lax,
            },
        ]);
    }

    public Task<IPage> NewPageAsync() => Context.NewPageAsync();

    public async Task DisposeAsync()
    {
        if (Context is not null)
        {
            await Context.DisposeAsync();
        }

        if (_browser is not null)
        {
            await _browser.DisposeAsync();
        }

        _playwright?.Dispose();

        if (_serverProcess is { HasExited: false })
        {
            _serverProcess.Kill(entireProcessTree: true);
            await _serverProcess.WaitForExitAsync();
        }

        _serverProcess?.Dispose();
    }

    public void Dispose()
    {
        if (File.Exists(_databasePath))
        {
            try
            {
                File.Delete(_databasePath);
            }
            catch (IOException)
            {
            }
        }
    }

    private async Task WaitForServerAsync()
    {
        using var client = new HttpClient();
        var readyUrl = BaseUrl + "/Auth/WaitingForBinding";

        var deadline = DateTime.UtcNow.AddSeconds(30);

        while (DateTime.UtcNow < deadline)
        {
            if (_serverProcess is { HasExited: true })
            {
                throw new InvalidOperationException($"Atelier.Web exited before becoming ready.{Environment.NewLine}{string.Join(Environment.NewLine, _output)}");
            }

            try
            {
                using var response = await client.GetAsync(readyUrl);
                if ((int)response.StatusCode >= 200 && (int)response.StatusCode < 500)
                {
                    return;
                }
            }
            catch (HttpRequestException)
            {
            }

            await Task.Delay(500);
        }

        throw new TimeoutException($"Timed out waiting for Atelier.Web to start.{Environment.NewLine}{string.Join(Environment.NewLine, _output)}");
    }

    private void CaptureOutput(object sender, DataReceivedEventArgs args)
    {
        if (!string.IsNullOrWhiteSpace(args.Data))
        {
            _output.Add(args.Data);
        }
    }

    private static string GetWorkspaceRoot()
    {
        return Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
    }

    private static int GetFreePort()
    {
        var listener = new System.Net.Sockets.TcpListener(System.Net.IPAddress.Loopback, 0);
        listener.Start();
        var port = ((System.Net.IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
    }
}
