namespace Core.Interfaces
{
    /// <summary>
    /// <see cref="Initialize" /> method will be called during dependency injection.
    /// Should only be added to controllers or viewmodels.
    /// </summary>
    public interface IInitializable
    {
        /// <summary>
        /// Will be called during the dependency injection phase.
        /// </summary>
        void Initialize();
    }
}