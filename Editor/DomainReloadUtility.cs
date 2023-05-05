#if UNITY_EDITOR
using UnityEditor;

namespace Puffercat.Uxt.Editor
{
    [InitializeOnLoad]
    public static class DomainReloadUtility
    {
        public static bool IsDomainReloading { get; private set; }

        static DomainReloadUtility()
        {
            IsDomainReloading = false;
            AssemblyReloadEvents.beforeAssemblyReload += () => IsDomainReloading = true;
        }
    }
}
#endif
