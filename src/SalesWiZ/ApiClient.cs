using System.Net;
using System.Text;

namespace SalesWiZ;

public sealed class ApiException : Exception
{
    public ApiException(string message) : base(message) { }
}

public sealed class UnauthorizedException : ApiException
{
    public UnauthorizedException() : base("Your session expired. Please log in again.") { }
}

/// <summary>
/// Talks to the live SalesWiZ CRM at https://crm.129-146-5-140.sslip.io/.
/// Auth is the shared CRM password: POST it to login.php once, and the
/// session cookie authenticates every API call after that. The password is
/// kept in the Windows Credential Locker so the app can re-log in silently
/// when the server session expires (401) — the user only types it once.
/// The CRM API token is NEVER used here and must never appear in this app.
/// </summary>
public sealed class ApiClient
{
    public static readonly Uri BaseUri = new("https://crm.129-146-5-140.sslip.io");

    public bool LoggedIn { get; private set; }
    public bool IsAuthenticating { get; private set; }
    public string? AuthError { get; private set; }

    private HttpClient _http;

    public ApiClient()
    {
        _http = CreateClient();
    }

    private static HttpClient CreateClient()
    {
        var handler = new HttpClientHandler
        {
            CookieContainer = new CookieContainer(),
            AutomaticDecompression = DecompressionMethods.All,
        };
        var client = new HttpClient(handler) { BaseAddress = BaseUri };
        client.DefaultRequestHeaders.UserAgent.ParseAdd("SalesWiZ-Windows/1.1");
        return client;
    }

    /// <summary>Drop every cookie (fresh session).</summary>
    private void ResetClient()
    {
        _http.Dispose();
        _http = CreateClient();
    }

    // MARK: Login / logout

    /// <summary>
    /// POST the password to login.php, then prove the session works with a
    /// dashboard call. On success the password is saved to the Credential
    /// Locker.
    /// </summary>
    public async Task<bool> LoginAsync(string password)
    {
        IsAuthenticating = true;
        AuthError = null;
        try
        {
            if (!await PostLoginAsync(password)) return Fail("Wrong password.");
            if (!await VerifySessionAsync()) return Fail("Wrong password.");
            CredentialStore.SavePassword(password);
            LoggedIn = true;
            return true;
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            return Fail(ex.Message);
        }
        finally
        {
            IsAuthenticating = false;
        }

        bool Fail(string message)
        {
            AuthError = message;
            return false;
        }
    }

    /// <summary>Try the saved password without asking the user.</summary>
    public async Task<bool> TrySilentLoginAsync()
    {
        var password = CredentialStore.ReadPassword();
        if (string.IsNullOrEmpty(password)) return false;
        try
        {
            if (!await PostLoginAsync(password)) return false;
            if (!await VerifySessionAsync()) return false;
            LoggedIn = true;
            return true;
        }
        catch
        {
            return false;
        }
    }

    public void Logout()
    {
        CredentialStore.DeletePassword();
        ResetClient();
        LoggedIn = false;
    }

