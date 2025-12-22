namespace NimbusStation.Core.Session;

/// <summary>
/// Represents a Nimbus Station session tied to a ticket ID.
/// Sessions persist state to ~/.nimbus/sessions/{TicketId}/.
/// </summary>
/// <param name="TicketId">The unique identifier for this session (e.g., "SUP-123").</param>
/// <param name="CreatedAt">When the session was first created.</param>
/// <param name="LastAccessedAt">When the session was last accessed or resumed.</param>
/// <param name="ActiveContext">The currently active context within the session.</param>
public sealed record Session(
    string TicketId,
    DateTimeOffset CreatedAt,
    DateTimeOffset LastAccessedAt,
    SessionContext? ActiveContext)
{
    /// <summary>
    /// Creates a new session with the current timestamp.
    /// </summary>
    /// <param name="ticketId">The ticket ID for the session.</param>
    /// <returns>A new session instance.</returns>
    public static Session Create(string ticketId)
    {
        var now = DateTimeOffset.UtcNow;
        return new Session(ticketId, now, now, null);
    }

    /// <summary>
    /// Creates a new session instance with an updated LastAccessedAt timestamp.
    /// </summary>
    /// <returns>A new session with the current UTC time as LastAccessedAt.</returns>
    public Session Touch() => this with { LastAccessedAt = DateTimeOffset.UtcNow };

    /// <summary>
    /// Creates a new session instance with an updated active context.
    /// </summary>
    /// <param name="context">The new active context.</param>
    /// <returns>A new session with the updated context.</returns>
    public Session WithContext(SessionContext? context) => this with { ActiveContext = context };
}
