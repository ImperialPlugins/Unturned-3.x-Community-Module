using UnityEngine;

namespace SDG.Unturned.Community.Components
{
	public abstract class ModuleComponent : MonoBehaviour
	{
		protected SteamChannel Channel { get; set; }
		public virtual void OnInitialize()
		{

		}

		public virtual void OnShutdown()
		{

		}

		public void Awake()
		{
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
