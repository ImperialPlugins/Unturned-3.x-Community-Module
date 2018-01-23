using System;
using System.Collections.Generic;
using System.Reflection;
using SDG.Framework.Modules;
using SDG.Unturned.Community.Components;
using UnityEngine;

namespace SDG.Unturned.Community
{
    public class CommunityNexus : IModuleNexus
    {
	    private readonly List<ModuleComponent> _featureComponents = new List<ModuleComponent>();
	    private GameObject _moduleComponentsObject;

		public void initialize()
		{
			_moduleComponentsObject = new GameObject();
			var ch = _moduleComponentsObject.AddComponent<SteamChannel>();
			ch.id = Provider.channels;
			ch.setup();

			foreach (var type in GetType().Assembly.GetTypes())
			{
				if (!typeof(ModuleComponent).IsAssignableFrom(type)) continue;
				var obj = (ModuleComponent) _moduleComponentsObject.AddComponent(type);
				_featureComponents.Add(obj);
			}

			ch.build();
			Player.onPlayerCreated += OnPlayerCreated;
			
			Debug.Log("Community Module initialization completed");
		}

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
				UnityEngine.Object.Destroy(comp);

			UnityEngine.Object.Destroy(_moduleComponentsObject);

			Debug.Log("Community Module Shutdown completed");
		}
    }
}
