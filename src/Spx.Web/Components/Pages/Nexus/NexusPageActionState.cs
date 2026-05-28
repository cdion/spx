namespace Spx.Web.Components.Pages.Nexus;

internal sealed class NexusPageActionState
{
    public bool IsLeaving { get; private set; }

    public bool IsSubmittingGameplayAction { get; private set; }

    public bool TryBeginLeave()
    {
        if (IsLeaving)
        {
            return false;
        }

        IsLeaving = true;
        return true;
    }

    public void CompleteLeave() => IsLeaving = false;

    public bool TryBeginGameplayAction()
    {
        if (IsSubmittingGameplayAction)
        {
            return false;
        }

        IsSubmittingGameplayAction = true;
        return true;
    }

    public void CompleteGameplayAction() => IsSubmittingGameplayAction = false;
}
