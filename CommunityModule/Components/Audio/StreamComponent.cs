using UnityEngine;

namespace SDG.Unturned.Community.Components.Audio
{
	public class StreamComponent : MonoBehaviour
	{
		public AudioSource Audio => GetComponent<AudioSource>();
		public string Url;
		public int Interval = 300;
		public AudioComponent AudioComponent;

		private AudioClip _clip;
		private bool _played;
		private WWW _www;
		private float _timer;
		
		public static GameObject Create(AudioComponent audioComponent, string url)
		{
			GameObject o = new GameObject();

			o.AddComponent<AudioSource>();
			StreamComponent comp = o.AddComponent<StreamComponent>();

			comp.Url = url;
			comp.AudioComponent = audioComponent;
			return o;
		}
		
		private void Start()
		{
			_clip = null;
			_played = false;
			_timer = 0;
		}

		private void Update()
		{
			_timer = _timer + 1 * Time.deltaTime; //Mathf.FloorToInt(Time.timeSinceLevelLoad*10); 
												  //Time.frameCount; 
			if (Url == null)
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
					_www = new WWW(Url);
				}
			}
			if (_clip == null)
			{
				if (_www != null)
				{
					_clip = _www.GetAudioClip(false, true);
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
	}
}
