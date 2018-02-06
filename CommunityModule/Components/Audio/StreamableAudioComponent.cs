using UnityEngine;

namespace SDG.Unturned.Community.Components.Audio
{
	public class StreamableAudioComponent : MonoBehaviour
	{
		public AudioSource Audio => GetComponent<AudioSource>();
		public StreamableAudio AudioInfo { get; private set; }
		public bool IsPlaying => Audio.isPlaying;

		public int Interval = 60;

		private AudioClip _clip;
		private bool _played;
		private WWW _www;
		private float _timer;

		public static StreamableAudioComponent CreateFrom(StreamableAudio audioInfo)
		{
			GameObject o = new GameObject();
			if (Player.player != null)
				o.transform.position = Player.player.transform.position;

			var audioSource = o.AddComponent<AudioSource>();
			audioSource.loop = false;
			audioSource.rolloffMode = AudioRolloffMode.Logarithmic;
			audioSource.dopplerLevel = 1f;
			audioSource.spread = 0f;

			StreamableAudioComponent comp = o.AddComponent<StreamableAudioComponent>();

			comp.AudioInfo = audioInfo;
			return comp;
		}

		private void Start()
		{
			Reset();
		}

		public void Reset()
		{
			_clip = null;
			_played = false;
			_timer = 0;
			if (Audio.isPlaying)
				Audio.Stop();
		}

		private void Update()
		{
			if (!AudioInfo.IsStream)
			{
				if (_played)
					return;

				_www = new WWW(AudioInfo.Url);
				_clip = _www.GetAudioClip(false, AudioInfo.IsStream);
				Audio.PlayOneShot(_clip);
				_played = true;
				return;
			}

			_timer = _timer + 1 * Time.deltaTime; //Mathf.FloorToInt(Time.timeSinceLevelLoad*10); 
												  //Time.frameCount; 
			if (string.IsNullOrEmpty(AudioInfo.Url))
				return;

			if (_timer >= Interval)
			{             //if(timer%interval == 0){
				if (_www != null)
				{
					_www.Dispose();
					_www = null;
					_played = false;
					_timer = 0;
				}
			}
			else
			{
				if (_www == null)
				{
					_www = new WWW(AudioInfo.Url);
				}
			}
			if (_clip == null)
			{
				if (_www != null)
				{
					_clip = _www.GetAudioClip(false, AudioInfo.IsStream);
				}
			}

			if (_clip == null)
				return;

			if (_clip.loadState == AudioDataLoadState.Loaded && _played == false)
			{
				Audio.PlayOneShot(_clip);
				_played = true;
				_clip = null;
			}
		}

		public void Dispose()
		{
			Pause();
			_www.Dispose();

			Destroy(Audio);
			Destroy(this);
		}

		public void Resume()
		{
			Audio.UnPause();
		}

		public void Pause()
		{
			Audio.Pause();
		}

		public void SetMode(AudioMode mode)
		{
			throw new System.NotImplementedException();
		}
	}
}
