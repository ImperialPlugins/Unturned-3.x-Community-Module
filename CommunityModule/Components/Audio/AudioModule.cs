using System.Collections.Generic;
using System.Linq;
using Steamworks;
using UnityEngine;

namespace SDG.Unturned.Community.Components.Audio
{
	public class AudioModule : SingletonModuleComponent<AudioModule>
	{
		public const int MAX_CONCURRENT_AUDIOS_PER_SERVER = 5;
		public const int MAX_CONCURRENT_STREAMS_PER_SERVER = 1;

		private readonly List<StreamableAudio> _audios = new List<StreamableAudio>();
		private readonly Queue<StreamableAudio> _queuedAudio = new Queue<StreamableAudio>();
		private StreamableAudioComponent GetAudio(AudioHandle handle)
		{
			return _audios.FirstOrDefault(c => c.Handle == handle)?.StreamableAudioComponent;
		}
		private bool StreamExists(AudioHandle handle)
		{
			return _audios.Any(c => c.Handle == handle);
		}

		private bool CanPlayAudio(AudioHandle handle, bool isStream)
		{
			//check if total count of playing audios and queued autostart audios exceeds limit
			bool canPlayAudio = StreamExists(handle)
								|| _audios.Count(c => c.StreamableAudioComponent.IsPlaying) +
								_queuedAudio.Count(c => !StreamExists(c.Handle) && c.AutoStart)
								< MAX_CONCURRENT_AUDIOS_PER_SERVER;

			if (!isStream)
				return canPlayAudio;

			//check if total count of streamed audios and queued streamed audios exceeds limit
			bool canPlayStream =
				StreamExists(handle)
				|| _audios.Count(c => c.IsStream) + _queuedAudio.Count(c => c.IsStream && !StreamExists(c.Handle))
				< MAX_CONCURRENT_STREAMS_PER_SERVER;

			return canPlayAudio && canPlayStream;
		}

		protected override void Awake()
		{
			base.Awake();
			Provider.onServerDisconnected += OnServerDisconnected;
		}

		private void OnServerDisconnected(CSteamID steamid)
		{
			DisposeAll();
		}

		protected override void OnDestroy()
		{
			DisposeAll();
			base.OnDestroy();
		}

		private void DisposeAll()
		{
			foreach (var handle in _audios.Select(c => c.Handle))
			{
				StopAndDispose(handle);
			}

			_queuedAudio.Clear();
			_audios.Clear();
		}

		public AudioHandle LastPlaybackHandle { get; private set; }

		/// <summary>
		/// Starts playing some audio
		/// </summary>
		/// <param name="target">The SteamCall target</param>
		/// <param name="url">Url to play / stream</param>
		/// <param name="isStream">Download the audio or stream it?</param>
		/// <param name="autoStart">Automatically start playing?</param>
		/// <returns>the associated handle</returns>
		public AudioHandle Play(CSteamID target, string url, bool isStream, bool autoStart = true)
		{
			LastPlaybackHandle++;
			Channel.send(nameof(AudioSteamCall_Play), target, ESteamPacket.UPDATE_RELIABLE_BUFFER, url, (int)LastPlaybackHandle, isStream, autoStart);
			return LastPlaybackHandle;
		}

		/// <summary>
		/// Starts playing some audio
		/// </summary>
		/// <param name="target">The SteamCall target</param>
		/// <param name="url">Url to play / stream</param>
		/// <param name="isStream">Download the audio or stream it?</param>
		/// <param name="autoStart">Automatically start playing?</param>
		/// <returns>the associated audio handle</returns>
		public AudioHandle Play(ESteamCall target, string url, bool isStream, bool autoStart = true)
		{
			LastPlaybackHandle++;
			Channel.send(nameof(AudioSteamCall_Play), target, ESteamPacket.UPDATE_RELIABLE_BUFFER, url, (int)LastPlaybackHandle, isStream, autoStart);
			return LastPlaybackHandle;
		}

		[SteamCall]
		public void AudioSteamCall_Play(CSteamID sender, string url, int playbackId, bool isStream, bool autoStart)
		{
			if (!Channel.checkServer(sender))
				return;

			if (!CanPlayAudio(playbackId, isStream))
				return;

			_queuedAudio.Enqueue(new StreamableAudio(url, playbackId, isStream, autoStart));
		}

