using System.Collections.Generic;
using SDG.Framework.Modules;
using SDG.Unturned.Community.Components;
using UnityEngine;
using Object = UnityEngine.Object;

namespace SDG.Unturned.Community
{
	public class CommunityNexus : IModuleNexus
	{
		private readonly List<ModuleComponent> _featureComponents = new List<ModuleComponent>();
		private GameObject _moduleComponentsObject;
		public static CommunityNexus Instance { get; private set; }
		public void initialize()
		{
			Instance = this;

			_moduleComponentsObject = new GameObject();

			Object.DontDestroyOnLoad(_moduleComponentsObject);

			Channel = _moduleComponentsObject.GetComponent<SteamChannel>();
			if (!Channel)
			{
				Channel = _moduleComponentsObject.AddComponent<SteamChannel>();
				Object.DontDestroyOnLoad(Channel);
				Channel.id = Provider.channels;
				Channel.setup();
			}

			foreach (var type in GetType().Assembly.GetTypes())
			{
				if (!typeof(ModuleComponent).IsAssignableFrom(type)) continue;
				var comp = (ModuleComponent)_moduleComponentsObject.AddComponent(type);
				_featureComponents.Add(comp);
				Object.DontDestroyOnLoad(comp);
			}

			Channel.build();
			Player.onPlayerCreated += OnPlayerCreated;
			Debug.Log("Community Module initialization completed at ch: #" + Channel.id);
		}

		public SteamChannel Channel { get; set; }

		private void OnPlayerCreated(Player player)
		{
			foreach (var type in GetType().Assembly.GetTypes())
			{
				if (!typeof(PlayerModuleComponent).IsAssignableFrom(type)) continue;
				player.gameObject.AddComponent(type);
			}
		}

		public void shutdown()
		{
			foreach (var comp in _featureComponents)
				Object.Destroy(comp);

			Object.Destroy(_moduleComponentsObject);

			Debug.Log("Community Module Shutdown completed");
			Instance = null;
		}
	}
}
