namespace SDG.Unturned.Community.Components.Audio
{
	public class StreamableAudio
	{
		public StreamableAudio(string url, AudioHandle handle, bool isStream, bool autoStart)
		{
			Url = url;
			Handle = handle;
			IsStream = isStream;
			AutoStart = autoStart;
		}

		public string Url { get; }
		public AudioHandle Handle { get; }
		public bool IsStream { get; }
		public bool AutoStart { get; }

		public StreamableAudioComponent StreamableAudioComponent { get; set; }
	}
}
