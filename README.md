# SalesWiZ for Windows

Native Windows desktop app for the SalesWiZ CRM (Riley AI Consulting) —
WinUI 3 / Windows App SDK, C#, .NET 8. Same data as the web CRM and the
iPhone app, same login, no web views.

**Live CRM:** https://crm.129-146-5-140.sslip.io/
**API docs:** `../riley-crm/API.md` (in this workspace)

## Features (parity with the iPhone app v1.1)

- **Login** — shared CRM password (POST `login.php`, session cookie).
  Password is saved in the Windows Credential Locker so the app can re-log
  in silently when the session expires (401). If the Locker is unavailable
  it falls back to a DPAPI-encrypted file under
  `%LOCALAPPDATA%\SalesWiZ\`. The CRM API token is **never** used and must
  never appear in this app, the repo, or the binary.
- **Dashboard** — funnel counts (leads, sent, delivered, replies,
  interested, demos, customers, bounced, not contacted), money (setup
  collected, MRR, price), today (calls left, follow-ups due, call queue,
  email leads left), per-industry stats, and the progress bar / stage /
  ETA to first customer (with the stall warning).
- **Leads** — search, industry/status filters, pagination, lead detail
  (contact, status change, log call, log reply, mark bounced, history,
  email thread), add a lead.
- **Inbox** — `list_emails`, newest first, In/Queued/Sent/Failed badges,
  lead name. Tap a message for the thread view; reply (queues via
  `queue_email` with `in_reply_to`, "Re: …" subject, quoted original) or
  compose (type an address or pick a lead). The button says **Queue** —
  queued mail only sends after Matt approves it.
- **Interest** — `interested_leads`, newest first (business, contact ·
  city, reply snippet, "Replied <date>"); tap for the full lead + thread.
- **Actions** — follow-ups due (with due text), call queue (with a Call
  button), next batch, calls left today, and "Queue Next Batch of 20".

## Run it (once built)

1. Download the `SalesWiZ-windows` artifact from the GitHub Actions run.
2. Unzip it anywhere on your PC.
3. Run `SalesWiZ.exe`. No install step, no admin rights needed.
4. Log in once with the shared CRM password — it saves it and stays
   logged in.

## Build it

GitHub Actions (the normal route — see `.github/workflows/build-windows.yml`):
push to `main` and the `SalesWiZ-windows` artifact appears. Locally, on a
Windows machine with .NET 8 SDK:

```
dotnet workload install windows
dotnet publish src/SalesWiZ/SalesWiZ.csproj -c Release -r win-x64 --self-contained -o publish
```

The build cannot run on Linux/macOS (no Windows toolchain) — the Actions
run is what proves it compiles.

## Icon

**Temporary placeholder only.** The tiles and exe icon are a plain navy
square with a white "W" and a copper rule (matching the iPhone app's
drawn mark), generated in `src/SalesWiZ/Assets/`. Matt is still picking
the universal SalesWiZ icon — once he picks it, replace the files in
`Assets/` (`Square150x150Logo.png`, `Square44x44Logo.png`,
`Wide310x150Logo.png`, `StoreLogo.png`, `SplashScreen.png`,
`SalesWiZ.ico`) and rebuild. Do not treat the placeholder as final.

## Project layout

```
SalesWiZ.sln
src/SalesWiZ/
  SalesWiZ.csproj            WinUI 3, .NET 8, unpackaged, self-contained
  Package.appxmanifest       Identity com.philbert.saleswiz, internetClient
  app.manifest               DPI-aware, long paths
  App.xaml / App.xaml.cs     App + shared ApiClient
  MainWindow.xaml(.cs)       NavigationView shell + login layer
  Models.cs                  CRM JSON models (flexible number/string decoding)
  ApiClient.cs               HttpClient + cookie jar, login, silent re-login
  CredentialStore.cs         Credential Locker (+ DPAPI fallback)
  Helpers.cs                 Dates, due text, status colors
  Views/
    LoginPage                Password login
    DashboardPage            Funnel, money, today, by industry, ETA, progress
    LeadsPage                Search, filters, pagination, add lead
    LeadDetailPage           Contact, status, log call/reply/bounce, history, emails
    InboxPage                Email list with badges
    EmailDetailPage          Message + thread + reply
    InterestPage             Interested leads
    ActionsPage              Follow-ups, call queue, next batch
    LogCallDialog / LogReplyDialog / ReplyDialog / ComposeDialog
    AddLeadDialog / LeadPickerDialog
  Assets/                    Placeholder icons (see Icon above)
.github/workflows/
  build-windows.yml          windows-latest: restore, publish, zip, artifact
```

## Untested

- **Compile**: no Windows toolchain in this sandbox — the first Actions
  run is what proves the code compiles (same as the iPhone app's first
  build, which caught a real Swift error).
- **On-PC**: login on the PC, Credential Locker persistence, silent
  re-login, every tab, reply/compose queueing, tel: links. Nothing is
  claimed to work on Matt's PC until he runs the exe.
- **MSIX**: not built — it needs a signing certificate. The zip + exe is
  the unsigned route for now.

