using UnityEngine;
using Facebook.Unity;
using Ared.Core.Internal;

namespace Ared.Core.Internal
{
    public class FacebookAnalyticHandler : ILogOrigin
    {
        public ILogOrigin Logger => this;
        public ELogOrigin LogOrigin => ELogOrigin.Analytics;
        
        public FacebookAnalyticHandler()
        {
            Logger.Log("Is Facebook Initialized: " + FB.IsInitialized);
            
            if (!FB.IsInitialized)
            {
                // Initialize the Facebook SDK
                FB.Init(InitCallback, OnHideUnity);
            }
            else
            {
                // Already initialized, signal an app activation App Event
                FB.ActivateApp();
            }
        }
        
        private void InitCallback()
        {
            if (FB.IsInitialized)
            {
                // Signal an app activation App Event
                FB.ActivateApp();
                // Continue with Facebook SDK
                // ...
                Logger.Log("Initialized the Facebook SDK");
            }
            else
            {
                Logger.Log("Failed to Initialize the Facebook SDK");
            }
        }

        private void OnHideUnity(bool isGameShown)
        {
            if (!isGameShown)
            {
                // Pause the game - we will need to hide
                Time.timeScale = 0;
            }
            else
            {
                // Resume the game - we're getting focus again
                Time.timeScale = 1;
            }
        }
    }
}