    private async Task<bool> PostLoginAsync(string password)
    {
        using var content = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("password", password),
        });
        // The login POST 302-redirects on success; what matters is the cookie
        // it sets, not the redirect target, so the result is ignored here and
        // verified with a dashboard call instead.
        using var _ = await _http.PostAsync("login.php", content);
        return true;
    }

    private async Task<bool> VerifySessionAsync()
    {
        try
        {
            var dashboard = await GetAsync<Dashboard>("dashboard", allowRetry: false);
            return dashboard.Total is not null;
        }
        catch
        {
            return false;
        }
    }

    // MARK: Reads

    public Task<Dashboard> FetchDashboardAsync() => GetAsync<Dashboard>("dashboard");

    public Task<LeadsResponse> FetchLeadsAsync(string search, string? industry, string? status,
        int limit = 100, int offset = 0)
    {
        var p = new Dictionary<string, string>
        {
            ["limit"] = limit.ToString(),
            ["offset"] = offset.ToString(),
        };
        if (!string.IsNullOrEmpty(search)) p["q"] = search;
        if (!string.IsNullOrEmpty(industry)) p["industry"] = industry;
        if (!string.IsNullOrEmpty(status)) p["status"] = status;
        return GetAsync<LeadsResponse>("leads", p);
    }

    public async Task<Lead> FetchLeadAsync(int id)
    {
        var response = await GetAsync<LeadResponse>("lead", new Dictionary<string, string>
        {
            ["id"] = id.ToString(),
        });
        return response.Lead;
    }

    public Task<ActionsList> FetchActionsAsync() => GetAsync<ActionsList>("actions");

    public Task<EmailsResponse> FetchEmailsAsync(int? leadId = null, int limit = 50, int offset = 0)
    {
        var p = new Dictionary<string, string>
        {
            ["limit"] = limit.ToString(),
            ["offset"] = offset.ToString(),
        };
        if (leadId is not null) p["lead_id"] = leadId.Value.ToString();
        return GetAsync<EmailsResponse>("list_emails", p);
    }

    public Task<InterestedLeadsResponse> FetchInterestedLeadsAsync() =>
        GetAsync<InterestedLeadsResponse>("interested_leads");

    // MARK: Writes

    public async Task<Lead> LogCallAsync(int leadId, string outcome, string? note, string? followUpDue)
    {
        var body = new Dictionary<string, string>
        {
            ["id"] = leadId.ToString(),
            ["outcome"] = outcome,
        };
        if (!string.IsNullOrEmpty(note)) body["note"] = note;
        if (followUpDue is not null) body["follow_up_due"] = followUpDue;
        var response = await PostAsync<LeadResponse>("log_call", body);
        return response.Lead;
    }

    public async Task<Lead> LogReplyAsync(int leadId, string? note)
    {
        var body = new Dictionary<string, string> { ["id"] = leadId.ToString() };
        if (!string.IsNullOrEmpty(note)) body["note"] = note;
        var response = await PostAsync<LeadResponse>("log_reply", body);
        return response.Lead;
    }

    public async Task<Lead> RecordBounceAsync(int leadId, string? reason)
    {
        var body = new Dictionary<string, string> { ["id"] = leadId.ToString() };
        if (!string.IsNullOrEmpty(reason)) body["reason"] = reason;
        var response = await PostAsync<LeadResponse>("record_bounce", body);
        return response.Lead;
    }

    public async Task<Lead> UpdateLeadStatusAsync(int leadId, string status)
    {
        var body = new Dictionary<string, string>
        {
            ["id"] = leadId.ToString(),
            ["status"] = status,
        };
        var response = await PostAsync<LeadResponse>("update_lead", body);
        return response.Lead;
    }

    public async Task<Lead> AddLeadAsync(string name, string? industry, string? city, string? phone,
        string? email, string? contactPerson, string? notes)
    {
        var body = new Dictionary<string, string> { ["name"] = name };
        if (!string.IsNullOrEmpty(industry)) body["industry"] = industry;
        if (!string.IsNullOrEmpty(city)) body["city"] = city;
        if (!string.IsNullOrEmpty(phone)) body["phone"] = phone;
        if (!string.IsNullOrEmpty(email)) body["email"] = email;
        if (!string.IsNullOrEmpty(contactPerson)) body["contact_person"] = contactPerson;
        if (!string.IsNullOrEmpty(notes)) body["notes"] = notes;
        var response = await PostAsync<LeadResponse>("add_lead", body);
        return response.Lead;
    }

    public Task<QueueBatchResult> QueueBatchAsync() =>
        PostAsync<QueueBatchResult>("queue_batch", new Dictionary<string, string>());

    /// <summary>
    /// Queue an outbound email. It only sends after Matt approves the send
    /// (the bridge sends queued rows one approved row at a time). Returns
    /// the queued row. Pass <paramref name="inReplyTo"/> (the Gmail id of the
    /// message being answered) to inherit that message's lead when
    /// <paramref name="leadId"/> is null.
    /// </summary>
    public async Task<EmailMessage> QueueEmailAsync(int? leadId, string to, string subject, string body,
        string? inReplyTo)
    {
        var payload = new Dictionary<string, string>
        {
            ["to"] = to,
            ["subject"] = subject,
            ["body"] = body,
        };
        if (leadId is not null) payload["lead_id"] = leadId.Value.ToString();
        if (!string.IsNullOrEmpty(inReplyTo)) payload["in_reply_to"] = inReplyTo;
        var response = await PostAsync<QueuedEmailResponse>("queue_email", payload);
        return response.Email;
    }

    // MARK: Transport

    private string ApiUrl(string action, Dictionary<string, string>? parameters = null)
    {
        var sb = new StringBuilder("api.php?action=").Append(Uri.EscapeDataString(action));
        if (parameters is not null)
        {
            foreach (var kv in parameters)
            {
                sb.Append('&')
                  .Append(Uri.EscapeDataString(kv.Key))
                  .Append('=')
                  .Append(Uri.EscapeDataString(kv.Value));
            }
        }
        return sb.ToString();
    }

    private async Task<T> GetAsync<T>(string action,
        Dictionary<string, string>? parameters = null, bool allowRetry = true)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, ApiUrl(action, parameters));
        return await SendAsync<T>(request, allowRetry);
    }

    private async Task<T> PostAsync<T>(string action, Dictionary<string, string> body,
        bool allowRetry = true)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, ApiUrl(action))
        {
            Content = new StringContent(CrmJson.Serialize(body), Encoding.UTF8, "application/json"),
        };
        return await SendAsync<T>(request, allowRetry);
    }

    private async Task<T> SendAsync<T>(HttpRequestMessage request, bool allowRetry)
    {
        HttpResponseMessage response;
        try
        {
            response = await _http.SendAsync(request);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            throw new ApiException(ex.Message);
        }

        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            if (allowRetry && await TrySilentLoginAsync())
            {
                // Rebuild the request (it was already sent) and retry once.
                using var retry = await CloneRequestAsync(request);
                return await SendAsync<T>(retry, allowRetry: false);
            }
            LoggedIn = false;
            throw new UnauthorizedException();
        }

        var json = await response.Content.ReadAsStringAsync();
        Envelope? envelope;
        try
        {
            envelope = CrmJson.Deserialize<Envelope>(json);
        }
        catch
        {
            throw new ApiException("The server sent something unexpected. Please try again.");
        }
        if (envelope is null)
        {
            throw new ApiException("The server sent something unexpected. Please try again.");
        }
        if (!envelope.Ok)
        {
            throw new ApiException(envelope.Error ?? "The server declined the request.");
        }
        try
        {
            return CrmJson.Deserialize<T>(json)
                ?? throw new ApiException("The server sent something unexpected. Please try again.");
        }
        catch (ApiException)
        {
            throw;
        }
        catch
        {
            throw new ApiException("The server sent something unexpected. Please try again.");
        }
    }

    private static async Task<HttpRequestMessage> CloneRequestAsync(HttpRequestMessage request)
    {
        var clone = new HttpRequestMessage(request.Method, request.RequestUri);
        foreach (var header in request.Headers)
        {
            clone.Headers.TryAddWithoutValidation(header.Key, header.Value);
        }
        if (request.Content is not null)
        {
            var body = await request.Content.ReadAsStringAsync();
            clone.Content = new StringContent(body, Encoding.UTF8,
                request.Content.Headers.ContentType?.MediaType ?? "application/json");
        }
        return clone;
    }
}
