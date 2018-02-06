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

			if (!url.StartsWith("http://") && !url.StartsWith("https://"))
				return;

			if (!url.EndsWith(".ogg") && !url.EndsWith(".wav"))
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

			var player = Provider.clients.FirstOrDefault(c => c.playerID.steamID == target);
			if (player == null)
				return;

			GetAudio(playbackId)?.AttachTo(player.player.transform);
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

			var veh = VehicleManager.vehicles.FirstOrDefault(c => c.instanceID == vehId);
			if (veh == null)
				return;

			GetAudio(playbackId)?.AttachTo(veh.transform);
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

			GetAudio(playbackId)?.Deattach();
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

			GetAudio(playbackId)?.SetPosition(pos);
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
			if (!Channel.checkServer(sender))
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
		/// Sets the audio mode
		/// </summary>
		/// <param name="target">The SteamCall target</param>
		/// <param name="handle">The audio handle</param>
		/// <param name="mode">The target audio mode</param>
		public void SetMode(CSteamID target, AudioHandle handle, AudioMode mode)
		{
			channel.send(nameof(AudioSteamCall_SetMode), target, ESteamPacket.UPDATE_RELIABLE_BUFFER, (int)handle, (int)mode);
		}

		/// <summary>
		/// Sets the audio mode
		/// </summary>
		/// <param name="target">The SteamCall target</param>
		/// <param name="handle">The audio handle</param>
		/// <param name="mode">The target audio mode</param>
		public void SetMode(ESteamCall target, AudioHandle handle, AudioMode mode)
		{
			channel.send(nameof(AudioSteamCall_SetMode), target, ESteamPacket.UPDATE_RELIABLE_BUFFER, (int)handle, (int)mode);
		}

		[SteamCall]
		public void AudioSteamCall_SetMode(CSteamID sender, int playbackId, int mode)
		{
			if (!Channel.checkServer(sender))
				return;

			GetAudio(playbackId)?.SetMode((AudioMode) mode);
		}

		/// <summary>
		/// Mutes or unmutes an audio
		/// </summary>
		/// <param name="target">The SteamCall target</param>
		/// <param name="handle">The audio handle</param>
		/// <param name="mute">Mute audio?</param>
		public void SetMute(CSteamID target, AudioHandle handle, bool mute)
		{
			channel.send(nameof(AudioSteamCall_SetMute), target, ESteamPacket.UPDATE_RELIABLE_BUFFER, (int)handle, mute);
		}

		/// <summary>
		/// Mutes or unmutes an audio
		/// </summary>
		/// <param name="target">The SteamCall target</param>
		/// <param name="handle">The audio handle</param>
		/// <param name="mute">Mute audio?</param>
		public void SetMute(ESteamCall target, AudioHandle handle, bool mute)
		{
			channel.send(nameof(AudioSteamCall_SetMute), target, ESteamPacket.UPDATE_RELIABLE_BUFFER, (int)handle, mute);
		}

		[SteamCall]
		public void AudioSteamCall_SetMute(CSteamID sender, int playbackId, bool mute)
		{
			if (!Channel.checkServer(sender))
				return;

			GetAudio(playbackId)?.SetMuted(mute);
		}

		/// <summary>
		/// Sets how much the 3D engine has an effect on the audio source
		/// </summary>
		/// <param name="target">The SteamCall target</param>
		/// <param name="handle">The audio handle</param>
		/// <param name="blend">Spatial blend amount</param>
		public void SetSpatialBlend(CSteamID target, AudioHandle handle, float blend)
		{
			channel.send(nameof(AudioSteamCall_SetSpatialBlend), target, ESteamPacket.UPDATE_RELIABLE_BUFFER, (int)handle, blend);
		}

		/// <summary>
		/// Sets how much the 3D engine has an effect on the audio source
		/// </summary>
		/// <param name="target">The SteamCall target</param>
		/// <param name="handle">The audio handle</param>
		/// <param name="blend">Spatial blend amount</param>
		public void SetSpatialBlend(ESteamCall target, AudioHandle handle, float blend)
		{
			channel.send(nameof(AudioSteamCall_SetSpatialBlend), target, ESteamPacket.UPDATE_RELIABLE_BUFFER, (int)handle, blend);
		}

		[SteamCall]
		public void AudioSteamCall_SetSpatialBlend(CSteamID sender, int playbackId, float blend)
		{
			if (!Channel.checkServer(sender))
				return;

			GetAudio(playbackId)?.SetSpatialBlend(blend);
		}

		/// <summary>
		/// Sets the spread angle to 3D stereo or multichannel sound in speaker space.
		/// </summary>
		/// <param name="target">The SteamCall target</param>
		/// <param name="handle">The audio handle</param>
		/// <param name="spread">Spread amount</param>
		public void SetSpread(CSteamID target, AudioHandle handle, float spread)
		{
			channel.send(nameof(AudioSteamCall_SetSpread), target, ESteamPacket.UPDATE_RELIABLE_BUFFER, (int)handle, spread);
		}

		/// <summary>
		///	Sets the spread angle to 3D stereo or multichannel sound in speaker space.
		/// </summary>
		/// <param name="target">The SteamCall target</param>
		/// <param name="handle">The audio handle</param>
		/// <param name="spread">Spread amount</param>
		public void SetSpread(ESteamCall target, AudioHandle handle, float spread)
		{
			channel.send(nameof(AudioSteamCall_SetSpread), target, ESteamPacket.UPDATE_RELIABLE_BUFFER, (int)handle, spread);
		}

		[SteamCall]
		public void AudioSteamCall_SetSpread(CSteamID sender, int playbackId, float blend)
		{
			if (!Channel.checkServer(sender))
				return;

			GetAudio(playbackId)?.SetSpatialBlend(blend);
		}

		/// <summary>
		/// Determines how much doppler effect will be applied to this audio source (if is set to 0, then no effect is applied
		/// </summary>
		/// <param name="target">The SteamCall target</param>
		/// <param name="handle">The audio handle</param>
		/// <param name="level">Doppler level</param>
		public void SetDopplerLevel(CSteamID target, AudioHandle handle, float level)
		{
			channel.send(nameof(AudioSteamCall_SetDopplerLevel), target, ESteamPacket.UPDATE_RELIABLE_BUFFER, (int)handle, level);
		}

		/// <summary>
		///	Determines how much doppler effect will be applied to this audio source (if is set to 0, then no effect is applied
		/// </summary>
		/// <param name="target">The SteamCall target</param>
		/// <param name="handle">The audio handle</param>
		/// <param name="level">Doppler level</param>
		public void SetDopplerLevel(ESteamCall target, AudioHandle handle, float level)
		{
			channel.send(nameof(AudioSteamCall_SetDopplerLevel), target, ESteamPacket.UPDATE_RELIABLE_BUFFER, (int)handle, level);
		}

		[SteamCall]
		public void AudioSteamCall_SetDopplerLevel(CSteamID sender, int playbackId, float level)
		{
			if (!Channel.checkServer(sender))
				return;

			GetAudio(playbackId)?.SetDopplerLevel(level);
		}

		/// <summary>
		/// Sets the audio rolloff mode. Custom is not supported.
		/// </summary>
		/// <param name="target">The SteamCall target</param>
		/// <param name="handle">The audio handle</param>
		/// <param name="mode">The target audio rolloff mode</param>
		public void SetRolloffMode(CSteamID target, AudioHandle handle, AudioRolloffMode mode)
		{
			channel.send(nameof(AudioSteamCall_SetRolloffMode), target, ESteamPacket.UPDATE_RELIABLE_BUFFER, (int)handle, (int)mode);
		}

		/// <summary>
		/// Sets the audio rolloff mode. Custom is not supported.
		/// </summary>
		/// <param name="target">The SteamCall target</param>
		/// <param name="handle">The audio handle</param>
		/// <param name="mode">The target audio rolloff mode</param>
		public void SetRolloffMode(ESteamCall target, AudioHandle handle, AudioRolloffMode mode)
		{
			channel.send(nameof(AudioSteamCall_SetRolloffMode), target, ESteamPacket.UPDATE_RELIABLE_BUFFER, (int)handle, (int)mode);
		}

		[SteamCall]
		public void AudioSteamCall_SetRolloffMode(CSteamID sender, int playbackId, int mode)
		{
			if (!Channel.checkServer(sender))
				return;

			GetAudio(playbackId)?.SetRolloffMode((AudioRolloffMode)mode);
		}

		/// <summary>
		/// Sets the interval of when the audio should refresh stream
		/// </summary>
		/// <param name="target">The SteamCall target</param>
		/// <param name="handle">The audio handle</param>
		/// <param name="interval">The interval</param>
		public void SetInterval(CSteamID target, AudioHandle handle, float interval)
		{
			channel.send(nameof(AudioSteamCall_SetInterval), target, ESteamPacket.UPDATE_RELIABLE_BUFFER, (int)handle, interval);
		}

		/// <summary>
		/// Sets the interval of when the audio should refresh stream
		/// </summary>
		/// <param name="target">The SteamCall target</param>
		/// <param name="handle">The audio handle</param>
		/// <param name="interval">The interval</param>
		public void SetInterval(ESteamCall target, AudioHandle handle, float interval)
		{
			channel.send(nameof(AudioSteamCall_SetInterval), target, ESteamPacket.UPDATE_RELIABLE_BUFFER, (int)handle, interval);
		}

		[SteamCall]
		public void AudioSteamCall_SetInterval(CSteamID sender, int playbackId, float interval)
		{
			if (!Channel.checkServer(sender))
				return;

			var audio = GetAudio(playbackId);
			if (audio == null)
				return;

			audio.Interval = interval;
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
