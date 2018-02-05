using UnityEngine;

namespace SDG.Unturned.Community.Components.Audio
{
	public class StreamableAudio
	{
		public StreamableAudio(string url, int playbackId, bool isStream, bool autoStart)
		{
			Url = url;
			PlaybackId = playbackId;
			IsStream = isStream;
			AutoStart = autoStart;
		}

		public string Url { get; }
		public int PlaybackId { get; }
		public bool IsStream { get; }
		public bool AutoStart { get; }

		public StreamableAudioComponent StreamableAudioComponent { get; set; }
	}
}
