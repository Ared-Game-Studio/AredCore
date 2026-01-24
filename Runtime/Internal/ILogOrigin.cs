namespace Ared.Core.Internal
{
    public interface ILogOrigin
    {
        abstract ILogOrigin Logger { get; }
        ELogOrigin LogOrigin { get; }
        
        void LogError(string message) => Ared.Core.Internal.Logger.LogError(message, LogOrigin);
        void LogWarning(string message) => Ared.Core.Internal.Logger.LogWarning(message, LogOrigin);
        void Log(string message) => Ared.Core.Internal.Logger.Log(message, LogOrigin);
    }
}