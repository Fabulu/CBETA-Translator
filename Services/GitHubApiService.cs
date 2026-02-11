using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace CbetaTranslator.App.Services;

public sealed record GitHubUser(string login);

public sealed record GitHubRepo(string name, string full_name, bool fork, string default_branch);

public sealed record GitHubPullRequestResponse(string html_url);

public interface IGitHubApiService
{
    Task<GitHubUser?> GetMeAsync(string accessToken, CancellationToken ct);

    Task<bool> ForkExistsAsync(string accessToken, string owner, string repo, CancellationToken ct);
    Task<bool> CreateForkAsync(string accessToken, string upstreamOwner, string upstreamRepo, CancellationToken ct);
    Task<bool> WaitForForkAsync(string accessToken, string owner, string repo, TimeSpan timeout, IProgress<string> log, CancellationToken ct);

    Task<string?> CreatePullRequestAsync(
        string accessToken,
        string upstreamOwner,
        string upstreamRepo,
        string head,
        string baseBranch,
        string title,
        string body,
        CancellationToken ct);
}

public sealed class GitHubApiService : IGitHubApiService
{
    private readonly HttpClient _http;

    public GitHubApiService()
    {
        _http = new HttpClient
        {
            BaseAddress = new Uri("https://api.github.com/")
        };

        _http.DefaultRequestHeaders.UserAgent.ParseAdd("CbetaTranslator-App");
        _http.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
    }

    private void SetAuth(string token)
    {
        _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Ensure we don't accumulate duplicates
        if (_http.DefaultRequestHeaders.Contains("X-GitHub-Api-Version"))
            _http.DefaultRequestHeaders.Remove("X-GitHub-Api-Version");

        _http.DefaultRequestHeaders.Add("X-GitHub-Api-Version", "2022-11-28");
    }

    private static string SafeBody(string? s, int max = 4000)
    {
        s ??= "";
        s = s.Replace("\r", "").Trim();
        return s.Length <= max ? s : s.Substring(0, max) + "\n…(truncated)…";
    }

    public async Task<GitHubUser?> GetMeAsync(string accessToken, CancellationToken ct)
    {
        SetAuth(accessToken);

        using var resp = await _http.GetAsync("user", ct);
        if (!resp.IsSuccessStatusCode) return null;

        var json = await resp.Content.ReadAsStringAsync(ct);
        return JsonSerializer.Deserialize<GitHubUser>(json);
    }

    public async Task<bool> ForkExistsAsync(string accessToken, string owner, string repo, CancellationToken ct)
    {
        SetAuth(accessToken);

        using var resp = await _http.GetAsync($"repos/{Uri.EscapeDataString(owner)}/{Uri.EscapeDataString(repo)}", ct);
        if (resp.StatusCode == HttpStatusCode.NotFound) return false;
        return resp.IsSuccessStatusCode;
    }

    public async Task<bool> CreateForkAsync(string accessToken, string upstreamOwner, string upstreamRepo, CancellationToken ct)
    {
        SetAuth(accessToken);

        using var resp = await _http.PostAsync(
            $"repos/{Uri.EscapeDataString(upstreamOwner)}/{Uri.EscapeDataString(upstreamRepo)}/forks",
            new StringContent("{}", Encoding.UTF8, "application/json"),
            ct);

        return resp.IsSuccessStatusCode || resp.StatusCode == HttpStatusCode.Accepted;
    }

    public async Task<bool> WaitForForkAsync(string accessToken, string owner, string repo, TimeSpan timeout, IProgress<string> log, CancellationToken ct)
    {
        var until = DateTime.UtcNow.Add(timeout);
        while (DateTime.UtcNow < until)
        {
            ct.ThrowIfCancellationRequested();

            if (await ForkExistsAsync(accessToken, owner, repo, ct))
                return true;

            log.Report("[auth] waiting for fork to be created…");
            await Task.Delay(2000, ct);
        }

        return false;
    }

    public async Task<string?> CreatePullRequestAsync(
        string accessToken,
        string upstreamOwner,
        string upstreamRepo,
        string head,
        string baseBranch,
        string title,
        string body,
        CancellationToken ct)
    {
        SetAuth(accessToken);

        var payload = new
        {
            title = title ?? "",
            body = body ?? "",
            head = head ?? "",
            @base = baseBranch ?? "main"
        };

        var json = JsonSerializer.Serialize(payload);

        using var resp = await _http.PostAsync(
            $"repos/{Uri.EscapeDataString(upstreamOwner)}/{Uri.EscapeDataString(upstreamRepo)}/pulls",
            new StringContent(json, Encoding.UTF8, "application/json"),
            ct);

        var respJson = await resp.Content.ReadAsStringAsync(ct);

        if (!resp.IsSuccessStatusCode)
        {
            // DO NOT return null silently. Return a human-readable error string as "null"
            // isn't usable by the UI logger. We throw and let GitTabView catch and print it.
            throw new Exception(
                "GitHub PR create failed: " +
                $"{(int)resp.StatusCode} {resp.ReasonPhrase}\n" +
                SafeBody(respJson));
        }

        // Parse robustly: sometimes fields are missing or casing differs.
        try
        {
            using var doc = JsonDocument.Parse(respJson);
            if (doc.RootElement.TryGetProperty("html_url", out var urlEl))
            {
                var url = urlEl.GetString();
                if (!string.IsNullOrWhiteSpace(url))
                    return url;
            }
        }
        catch
        {
            // fallback to typed deserialize
        }

        var pr = JsonSerializer.Deserialize<GitHubPullRequestResponse>(respJson);
        return pr?.html_url;
    }
}
