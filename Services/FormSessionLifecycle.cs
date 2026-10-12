using System.Runtime.CompilerServices;

namespace EHMR.Services;

/// <summary>
/// Shared entry-session state for form view-models.
/// The lifecycle is centralized here; each form supplies its own reset/reload
/// action so entity-specific behavior and incoming navigation context remain intact.
/// </summary>
public sealed class FormSessionLifecycle
{
    private sealed class SessionState
    {
        public bool HasCompletedEntry { get; set; }
    }

    private readonly ConditionalWeakTable<object, SessionState> _sessions = new();

    /// <summary>
    /// Starts a Create entry. If this owner has already completed an entry,
    /// the supplied reset runs before the previous session state is cleared.
    /// </summary>
    public bool BeginCreateEntry(object owner, Action resetForNewSession)
    {
        ArgumentNullException.ThrowIfNull(owner);
        ArgumentNullException.ThrowIfNull(resetForNewSession);

        if (!_sessions.TryGetValue(owner, out var state) || !state.HasCompletedEntry)
            return false;

        // Only clear the previous session after reset succeeds.
        resetForNewSession();
        _sessions.Remove(owner);
        return true;
    }

    /// <summary>Returns true when the owner has completed an entry session.</summary>
    public bool HasCompletedEntry(object owner)
    {
        ArgumentNullException.ThrowIfNull(owner);
        return _sessions.TryGetValue(owner, out var state) && state.HasCompletedEntry;
    }

    /// <summary>Marks entry complete only after initial data/context loading succeeds.</summary>
    public void MarkEntryCompleted(object owner)
    {
        ArgumentNullException.ThrowIfNull(owner);
        _sessions.GetValue(owner, _ => new SessionState()).HasCompletedEntry = true;
    }

    /// <summary>Clears lifecycle state when a form owner is intentionally discarded.</summary>
    public void Forget(object owner)
    {
        ArgumentNullException.ThrowIfNull(owner);
        _sessions.Remove(owner);
    }
}
