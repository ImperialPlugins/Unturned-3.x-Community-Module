namespace SDG.Unturned.Community.Components.Audio
{
	public enum AudioMode
	{
		/// <summary>
		/// Audio which does not depend on position of AudioListener
		/// </summary>
		NonPositional,

		/// <summary>
		/// Audio which adjusts its volume according to the 3D position of AudioListener
		/// </summary>
		Positional3D
	}
}
