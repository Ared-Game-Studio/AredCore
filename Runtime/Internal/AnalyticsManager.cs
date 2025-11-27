namespace Ared.Core.Internal
{
    public static class AnalyticsManager
    {
        public static void InitializeGameAnalyticsAndFacebook()
        {
            // Initialize Game Analytics
            var gameAnalyticsHandler = new GameAnalyticHandler();
            
            // Initialize Facebook Analytics
            var facebookHandler = new FacebookAnalyticHandler();
        }
    }
}
