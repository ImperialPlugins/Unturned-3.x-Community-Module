using System;
using UnityEngine;

namespace SDG.Unturned.Community.Components
{
	public abstract class ModuleComponent : SteamCaller
	{
		public SteamChannel Channel => GetComponent<SteamChannel>();

		protected virtual void Awake()
		{
			_channel = Channel;
			Debug.Log("Initializing: " + GetType().Name);
		}

		protected virtual void OnDestroy()
		{
			Debug.Log("Shutting down: " + GetType().Name);
		}
	}

	public abstract class SingletonModuleComponent<T> : ModuleComponent where T: SteamCaller
	{
		public static T Instance { get; private set; }

		protected override void Awake()
		{
			if(Instance != null)
				throw new Exception(GetType().FullName + " is singleton but multiple instances tried to initialize!");
			Instance = this as T;
			base.Awake();
		}

		protected override void OnDestroy()
		{
			base.OnDestroy();
			Instance = null;
		}
	}
}