		/// <summary>
		/// Sets the volume of an audio
		/// </summary>
		/// <param name="target">The SteamCall target</param>
		/// <param name="handle">The audio handle</param>
		/// <param name="volume">The volume to set</param>
		public void SetVolume(CSteamID target, AudioHandle handle, float volume)
		{
			channel.send(nameof(AudioSteamCall_SetVolume), target, ESteamPacket.UPDATE_RELIABLE_BUFFER, (int)handle, volume);
		}

		/// <summary>
		/// Sets the volume of an audio
		/// </summary>
		/// <param name="target">The SteamCall target</param>
		/// <param name="handle">The audio handle</param>
		/// <param name="volume">The volume to set</param>
		public void SetVolume(ESteamCall target, AudioHandle handle, float volume)
		{
			channel.send(nameof(AudioSteamCall_SetVolume), target, ESteamPacket.UPDATE_RELIABLE_BUFFER, (int)handle, volume);
		}

		[SteamCall]
		public void AudioSteamCall_SetVolume(CSteamID sender, int playbackId, float volume)
		{
			if (!Channel.checkServer(sender))
				return;

			var audio = GetAudio(playbackId);
			if (audio == null)
				return;

			audio.Audio.volume = volume;
		}

		/// <summary>
		/// Attaches an audio to a player (position gets reset)
		/// </summary>
		/// <param name="target">The SteamCall target</param>
		/// <param name="handle">The audio handle</param>
		/// <param name="targetPlayer">The player to attach to</param>
		public void AttachToPlayer(CSteamID target, AudioHandle handle, SteamPlayer targetPlayer)
		{
			channel.send(nameof(AudioSteamCall_AttachToPlayer), target, ESteamPacket.UPDATE_RELIABLE_BUFFER, (int)handle, targetPlayer.playerID.steamID);
		}

		/// <summary>
		/// Attaches an audio to a player (position gets reset)
		/// </summary>
		/// <param name="target">The SteamCall target</param>
		/// <param name="handle">The audio handle</param>
		/// <param name="targetPlayer">The player to attach to</param>
		public void AttachToPlayer(ESteamCall target, AudioHandle handle, SteamPlayer targetPlayer)
		{
			channel.send(nameof(AudioSteamCall_AttachToPlayer), target, ESteamPacket.UPDATE_RELIABLE_BUFFER, (int)handle, targetPlayer.playerID.steamID);
		}

		[SteamCall]
		public void AudioSteamCall_AttachToPlayer(CSteamID sender, int playbackId, CSteamID target)
		{
			if (!Channel.checkServer(sender))
				return;

			var audio = GetAudio(playbackId);
			if (audio == null)
				return;

			var player = Provider.clients.FirstOrDefault(c => c.playerID.steamID == target);
			if (player == null)
				return;

			audio.transform.SetParent(player.player.transform, false);
			audio.transform.localPosition = Vector3.zero;
		}

		/// <summary>
		/// Attaches an audio to a vehicle (position gets reset)
		/// </summary>
		/// <param name="target">The SteamCall target</param>
		/// <param name="handle">The audio handle</param>
		/// <param name="targetVehicle">The id of the vehicle to attach to</param>
		public void AttachToVehicle(CSteamID target, AudioHandle handle, InteractableVehicle targetVehicle)
		{
			channel.send(nameof(AudioSteamCall_AttachToVehicle), target, ESteamPacket.UPDATE_RELIABLE_BUFFER, (int)handle, targetVehicle.instanceID);
		}

		/// <summary>
		/// Attaches an audio to a vehicle (position gets reset)
		/// </summary>
		/// <param name="target">The SteamCall target</param>
		/// <param name="handle">The audio handle</param>
		/// <param name="targetVehicle">The id of the vehicle to attach to</param>
		public void AttachToVehicle(ESteamCall target, AudioHandle handle, InteractableVehicle targetVehicle)
		{
			channel.send(nameof(AudioSteamCall_AttachToVehicle), target, ESteamPacket.UPDATE_RELIABLE_BUFFER, (int)handle, targetVehicle.instanceID);
		}

		[SteamCall]
		public void AudioSteamCall_AttachToVehicle(CSteamID sender, int playbackId, uint vehId)
		{
			if (!Channel.checkServer(sender))
				return;

			var audio = GetAudio(playbackId);
			if (audio == null)
				return;

			var veh = VehicleManager.vehicles.FirstOrDefault(c => c.instanceID == vehId);
			if (veh == null)
				return;

			audio.transform.SetParent(veh.transform, false);
			audio.transform.localPosition = Vector3.zero;
		}

