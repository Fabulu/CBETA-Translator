using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace CbetaTranslator.App.Services;

public sealed record GitHubDeviceCode(
    string device_code,
    string user_code,
    string verification_uri,
    int expires_in,
    int interval
);

public sealed record GitHubToken(
    string access_token,
    string token_type,
    string scope
);

public interface IGitHubAuthService
{
    Task<GitHubToken?> AuthorizeDeviceFlowAsync(IProgress<string> log, CancellationToken ct);
}

public sealed class GitHubAuthService : IGitHubAuthService
{
    // NOTE: OAuth "Client ID" is PUBLIC by design. It's okay to ship it in the app.
    // Secret must NEVER be in the app. Device flow does not require the secret.
    private const string ClientId = "Ov23liMZASjsTTz3f2AX";

    // For public repos:
    // - public_repo -> create forks, push to your fork (via git creds), create PRs
    // - read:user   -> read username via GET /user
    private const string Scope = "public_repo read:user";

    private static readonly Uri DeviceCodeUri = new("https://github.com/login/device/code");
    private static readonly Uri TokenUri = new("https://github.com/login/oauth/access_token");

    private readonly HttpClient _http;

    public GitHubAuthService()
    {
        _http = new HttpClient();
        _http.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        _http.DefaultRequestHeaders.UserAgent.ParseAdd("CbetaTranslator-App");
    }

    public async Task<GitHubToken?> AuthorizeDeviceFlowAsync(IProgress<string> log, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(ClientId))
        {
            log.Report("[error] GitHub OAuth ClientId missing.");
            return null;
        }

        // 1) Request device code
        log.Report("[auth] requesting device code…");

        var deviceReq = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string,string>("client_id", ClientId),
            new KeyValuePair<string,string>("scope", Scope),
        });

        using var deviceResp = await _http.PostAsync(DeviceCodeUri, deviceReq, ct);
        var deviceJson = await deviceResp.Content.ReadAsStringAsync(ct);

        if (!deviceResp.IsSuccessStatusCode)
        {
            log.Report("[error] device code request failed: " + deviceResp.StatusCode);
            log.Report(deviceJson);
            return null;
        }

        var device = JsonSerializer.Deserialize<GitHubDeviceCode>(deviceJson);
        if (device == null || string.IsNullOrWhiteSpace(device.device_code))
        {
            log.Report("[error] device code parse failed");
            log.Report(deviceJson);
            return null;
        }

        log.Report($"[auth] Go to: {device.verification_uri}");
        log.Report($"[auth] Enter code: {device.user_code}");
        log.Report("[auth] Opening browser…");

        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = device.verification_uri,
                UseShellExecute = true
            });
        }
        catch
        {
            // user can open manually
        }

        // 2) Poll for token
        var pollInterval = Math.Max(device.interval, 2);
        var deadline = DateTime.UtcNow.AddSeconds(device.expires_in);

        log.Report("[auth] waiting for authorization…");

        while (DateTime.UtcNow < deadline)
        {
            ct.ThrowIfCancellationRequested();

            await Task.Delay(TimeSpan.FromSeconds(pollInterval), ct);

            var tokenReq = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string,string>("client_id", ClientId),
                new KeyValuePair<string,string>("device_code", device.device_code),
                new KeyValuePair<string,string>("grant_type", "urn:ietf:params:oauth:grant-type:device_code"),
            });

            using var tokenResp = await _http.PostAsync(TokenUri, tokenReq, ct);
            var tokenJson = await tokenResp.Content.ReadAsStringAsync(ct);

            if (!tokenResp.IsSuccessStatusCode)
            {
                log.Report("[warn] token poll failed: " + tokenResp.StatusCode);
                log.Report(tokenJson);
                continue;
            }

            using var doc = JsonDocument.Parse(tokenJson);

            if (doc.RootElement.TryGetProperty("access_token", out _))
            {
                var token = JsonSerializer.Deserialize<GitHubToken>(tokenJson);
                if (token == null || string.IsNullOrWhiteSpace(token.access_token))
                {
                    log.Report("[error] token parse failed");
                    log.Report(tokenJson);
                    return null;
                }

                log.Report("[auth] success.");
                return token;
            }

            if (doc.RootElement.TryGetProperty("error", out var err))
            {
                var e = err.GetString() ?? "";

                if (e == "authorization_pending")
                    continue;

                if (e == "slow_down")
                {
                    pollInterval += 5;
                    continue;
                }

                log.Report("[error] auth failed: " + e);
                log.Report(tokenJson);
                return null;
            }

            log.Report("[warn] unexpected token response:");
            log.Report(tokenJson);
        }

        log.Report("[error] authorization timed out.");
        return null;
    }
}
