using UnityEngine;

namespace SDG.Unturned.Community.Components
{
	public abstract class ModuleComponent : SteamCaller
	{
		public SteamChannel Channel => GetComponent<SteamChannel>();
		public virtual void OnInitialize()
		{

		}

		public virtual void OnShutdown()
		{

		}

		public void Awake()
		{
			_channel = Channel;
			Debug.Log("Initializing: " + GetType().Name);
			OnInitialize();
		}

		public void OnDestroy()
		{
			Debug.Log("Shutting down: " + GetType().Name);
			OnShutdown();
		}
	}
}