		/// <summary>
		/// Deattachs an audio from a vehicle or player (resets position)
		/// </summary>
		/// <param name="target">The SteamCall target</param>
		/// <param name="handle">The audio handle</param>
		public void Deattach(CSteamID target, AudioHandle handle)
		{
			channel.send(nameof(AudioSteamCall_Deattach), target, ESteamPacket.UPDATE_RELIABLE_BUFFER, (int)handle);
		}

		/// <summary>
		/// Deattachs an audio from a vehicle or player (resets position)
		/// </summary>
		/// <param name="target">The SteamCall target</param>
		/// <param name="handle">The audio handle</param>
		public void Deattach(ESteamCall target, AudioHandle handle)
		{
			channel.send(nameof(AudioSteamCall_Deattach), target, ESteamPacket.UPDATE_RELIABLE_BUFFER, (int)handle);
		}


		[SteamCall]
		public void AudioSteamCall_Deattach(CSteamID sender, int playbackId)
		{
			if (!Channel.checkServer(sender))
				return;

			var audio = GetAudio(playbackId);
			if (audio == null)
				return;

			audio.transform.SetParent(null);
		}

		/// <summary>
		/// Sets the position of an audio
		/// </summary>
		/// <param name="target">The SteamCall target</param>
		/// <param name="handle">The audio handle</param>
		/// <param name="pos">The target position (local if attached; global if not)</param>
		public void SetPosition(CSteamID target, AudioHandle handle, Vector3 pos)
		{
			channel.send(nameof(AudioSteamCall_SetPosition), target, ESteamPacket.UPDATE_RELIABLE_BUFFER, (int)handle, pos);
		}

		/// <summary>
		/// Sets the position of an audio
		/// </summary>
		/// <param name="target">The SteamCall target</param>
		/// <param name="handle">The audio handle</param>
		/// <param name="pos">The target position (local if attached; global if not)</param>
		public void SetPosition(ESteamCall target, AudioHandle handle, Vector3 pos)
		{
			channel.send(nameof(AudioSteamCall_SetPosition), target, ESteamPacket.UPDATE_RELIABLE_BUFFER, (int)handle, pos);
		}

		[SteamCall]
		public void AudioSteamCall_SetPosition(CSteamID sender, int playbackId, Vector3 pos)
		{
			if (!Channel.checkServer(sender))
				return;

			var audio = GetAudio(playbackId);
			if (audio == null)
				return;

			var transf = audio.transform;
			if (transf.parent != null)
				transf.localPosition = pos;
			else
				transf.position = pos;
		}

		/// <summary>
		/// Sets the max distance of an audio
		/// </summary>
		/// <param name="target">The SteamCall target</param>
		/// <param name="handle">The audio handle</param>
		/// <param name="maxDistance">The max distance of the audio</param>
		public void SetMaxDistance(CSteamID target, AudioHandle handle, float maxDistance)
		{
			channel.send(nameof(AudioSteamCall_SetMaxDistance), target, ESteamPacket.UPDATE_RELIABLE_BUFFER, (int)handle, maxDistance);
		}

		/// <summary>
		/// Sets the max distance of an audio
		/// </summary>
		/// <param name="target">The SteamCall target</param>
		/// <param name="handle">The audio handle</param>
		/// <param name="maxDistance">The max distance of the audio</param>
		public void SetMaxDistance(ESteamCall target, AudioHandle handle, float maxDistance)
		{
			channel.send(nameof(AudioSteamCall_SetMaxDistance), target, ESteamPacket.UPDATE_RELIABLE_BUFFER, (int)handle, maxDistance);
		}

		[SteamCall]
		public void AudioSteamCall_SetMaxDistance(CSteamID sender, int playbackId, float maxDistance)
		{
			if (!Channel.checkServer(sender))
				return;

			var audio = GetAudio(playbackId);
			if (audio == null)
				return;

			audio.Audio.maxDistance = maxDistance;
		}

