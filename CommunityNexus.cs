using SDG.Framework.Modules;
using UnityEngine;

namespace SDG.Unturned.Community
{
    public class CommunityNexus : IModuleNexus
    {
		public void initialize()
		{
			Debug.Log("Community Module Initialized");
		}

		public void shutdown()
		{
			Debug.Log("Community Module Shutdown");
		}
    }
}
