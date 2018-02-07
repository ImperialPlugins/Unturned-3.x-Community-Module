using System.Security.Policy;
using UnityEngine;

namespace SDG.Unturned.Community.Components.Audio
{
	public class StreamableAudioComponent : MonoBehaviour
	{
		public AudioSource Audio => GetComponent<AudioSource>();
		public StreamableAudio AudioInfo { get; private set; }
		public bool IsPlaying => Audio.isPlaying;

		public float Interval { get; set; } = 60f;

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
			audioSource.priority = 0;
			audioSource.spatialBlend = 0f;

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
			//if (Audio.isPlaying)
			//	  Audio.Stop();

			_www?.Dispose();
			_www = null;
			_played = false;
			_timer = 0;
		}

		private void Update()
		{
			if (string.IsNullOrEmpty(AudioInfo.Url))
				return;

			_timer = _timer + 1 * Time.deltaTime; //Mathf.FloorToInt(Time.timeSinceLevelLoad*10); 
												  //Time.frameCount; 

			if (_timer >= Interval && AudioInfo.IsStream && _www != null && _clip.loadState == AudioDataLoadState.Loaded)
			{
				//Audio.Stop();
				Reset();
			}
			if (_www == null)
			{
				_www = new WWW(AudioInfo.Url);
			}
			if (_clip == null)
			{
				_clip = _www.GetAudioClip(false, true);
			}

			if (_clip.loadState != AudioDataLoadState.Loaded || _played)
				return;

			Audio.PlayOneShot(_clip);
			_played = true;
			_clip = null;
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
			Audio.spatialBlend = mode == AudioMode.Positional ? 1f : 0f;
		}

		public void SetSpatialBlend(float blend)
		{
			Audio.spatialBlend = blend;
		}

		public void SetMuted(bool mute)
		{
			Audio.mute = mute;
		}

		public void SetSpread(float spread)
		{
			Audio.spread = spread;
		}

		public void SetDopplerLevel(float level)
		{
			Audio.dopplerLevel = level;
		}

		public void SetRolloffMode(AudioRolloffMode mode)
		{
			Audio.rolloffMode = mode;
		}

		public void AttachTo(Transform targetTransform)
		{
			transform.SetParent(targetTransform, false);
			transform.localPosition = Vector3.zero;
		}

		public void Deattach()
		{
			transform.SetParent(null);
		}

		public void SetPosition(Vector3 pos)
		{
			if (transform.parent != null)
				transform.localPosition = pos;
			else
				transform.position = pos;
		}
	}
}
