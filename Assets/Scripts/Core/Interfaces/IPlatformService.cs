namespace GameCore.Core.Interfaces
{
    public interface IPlatformService : IService
    {
        string PlatformName { get; }
        bool SupportsCloud { get; }
        void ExecutePlatformSpecificAction(string actionName);
    }
}
