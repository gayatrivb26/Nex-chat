using ChatApp.Domain.Enums;

namespace ChatApp.Domain.Entities;

public class CallLog : BaseEntity
{
    public Guid? ConversationId { get; private set; }
    public Guid? InitiatorId { get; private set; }
    public CallType CallType { get; private set; }
    public CallStatus Status { get; set; }
    public DateTime StartedAt { get; private set; } = DateTime.UtcNow;
    public DateTime? AnsweredAt { get; set; }
    public DateTime? EndedAt { get; set; }
    public int DurationSeconds { get; set; }
    public string? EndReason { get; set; }
    public string? RecordingUrl { get; set; }

    public Conversation? Conversation { get; set; }
    public User? Initiator { get; set; }

    private CallLog() { }

    public static CallLog Create(Guid conversationId, Guid initiatorId, CallType callType)
        => new()
        {
            ConversationId = conversationId,
            InitiatorId = initiatorId,
            CallType = callType,
            Status = CallStatus.Initiated
        };

    public void Answer() { Status = CallStatus.Answered; AnsweredAt = DateTime.UtcNow; }
    public void End(string reason = "normal")
    {
        Status = CallStatus.Ended;
        EndedAt = DateTime.UtcNow;
        EndReason = reason;
        if (AnsweredAt.HasValue)
            DurationSeconds = (int)(EndedAt.Value - AnsweredAt.Value).TotalSeconds;
    }
    public void Reject() { Status = CallStatus.Rejected; EndedAt = DateTime.UtcNow; }
    public void Miss() { Status = CallStatus.Missed; EndedAt = DateTime.UtcNow; }
}
