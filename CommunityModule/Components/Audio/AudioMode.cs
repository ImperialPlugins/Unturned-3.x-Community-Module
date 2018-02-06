namespace SDG.Unturned.Community.Components.Audio
{
	public enum AudioMode
	{
		/// <summary>
		/// Audio which does not depend on position of the AudioListener
		/// </summary>
		NonPositional,

		/// <summary>
		/// Audio which adjusts its volume based on the position of the AudioListener
		/// </summary>
		Positional
	}
}
