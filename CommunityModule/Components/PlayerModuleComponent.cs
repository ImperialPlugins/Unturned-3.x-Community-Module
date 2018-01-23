using System.Diagnostics;

namespace SDG.Unturned.Community.Components
{
	public abstract class PlayerModuleComponent : PlayerCaller
	{
		public SteamChannel Channel => GetComponent<SteamChannel>();
		public Player Player => GetComponent<Player>();

		public virtual void OnInitialize()
		{

		}

		public virtual void OnShutdown()
		{

		}

		public void Awake()
		{
			_channel = Channel;
			_player = Player;
			OnInitialize();
		}

		public void OnDestroy()
		{
			OnShutdown();
		}
	}
}
