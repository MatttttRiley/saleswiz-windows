using System.Text.Json;
using System.Text.Json.Serialization;

namespace SalesWiZ;

/// <summary>
/// JSON models for the SalesWiZ CRM API (https://crm.129-146-5-140.sslip.io/).
/// Shapes mirror the iPhone app's Models.swift and ~/workspace/riley-crm/API.md.
/// The PHP API returns SQLite rows as JSON: numbers as numbers, empty fields
/// as null — the flexible converters below tolerate either shape.
/// </summary>
public static class CrmJson
{
    public static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = false,
        NumberHandling = JsonNumberHandling.AllowReadingFromString,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    public static T? Deserialize<T>(string json) => JsonSerializer.Deserialize<T>(json, Options);
    public static string Serialize<T>(T value) => JsonSerializer.Serialize(value, Options);
}

/// <summary>Reads true/false, 1/0, or "1"/"0" as a bool.</summary>
public sealed class FlexibleBoolConverter : JsonConverter<bool>
{
    public override bool Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return reader.TokenType switch
        {
            JsonTokenType.True => true,
            JsonTokenType.False => false,
            JsonTokenType.Number => reader.GetInt32() != 0,
            JsonTokenType.String => reader.GetString() is "1" or "true" or "True",
            _ => false,
        };
    }

    public override void Write(Utf8JsonWriter writer, bool value, JsonSerializerOptions options) =>
        writer.WriteBooleanValue(value);
}

public sealed class Envelope
{
    [JsonPropertyName("ok")] public bool Ok { get; set; }
    [JsonPropertyName("error")] public string? Error { get; set; }
}

// MARK: - Lead

public sealed class HistoryEvent
{
    [JsonPropertyName("id")] public int Id { get; set; }
    [JsonPropertyName("lead_id")] public int? LeadId { get; set; }
    [JsonPropertyName("kind")] public string Kind { get; set; } = "note";
    [JsonPropertyName("event_date")] public string? EventDate { get; set; }
    [JsonPropertyName("detail")] public string Detail { get; set; } = "";
    [JsonPropertyName("created_at")] public string? CreatedAt { get; set; }
}

public sealed class Lead
{
    [JsonPropertyName("id")] public int Id { get; set; }
    [JsonPropertyName("name")] public string Name { get; set; } = "";
    [JsonPropertyName("industry")] public string? Industry { get; set; }
    [JsonPropertyName("city")] public string? City { get; set; }
    [JsonPropertyName("phone")] public string? Phone { get; set; }
    [JsonPropertyName("email")] public string? Email { get; set; }
    [JsonPropertyName("contact_person")] public string? ContactPerson { get; set; }
    [JsonPropertyName("source")] public string? Source { get; set; }
    [JsonPropertyName("status")] public string Status { get; set; } = "not_contacted";
    [JsonPropertyName("date_emailed")] public string? DateEmailed { get; set; }
    [JsonPropertyName("date_called")] public string? DateCalled { get; set; }
    [JsonPropertyName("call_outcome")] public string? CallOutcome { get; set; }
    [JsonPropertyName("date_replied")] public string? DateReplied { get; set; }
    [JsonPropertyName("notes")] public string? Notes { get; set; }
    [JsonPropertyName("follow_up_due")] public string? FollowUpDue { get; set; }
    [JsonPropertyName("do_not_call")]
    [JsonConverter(typeof(FlexibleBoolConverter))]
    public bool DoNotCall { get; set; }
    [JsonPropertyName("do_not_contact")]
    [JsonConverter(typeof(FlexibleBoolConverter))]
    public bool DoNotContact { get; set; }
    [JsonPropertyName("bounce_reason")] public string? BounceReason { get; set; }
    [JsonPropertyName("date_bounced")] public string? DateBounced { get; set; }
    [JsonPropertyName("services_bought")] public string? ServicesBought { get; set; }
    [JsonPropertyName("grok_setup_status")] public string? GrokSetupStatus { get; set; }
    [JsonPropertyName("support_notes")] public string? SupportNotes { get; set; }
    [JsonPropertyName("billing_renewal_date")] public string? BillingRenewalDate { get; set; }
    [JsonPropertyName("in_call_queue")]
    [JsonConverter(typeof(FlexibleBoolConverter))]
    public bool InCallQueue { get; set; }
    [JsonPropertyName("queued_batch")] public int? QueuedBatch { get; set; }
    [JsonPropertyName("reply_snippet")] public string? ReplySnippet { get; set; }
    [JsonPropertyName("history")] public List<HistoryEvent>? History { get; set; }

    public string DisplayCity =>
        string.Join(" · ", new[] { City, Industry }.Where(s => !string.IsNullOrEmpty(s)));

