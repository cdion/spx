namespace Spx.Web.Components.Pages.Nexus;

internal sealed class NexusPagePresenceState
{
    public Guid ConnectedGameId { get; private set; }

    public Guid ConnectedPresenceLeaseId { get; private set; }

    public Guid ConnectedPresencePlayerId { get; private set; }

    public bool IsPresenceRegistered { get; private set; }

    public CancellationTokenSource? PresenceLeaseRenewalCts { get; private set; }

    public Task? PresenceLeaseRenewalTask { get; private set; }

    public bool HasConnectedGame => ConnectedGameId != Guid.Empty;

    public bool IsConnectedTo(Guid gameId) => ConnectedGameId == gameId;

    public void SetConnectedGame(Guid gameId) => ConnectedGameId = gameId;

    public void ClearConnectedGame() => ConnectedGameId = Guid.Empty;

    public Guid EnsurePresenceLease(Guid playerId)
    {
        if (!IsPresenceRegistered)
        {
            ConnectedPresenceLeaseId = Guid.NewGuid();
            IsPresenceRegistered = true;
        }

        ConnectedPresencePlayerId = playerId;
        return ConnectedPresenceLeaseId;
    }

    public Guid ClearPresenceLease()
    {
        var leaseId = ConnectedPresenceLeaseId;
        IsPresenceRegistered = false;
        ConnectedPresenceLeaseId = Guid.Empty;
        ConnectedPresencePlayerId = Guid.Empty;
        return leaseId;
    }

    public bool TryBeginRenewalLoop(out CancellationToken cancellationToken)
    {
        if (PresenceLeaseRenewalTask is not null)
        {
            cancellationToken = PresenceLeaseRenewalCts?.Token ?? CancellationToken.None;
            return false;
        }

        PresenceLeaseRenewalCts = new CancellationTokenSource();
        cancellationToken = PresenceLeaseRenewalCts.Token;
        return true;
    }

    public void SetRenewalLoopTask(Task renewalLoopTask) =>
        PresenceLeaseRenewalTask = renewalLoopTask;

    public (CancellationTokenSource? CancellationTokenSource, Task? RenewalTask) StopRenewalLoop()
    {
        var cancellationTokenSource = PresenceLeaseRenewalCts;
        var renewalTask = PresenceLeaseRenewalTask;
        PresenceLeaseRenewalCts = null;
        PresenceLeaseRenewalTask = null;
        return (cancellationTokenSource, renewalTask);
    }
}
