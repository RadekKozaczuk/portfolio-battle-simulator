namespace Core.Interfaces
{
    /// <summary>
    /// Indicates that this class is synchronized with Unity life-cycle. The synchronization happens in main controllers.
    /// Treat this interface as a label - its job it to make code more readable by making key methods more visible.
    /// </summary>
    public interface ICustomUpdate
    {
        void CustomUpdate();
    }
}