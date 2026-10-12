using System.Runtime.CompilerServices;

namespace EHMR.Services;

/// <summary>
/// Tracks whether a form view-model has already completed an entry session.
/// The tracker is shared so Create/Edit forms use one consistent session lifecycle
/// while keeping entity-specific reset and reload logic in their view-models.
/// </summary>
public sealed class FormSessionLifecycle
{
    private sealed class SessionState
    {
        public bool HasCompletedEntry { get; set; }
    }

    private readonly ConditionalWeakTable<object, SessionState> _sessions = new();

    /// <summary>
    /// Returns true when the owner has already completed an entry session.
    /// </summary>
    public bool HasCompletedEntry(object owner)
    {
        ArgumentNullException.ThrowIfNull(owner);
        return _sessions.TryGetValue(owner, out var state) && state.HasCompletedEntry;
    }

    /// <summary>
    /// Marks an entry session as completed after its initial data/context load succeeds.
    /// </summary>
    public void MarkEntryCompleted(object owner)
    {
        ArgumentNullException.ThrowIfNull(owner);
        _sessions.GetOrCreateValue(owner).HasCompletedEntry = true;
    }

    /// <summary>
    /// Clears lifecycle state when a form owner is intentionally discarded.
    /// </summary>
    public void Forget(object owner)
    {
        ArgumentNullException.ThrowIfNull(owner);
        _sessions.Remove(owner);
    }
}
