#if UNITY_EDITOR
using UnityEditor;
using GooglePlayServices;
using Ared.Core.Internal;
using Logger = Ared.Core.Internal.Logger;

namespace Ared.Core.Editor
{
    public static class AutoResolver
    {
        static AutoResolver()
        {
            EditorApplication.delayCall += TriggerResolve;
        }

        private static void TriggerResolve()
        {
            if (SessionState.GetBool("AredCore_DependenciesResolved", false)) return;
            
            Logger.Log("Forcing Android Dependency Resolution...", ELogOrigin.System);
            
            PlayServicesResolver.Resolve(null, false, (success) => {
                if (success) Logger.Log("Resolution Complete!", ELogOrigin.System);
                else Logger.LogError("Resolution Failed. Check Console.", ELogOrigin.System);
            });

            SessionState.SetBool("AredCore_DependenciesResolved", true);
        }
    }
}
#endif