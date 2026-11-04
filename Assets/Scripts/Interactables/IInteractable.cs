namespace StreamerGame.Interactables
{
    public interface IInteractable
    {
        string PromptText { get; }
        bool CanInteract { get; }
        void Interact();
    }
}