    public string? TelUri
    {
        get
        {
            if (string.IsNullOrEmpty(Phone)) return null;
            var digits = new string(Phone.Where(char.IsDigit).ToArray());
            if (digits.Length == 0) return null;
            if (digits.Length == 10) digits = "1" + digits;
            return "tel:+" + digits;
        }
    }
}

public static class LeadStatusInfo
{
    public static readonly string[] All =
    [
        "not_contacted", "emailed", "called", "replied", "interested",
        "demo_booked", "customer", "lost", "do_not_contact", "bounced",
    ];

    /// Statuses a lead can be moved to by hand.
    public static readonly string[] Settable =
    [
        "not_contacted", "emailed", "called", "replied", "interested",
        "demo_booked", "customer", "lost", "do_not_contact",
    ];

    public static string Label(string status) => status switch
    {
        "not_contacted" => "Not contacted",
        "emailed" => "Emailed",
        "called" => "Called",
        "replied" => "Replied",
        "interested" => "Interested",
        "demo_booked" => "Demo booked",
        "customer" => "Customer",
        "lost" => "Lost",
        "do_not_contact" => "Do not contact",
        "bounced" => "Bounced",
        _ => status,
    };

    /// Accent color per status (hex), matching the iPhone app.
    public static string ColorHex(string status) => status switch
    {
        "emailed" => "#3B82F6",
        "called" => "#F59E0B",
        "replied" => "#8B5CF6",
        "interested" => "#14B8A6",
        "demo_booked" => "#6366F1",
        "customer" => "#22C55E",
        "lost" => "#EF4444",
        "bounced" => "#EF4444",
        _ => "#6B7280",
    };
}

public static class IndustryInfo
{
    public static readonly string[] All = ["plumber", "electrician", "real estate", "insurance", "used car"];

    public static string Label(string? industry) =>
        string.IsNullOrEmpty(industry)
            ? "Business"
            : char.ToUpperInvariant(industry[0]) + industry[1..];

    /// One-letter mark shown in a colored circle (no icon font dependency).
    public static string Initial(string? industry) => industry switch
    {
        "plumber" => "P",
        "electrician" => "E",
        "real estate" => "R",
        "insurance" => "I",
        "used car" => "C",
        _ => "B",
    };
}

// MARK: - Dashboard

public sealed class Money
{
    [JsonPropertyName("setup_collected")] public int? SetupCollected { get; set; }
    [JsonPropertyName("mrr")] public int? Mrr { get; set; }
    [JsonPropertyName("setup_fee")] public int? SetupFee { get; set; }
    [JsonPropertyName("monthly_fee")] public int? MonthlyFee { get; set; }
}

public sealed class StageRate
{
    [JsonPropertyName("lo")] public double? Lo { get; set; }
    [JsonPropertyName("mid")] public double? Mid { get; set; }
    [JsonPropertyName("hi")] public double? Hi { get; set; }
    [JsonPropertyName("measured")] public bool? Measured { get; set; }
    [JsonPropertyName("n")] public int? N { get; set; }

    public string PercentText => Mid is null ? "—" : $"{Mid.Value * 100:0.0}%";
}

public sealed class DayRange
{
    [JsonPropertyName("lo")] public int? Lo { get; set; }
    [JsonPropertyName("mid")] public int? Mid { get; set; }
    [JsonPropertyName("hi")] public int? Hi { get; set; }

    public string Text => Lo is null || Hi is null ? "—" : $"{Lo}–{Hi} days";
}

public sealed class Eta
{
    [JsonPropertyName("low_days")] public int? LowDays { get; set; }
    [JsonPropertyName("mid_days")] public int? MidDays { get; set; }
    [JsonPropertyName("high_days")] public int? HighDays { get; set; }
    [JsonPropertyName("email_days")] public DayRange? EmailDays { get; set; }
    [JsonPropertyName("call_days")] public DayRange? CallDays { get; set; }
    [JsonPropertyName("email_expected_sales")] public double? EmailExpectedSales { get; set; }
    [JsonPropertyName("call_expected_sales")] public double? CallExpectedSales { get; set; }
    [JsonPropertyName("stalled")] public bool? Stalled { get; set; }
    [JsonPropertyName("emails_per_day")] public int? EmailsPerDay { get; set; }
    [JsonPropertyName("calls_per_day")] public int? CallsPerDay { get; set; }

    public string RangeText => LowDays is null || HighDays is null ? "—" : $"{LowDays}–{HighDays} days";
    public string LikelyText => MidDays is null ? "—" : $"most likely ~{MidDays} days";
}