		/// <summary>
		/// Sets the min distance of an audio
		/// </summary>
		/// <param name="target">The SteamCall target</param>
		/// <param name="handle">The audio handle</param>
		/// <param name="minDistance">The max distance of the audio</param>
		public void SetMinDistance(CSteamID target, AudioHandle handle, float minDistance)
		{
			channel.send(nameof(AudioSteamCall_SetMinDistance), target, ESteamPacket.UPDATE_RELIABLE_BUFFER, (int)handle, minDistance);
		}

		/// <summary>
		/// Sets the min distance of an audio
		/// </summary>
		/// <param name="target">The SteamCall target</param>
		/// <param name="handle">The audio handle</param>
		/// <param name="minDistance">The max distance of the audio</param>
		public void SetMinDistance(ESteamCall target, AudioHandle handle, float minDistance)
		{
			channel.send(nameof(AudioSteamCall_SetMinDistance), target, ESteamPacket.UPDATE_RELIABLE_BUFFER, (int)handle, minDistance);
		}

		[SteamCall]
		public void AudioSteamCall_SetMinDistance(CSteamID sender, int playbackId, float minDistance)
		{
			if (!Channel.checkServer(sender))
				return;

			var audio = GetAudio(playbackId);
			if (audio == null)
				return;

			audio.Audio.minDistance = minDistance;
		}

		/// <summary>
		/// Resumes a paused audio
		/// </summary>
		/// <param name="target">The SteamCall target</param>
		/// <param name="handle">The audio handle</param>
		public void Resume(CSteamID target, AudioHandle handle)
		{
			channel.send(nameof(AudioSteamCall_Resume), target, ESteamPacket.UPDATE_RELIABLE_BUFFER, (int)handle);
		}

		/// <summary>
		/// Resumes a paused audio
		/// </summary>
		/// <param name="target">The SteamCall target</param>
		/// <param name="handle">The audio handle</param>
		public void Resume(ESteamCall target, AudioHandle handle)
		{
			channel.send(nameof(AudioSteamCall_Resume), target, ESteamPacket.UPDATE_RELIABLE_BUFFER, (int)handle);
		}

		[SteamCall]
		public void AudioSteamCall_Resume(CSteamID sender, int playbackId)
		{
			if (!Channel.checkServer(sender))
				return;

			var audio = GetAudio(playbackId);
			if (audio == null)
				return;

			if (!CanPlayAudio(playbackId, audio.AudioInfo.IsStream))
				return;

			GetAudio(playbackId).Resume();
		}

		/// <summary>
		/// Pauses an audio
		/// </summary>
		/// <param name="target">The SteamCall target</param>
		/// <param name="handle">The audio handle</param>
		public void Pause(CSteamID target, AudioHandle handle)
		{
			channel.send(nameof(AudioSteamCall_Pause), target, ESteamPacket.UPDATE_RELIABLE_BUFFER, (int)handle);
		}

		/// <summary>
		/// Pauses an audio
		/// </summary>
		/// <param name="target">The SteamCall target</param>
		/// <param name="handle">The audio handle</param>
		public void Pause(ESteamCall target, AudioHandle handle)
		{
			channel.send(nameof(AudioSteamCall_Pause), target, ESteamPacket.UPDATE_RELIABLE_BUFFER, (int)handle);
		}

		[SteamCall]
		public void AudioSteamCall_Pause(CSteamID sender, int playbackId)
		{
			if (!Channel.checkServer(sender))
				return;

			GetAudio(playbackId)?.Pause();
		}

		/// <summary>
		/// Destroys an audio to free resources 
		/// </summary>
		/// <param name="target">The SteamCall target</param>
		/// <param name="handle">The audio handle</param>
		public void Destroy(CSteamID target, AudioHandle handle)
		{
			channel.send(nameof(AudioSteamCall_Destroy), target, ESteamPacket.UPDATE_RELIABLE_BUFFER, (int)handle);
		}

		/// <summary>
		/// Destroys an audio to free resources 
		/// </summary>
		/// <param name="target">The SteamCall target</param>
		/// <param name="handle">The audio handle</param>
		public void Destroy(ESteamCall target, AudioHandle handle)
		{
			channel.send(nameof(AudioSteamCall_Destroy), target, ESteamPacket.UPDATE_RELIABLE_BUFFER, (int)handle);
		}

		[SteamCall]
		public void AudioSteamCall_Destroy(CSteamID sender, int playbackId)
		{
			if (!Channel.checkServer(sender) || !StreamExists(playbackId))
				return;

			StopAndDispose(playbackId, true);
		}

