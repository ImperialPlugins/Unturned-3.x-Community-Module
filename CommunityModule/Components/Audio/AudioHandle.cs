namespace SDG.Unturned.Community.Components.Audio
{
	public struct AudioHandle
	{
		internal AudioHandle(int playbackId)
		{
			PlaybackId = playbackId;
		}

		internal int PlaybackId;

		public static implicit operator int(AudioHandle handle)
		{
			return handle.PlaybackId;
		}

		public static implicit operator AudioHandle(int id)
		{
			return new AudioHandle(id);
		}
	}
}