public sealed class ProgressInfo
{
    [JsonPropertyName("percent")] public int? Percent { get; set; }
    [JsonPropertyName("stage")] public string? Stage { get; set; }
    [JsonPropertyName("outreach_frac")] public double? OutreachFrac { get; set; }
    [JsonPropertyName("outreach_done")] public int? OutreachDone { get; set; }
    [JsonPropertyName("pool")] public int? Pool { get; set; }
}

public sealed class IndustryStats
{
    [JsonPropertyName("total")] public int? Total { get; set; }
    [JsonPropertyName("delivered")] public int? Delivered { get; set; }
    [JsonPropertyName("replies")] public int? Replies { get; set; }
    [JsonPropertyName("customers")] public int? Customers { get; set; }
}

public sealed class BatchLead
{
    [JsonPropertyName("id")] public int Id { get; set; }
    [JsonPropertyName("name")] public string? Name { get; set; }
    [JsonPropertyName("industry")] public string? Industry { get; set; }
    [JsonPropertyName("city")] public string? City { get; set; }
    [JsonPropertyName("email")] public string? Email { get; set; }

    public string DisplayCity =>
        string.Join(" · ", new[] { City, Industry }.Where(s => !string.IsNullOrEmpty(s)));
}

public sealed class FollowupLead
{
    [JsonPropertyName("id")] public int Id { get; set; }
    [JsonPropertyName("name")] public string? Name { get; set; }
    [JsonPropertyName("industry")] public string? Industry { get; set; }
    [JsonPropertyName("city")] public string? City { get; set; }
    [JsonPropertyName("status")] public string? Status { get; set; }
    [JsonPropertyName("follow_up_due")] public string? FollowUpDue { get; set; }

    public string DisplayCity =>
        string.Join(" · ", new[] { City, Industry }.Where(s => !string.IsNullOrEmpty(s)));
}

public sealed class CallQueueLead
{
    [JsonPropertyName("id")] public int Id { get; set; }
    [JsonPropertyName("name")] public string? Name { get; set; }
    [JsonPropertyName("industry")] public string? Industry { get; set; }
    [JsonPropertyName("city")] public string? City { get; set; }
    [JsonPropertyName("phone")] public string? Phone { get; set; }

    public string DisplayCity =>
        string.Join(" · ", new[] { City, Industry }.Where(s => !string.IsNullOrEmpty(s)));

    public string? TelUri
    {
        get
        {
            if (string.IsNullOrEmpty(Phone)) return null;
            var digits = new string(Phone.Where(char.IsDigit).ToArray());
            if (digits.Length == 0) return null;
            if (digits.Length == 10) digits = "1" + digits;
            return "tel:+" + digits;
        }
    }
}

public sealed class QueuedBatchInfo
{
    [JsonPropertyName("queued_batch")] public int? Batch { get; set; }
    [JsonPropertyName("n")] public int? Count { get; set; }
}

public sealed class Dashboard
{
    [JsonPropertyName("date")] public string? Date { get; set; }
    [JsonPropertyName("total")] public int? Total { get; set; }
    [JsonPropertyName("sent")] public int? Sent { get; set; }
    [JsonPropertyName("bounced")] public int? Bounced { get; set; }
    [JsonPropertyName("delivered")] public int? Delivered { get; set; }
    [JsonPropertyName("emailed")] public int? Emailed { get; set; }
    [JsonPropertyName("replies")] public int? Replies { get; set; }
    [JsonPropertyName("interested")] public int? Interested { get; set; }
    [JsonPropertyName("demos")] public int? Demos { get; set; }
    [JsonPropertyName("customers")] public int? Customers { get; set; }
    [JsonPropertyName("lost")] public int? Lost { get; set; }
    [JsonPropertyName("do_not_contact")] public int? DoNotContact { get; set; }
    [JsonPropertyName("not_contacted")] public int? NotContacted { get; set; }
    [JsonPropertyName("calls_total")] public int? CallsTotal { get; set; }
    [JsonPropertyName("calls_today")] public int? CallsToday { get; set; }
    [JsonPropertyName("calls_left_today")] public int? CallsLeftToday { get; set; }
    [JsonPropertyName("remaining_email")] public int? RemainingEmail { get; set; }
    [JsonPropertyName("remaining_phone")] public int? RemainingPhone { get; set; }
    [JsonPropertyName("in_call_queue")] public int? InCallQueue { get; set; }
    [JsonPropertyName("followups_due")] public int? FollowupsDue { get; set; }
    [JsonPropertyName("followups_today")] public int? FollowupsToday { get; set; }
    [JsonPropertyName("by_industry")] public Dictionary<string, IndustryStats>? ByIndustry { get; set; }
    [JsonPropertyName("money")] public Money? Money { get; set; }
    [JsonPropertyName("rates")] public Dictionary<string, StageRate>? Rates { get; set; }
    [JsonPropertyName("eta")] public Eta? Eta { get; set; }
    [JsonPropertyName("progress")] public ProgressInfo? Progress { get; set; }
    [JsonPropertyName("next_batch")] public List<BatchLead>? NextBatch { get; set; }
    [JsonPropertyName("queued_batch")] public QueuedBatchInfo? QueuedBatch { get; set; }
    [JsonPropertyName("followups")] public List<FollowupLead>? Followups { get; set; }
    [JsonPropertyName("call_queue")] public List<CallQueueLead>? CallQueue { get; set; }
}

