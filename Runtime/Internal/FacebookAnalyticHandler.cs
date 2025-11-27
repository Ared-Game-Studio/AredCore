using UnityEngine;
using Facebook.Unity;
using Logger = Ared.Core.Internal.Logger;

namespace Ared.Core.Internal
{
    public class FacebookAnalyticHandler
    {
        public FacebookAnalyticHandler()
        {
            Logger.Log("Is Facebook Initialized: " + FB.IsInitialized, Logger.LogOrigin.Analytics);
            
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
                Logger.Log("Initialized the Facebook SDK", Logger.LogOrigin.Analytics);
            }
            else
            {
                Logger.Log("Failed to Initialize the Facebook SDK", Logger.LogOrigin.Analytics);
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