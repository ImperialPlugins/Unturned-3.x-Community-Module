using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Steamworks;
using UnityEngine;
using ThreadPriority = System.Threading.ThreadPriority;

namespace SDG.Unturned.Community.Components.Audio
{
	public class AudioModule : ModuleComponent
	{
		public const int MAX_CONCURRENT_AUDIOS_PER_SERVER = 5;
		public const int MAX_CONCURRENT_STREAMS_PER_SERVER = 1;

		private readonly List<StreamableAudio> _audios = new List<StreamableAudio>();
		private readonly Queue<StreamableAudio> _queuedAudio = new Queue<StreamableAudio>();

		public StreamableAudioComponent GetAudio(int playbackId)
		{
			return _audios.FirstOrDefault(c => c.PlaybackId == playbackId)?.StreamableAudioComponent;
		}

		public bool StreamExists(int playbackId)
		{
			return _audios.Any(c => c.PlaybackId == playbackId);
		}

		public bool CanPlayAudio(int playbackId, bool isStream)
		{
			//check if total count of playing audios and queued autostart audios exceeds limit
			bool canPlayAudio = StreamExists(playbackId)
			                    || _audios.Count(c => c.StreamableAudioComponent.IsPlaying) +
			                    _queuedAudio.Count(c => !StreamExists(c.PlaybackId) && c.AutoStart)
			                    < MAX_CONCURRENT_AUDIOS_PER_SERVER;

			if (!isStream)
				return canPlayAudio;

			//check if total count of streamed audios and queued streamed audios exceeds limit
			bool canPlayStream =
				StreamExists(playbackId)
				|| _audios.Count(c => c.IsStream) + _queuedAudio.Count(c => c.IsStream && !StreamExists(c.PlaybackId))
				< MAX_CONCURRENT_STREAMS_PER_SERVER;

			return canPlayAudio && canPlayStream;
		}
		
		public override void OnInitialize()
		{
			Provider.onServerDisconnected += OnServerDisconnected;
		}

		private void OnServerDisconnected(CSteamID steamid)
		{
			DisposeAll();
		}

		public override void OnShutdown()
		{
			DisposeAll();
		}

		private void DisposeAll()
		{
			foreach (var playbackId in _audios.Select(c => c.PlaybackId))
			{
				StopAndDispose(playbackId);
			}

			_queuedAudio.Clear();
			_audios.Clear();
		}

		[SteamCall]
		public void PlayAudio(CSteamID sender, string url, int playbackId, bool isStream, bool autoStart)
		{
			if (!Channel.checkServer(sender))
				return;

			if (!CanPlayAudio(playbackId, isStream))
				return;

			_queuedAudio.Enqueue(new StreamableAudio(url, playbackId, isStream, autoStart));
		}

		[SteamCall]
		public void SetAudioVolume(CSteamID sender, int playbackId, float volume)
		{
			if (!Channel.checkServer(sender))
				return;

			var audio = GetAudio(playbackId);
			if (audio == null)
				return;

			audio.Audio.volume = volume;
		}

		[SteamCall]
		public void AttachAudioToPlayer(CSteamID sender, int playbackId, CSteamID target)
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

		[SteamCall]
		public void AttachAudioToVehicle(CSteamID sender, int playbackId, uint vehId)
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

		[SteamCall]
		public void DeattachAudio(CSteamID sender, int playbackId)
		{
			if (!Channel.checkServer(sender))
				return;

			var audio = GetAudio(playbackId);
			if (audio == null)
				return;

			audio.transform.SetParent(null);
		}

		[SteamCall]
		public void SetAudioPosition(CSteamID sender, int playbackId, Vector3 pos)
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

		[SteamCall]
		public void SetAudioMaxDistance(CSteamID sender, int playbackId, float maxDistance)
		{
			if (!Channel.checkServer(sender))
				return;

			var audio = GetAudio(playbackId);
			if (audio == null)
				return;
			
			audio.Audio.maxDistance = maxDistance;
		}

		[SteamCall]
		public void SetAudioMinDistance(CSteamID sender, int playbackId, float minDistance)
		{
			if (!Channel.checkServer(sender))
				return;

			var audio = GetAudio(playbackId);
			if (audio == null)
				return;

			audio.Audio.minDistance = minDistance;
		}

		[SteamCall]
		public void ResumeAudio(CSteamID sender, int playbackId)
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

		[SteamCall]
		public void PauseAudio(CSteamID sender, int playbackId)
		{
			if (!Channel.checkServer(sender))
				return;

			var audio = GetAudio(playbackId);
			if (audio == null)
				return;

			audio.Pause();
		}

		/*
		[SteamCall]
		public void StopAudio(CSteamID sender, int playbackId)
		{
			if (!Channel.checkServer(sender) || !StreamExists(playbackId))
				return;

			GetAudio(playbackId).Stop();
		}
		*/

		[SteamCall]
		public void RemoveAudio(CSteamID sender, int playbackId)
		{
			if (!Channel.checkServer(sender) || !StreamExists(playbackId))
				return;

			StopAndDispose(playbackId, true);
		}

		private void StopAndDispose(int playbackId, bool remove = false)
		{
			var stream = GetAudio(playbackId);
			if (stream == null)
				return;

			stream.Dispose();

			Destroy(stream.gameObject);

			if (remove)
				_audios.RemoveAll(c => c.PlaybackId == playbackId);
		}

		private void Update()
		{
			if (_queuedAudio.Count == 0)
				return;

			var nextItem = _queuedAudio.Dequeue();
			bool isNew = true;

			var audio = _audios.FirstOrDefault(c => c.PlaybackId == nextItem.PlaybackId);
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
