namespace SDG.Unturned.Community.Components.Audio
{
	public class QueuedAudio
	{
		public QueuedAudio(string url, int playbackId)
		{
			Url = url;
			PlaybackId = playbackId;
		}

		public string Url { get; }
		public int PlaybackId { get; }
	}
}