// MARK: - Lists

public sealed class FilterLists
{
    [JsonPropertyName("cities")] public List<string>? Cities { get; set; }
    [JsonPropertyName("industries")] public List<string>? Industries { get; set; }
}

public sealed class LeadsResponse
{
    [JsonPropertyName("leads")] public List<Lead> Leads { get; set; } = [];
    [JsonPropertyName("total")] public int Total { get; set; }
    [JsonPropertyName("limit")] public int? Limit { get; set; }
    [JsonPropertyName("offset")] public int? Offset { get; set; }
    [JsonPropertyName("filters")] public FilterLists? Filters { get; set; }
}

public sealed class LeadResponse
{
    [JsonPropertyName("lead")] public Lead Lead { get; set; } = new();
}

public sealed class ActionsList
{
    [JsonPropertyName("date")] public string? Date { get; set; }
    [JsonPropertyName("followups")] public List<FollowupLead>? Followups { get; set; }
    [JsonPropertyName("call_queue")] public List<CallQueueLead>? CallQueue { get; set; }
    [JsonPropertyName("next_batch")] public List<BatchLead>? NextBatch { get; set; }
    [JsonPropertyName("calls_left_today")] public int? CallsLeftToday { get; set; }
    [JsonPropertyName("calls_today")] public int? CallsToday { get; set; }
}

public sealed class QueueBatchResult
{
    [JsonPropertyName("batch")] public int? Batch { get; set; }
    [JsonPropertyName("queued")] public int? Queued { get; set; }
}

// MARK: - Email

/// One row of the CRM `emails` table (Gmail integration). Inbound rows are
/// synced in by the bridge; outbound rows are queued here and sent by the
/// bridge only after Matt approves each send.
public sealed class EmailMessage
{
    [JsonPropertyName("id")] public int Id { get; set; }
    [JsonPropertyName("lead_id")] public int? LeadId { get; set; }
    [JsonPropertyName("direction")] public string Direction { get; set; } = "in";
    [JsonPropertyName("from_addr")] public string? FromAddr { get; set; }
    [JsonPropertyName("to_addr")] public string? ToAddr { get; set; }
    [JsonPropertyName("subject")] public string? Subject { get; set; }
    [JsonPropertyName("body")] public string? Body { get; set; }
    [JsonPropertyName("gmail_id")] public string? GmailId { get; set; }
    [JsonPropertyName("received_at")] public string? ReceivedAt { get; set; }
    [JsonPropertyName("status")] public string? Status { get; set; }
    [JsonPropertyName("error")] public string? Error { get; set; }
    [JsonPropertyName("lead_name")] public string? LeadName { get; set; }

    public bool IsInbound => Direction == "in";

    /// The other side of the conversation: who sent it (inbound) or who it
    /// goes to (outbound).
    public string Counterpart => IsInbound ? (FromAddr ?? "") : (ToAddr ?? "");

    public string BadgeLabel
    {
        get
        {
            if (IsInbound) return "In";
            return Status switch
            {
                "queued" => "Queued",
                "sent" => "Sent",
                "failed" => "Failed",
                _ => "Out",
            };
        }
    }

    public string BadgeColorHex
    {
        get
        {
            if (IsInbound) return "#3B82F6";
            return Status switch
            {
                "queued" => "#F59E0B",
                "sent" => "#22C55E",
                "failed" => "#EF4444",
                _ => "#6B7280",
            };
        }
    }
}

public sealed class EmailsResponse
{
    [JsonPropertyName("emails")] public List<EmailMessage> Emails { get; set; } = [];
    [JsonPropertyName("total")] public int Total { get; set; }
}

public sealed class InterestedLeadsResponse
{
    [JsonPropertyName("leads")] public List<Lead> Leads { get; set; } = [];
    [JsonPropertyName("total")] public int Total { get; set; }
}

public sealed class QueuedEmailResponse
{
    [JsonPropertyName("email")] public EmailMessage Email { get; set; } = new();
}
