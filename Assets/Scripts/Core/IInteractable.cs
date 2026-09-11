namespace TopDownGame.Core
{
    public interface IInteractable
    {
        bool CanInteract { get; }
        string InteractionPrompt { get; }
        void Interact(Player.PlayerInteraction player);
    }
}