		/// <summary>
		/// Sets if an audio should loop
		/// </summary>
		/// <param name="target">The SteamCall target</param>
		/// <param name="handle">The audio handle</param>
		/// <param name="loop">Loop audio?</param>
		public void SetLoop(CSteamID target, AudioHandle handle, bool loop)
		{
			channel.send(nameof(AudioSteamCall_SetLoop), target, ESteamPacket.UPDATE_RELIABLE_BUFFER, (int)handle, loop);
		}

		/// <summary>
		/// Sets if an audio should loop
		/// </summary>
		/// <param name="target">The SteamCall target</param>
		/// <param name="handle">The audio handle</param>
		/// <param name="loop">Loop audio?</param>
		public void SetLoop(ESteamCall target, AudioHandle handle, bool loop)
		{
			channel.send(nameof(AudioSteamCall_SetLoop), target, ESteamPacket.UPDATE_RELIABLE_BUFFER, (int)handle, loop);
		}

		[SteamCall]
		public void AudioSteamCall_SetLoop(CSteamID sender, int playbackId, bool loop)
		{
			if (!Channel.checkServer(sender) || !StreamExists(playbackId))
				return;

			var audio = GetAudio(playbackId);
			if (audio == null)
				return;

			audio.Audio.loop = loop;
		}

		/// <summary>
		/// Destroys all audio objects
		/// </summary>
		/// <param name="target">The SteamCall target</param>
		public void DestroyAll(CSteamID target)
		{
			channel.send(nameof(AudioSteamCall_DestroyAll), target, ESteamPacket.UPDATE_RELIABLE_BUFFER);
		}

		/// <summary>
		/// Destroys all audio objects
		/// </summary>
		/// <param name="target">The SteamCall target</param>
		public void DestroyAll(ESteamCall target)
		{
			channel.send(nameof(AudioSteamCall_DestroyAll), target, ESteamPacket.UPDATE_RELIABLE_BUFFER);
		}

		[SteamCall]
		public void AudioSteamCall_DestroyAll(CSteamID sender)
		{
			if (!Channel.checkServer(sender))
				return;

			DisposeAll();
		}

		/// <summary>
		/// Destroys all created audio objects
		/// </summary>
		/// <param name="target">The SteamCall target</param>
		/// <param name="handle">The audio handle</param>
		/// <param name="mode">The target audio mode</param>
		public void SetMode(CSteamID target, AudioHandle handle, AudioMode mode)
		{
			channel.send(nameof(AudioSteamCall_SetMode), target, ESteamPacket.UPDATE_RELIABLE_BUFFER, (int)mode);
		}

		/// <summary>
		/// Destroys all created audio objects
		/// </summary>
		/// <param name="target">The SteamCall target</param>
		/// <param name="handle">The audio handle</param>
		/// <param name="mode">The target audio mode</param>
		public void SetMode(ESteamCall target, AudioHandle handle, AudioMode mode)
		{
			channel.send(nameof(AudioSteamCall_SetMode), target, ESteamPacket.UPDATE_RELIABLE_BUFFER, (int)mode);
		}

		[SteamCall]
		public void AudioSteamCall_SetMode(CSteamID sender, int playbackId, int mode)
		{
			if (!Channel.checkServer(sender))
				return;

			GetAudio(playbackId)?.SetMode((AudioMode) mode);
		}

		private void StopAndDispose(AudioHandle handle, bool remove = false)
		{
			var stream = GetAudio(handle);
			if (stream == null)
				return;

			stream.Dispose();

			Destroy(stream.gameObject);

			if (remove)
				_audios.RemoveAll(c => c.Handle == handle);
		}

		private void Update()
		{
			if (_queuedAudio.Count == 0)
				return;

			var nextItem = _queuedAudio.Dequeue();
			bool isNew = true;

			var audio = _audios.FirstOrDefault(c => c.Handle == nextItem.Handle);
			if (audio != null)
			{
				DestroyImmediate(audio.StreamableAudioComponent.gameObject);
				DestroyImmediate(audio.StreamableAudioComponent);
				isNew = false;
			}
			else
			{
				audio = nextItem;
			}

			var obj = StreamableAudioComponent.CreateFrom(audio);
			if (!audio.AutoStart)
				obj.Pause();

			audio.StreamableAudioComponent = obj;

			if (isNew)
				_audios.Add(audio);
		}
	}
}
