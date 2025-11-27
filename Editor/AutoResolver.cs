#if UNITY_EDITOR
using UnityEditor;
using GooglePlayServices;
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
            
            Logger.Log("Forcing Android Dependency Resolution...", Logger.LogOrigin.System);
            
            PlayServicesResolver.Resolve(null, false, (success) => {
                if (success) Logger.Log("Resolution Complete!", Logger.LogOrigin.System);
                else Logger.LogError("Resolution Failed. Check Console.", Logger.LogOrigin.System);
            });

            SessionState.SetBool("AredCore_DependenciesResolved", true);
        }
    }
}
#endif