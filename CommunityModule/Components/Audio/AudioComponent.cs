using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using Steamworks;
using UnityEngine;
using ThreadPriority = System.Threading.ThreadPriority;

namespace SDG.Unturned.Community.Components.Audio
{
	public class AudioComponent : ModuleComponent
	{
		public const int MAX_CONCURRENT_STREAMS_PER_SERVER = 5;
		private readonly Dictionary<int, GameObject> _streams = new Dictionary<int, GameObject>();
		private bool _run = true;
		private Thread _networkThread;
		private EventWaitHandle _eventHandle;

		private readonly List<QueuedAudio> _queuedAudio = new List<QueuedAudio>();

		public StreamComponent GetStream(int playbackId)
		{
			if (!_streams.ContainsKey(playbackId))
				return null;
			return _streams[playbackId].GetComponent<StreamComponent>();
		}

		public override void OnInitialize()
		{
			_run = true;
			Provider.onServerDisconnected += OnServerDisconnected;

			_eventHandle = new EventWaitHandle(false, EventResetMode.AutoReset);

			_networkThread = new Thread(AsyncUpdate)
			{
				Priority = ThreadPriority.BelowNormal,
				IsBackground = true
			};

			_networkThread.Start();
		}

		private void AsyncUpdate()
		{
			while (_run)
			{
				_eventHandle.WaitOne();
				var item = _queuedAudio.First();

				var obj = StreamComponent.Create(this, item.Url);
				_streams.Add(item.PlaybackId, obj);
			}
		}

		private void OnServerDisconnected(CSteamID steamid)
		{
			DisposeAll();
		}

		public override void OnShutdown()
		{
			_run = false;
			DisposeAll();
		}

		private void DisposeAll()
		{
			foreach (var playbackId in _streams.Keys)
			{
				StopAndDispose(playbackId);
			}

			_streams.Clear();
		}

		[SteamCall]
		public void PlayAudio(CSteamID sender, string url, int playbackId, bool isStream)
		{
			if (!Channel.checkServer(sender))
				return;

			if (!_streams.ContainsKey(playbackId)
			    && (_streams.Count + _queuedAudio.Count(c => !_streams.ContainsKey(c.PlaybackId)))
			    > MAX_CONCURRENT_STREAMS_PER_SERVER)
				return;

			_queuedAudio.Add(new QueuedAudio(url, playbackId));
			_eventHandle.Set();
		}

		[SteamCall]
		public void SetAudioVolume(CSteamID sender, int playbackId, float volume)
		{
			if (!Channel.checkServer(sender) || !_streams.ContainsKey(playbackId))
				return;

			GetStream(playbackId).Audio.volume = volume;
		}

		[SteamCall]
		public void AttachAudioToPlayer(CSteamID sender, int playbackId, CSteamID target)
		{
			if (!Channel.checkServer(sender) || !_streams.ContainsKey(playbackId))
				return;

			var obj = _streams[playbackId];
			var player = Provider.clients.FirstOrDefault(c => c.playerID.steamID == target);
			if (player == null)
				return;

			obj.transform.SetParent(player.player.transform, false);
			obj.transform.localPosition = Vector3.zero;
		}

		[SteamCall]
		public void AttachAudioToVehicle(CSteamID sender, int playbackId, uint vehId)
		{
			if (!Channel.checkServer(sender) || !_streams.ContainsKey(playbackId))
				return;

			var obj = _streams[playbackId];

			var veh = VehicleManager.vehicles.FirstOrDefault(c => c.instanceID == vehId);
			if (veh == null)
				return;

			obj.transform.SetParent(veh.transform, false);
			obj.transform.localPosition = Vector3.zero;
		}

		[SteamCall]
		public void DeattachAudio(CSteamID sender, int playbackId)
		{
			if (!Channel.checkServer(sender) || !_streams.ContainsKey(playbackId))
				return;

			var obj = _streams[playbackId];
			obj.transform.SetParent(null);
		}

		[SteamCall]
		public void SetAudioPosition(CSteamID sender, int playbackId, Vector3 pos)
		{
			if (!Channel.checkServer(sender) || !_streams.ContainsKey(playbackId))
				return;

			var transf = _streams[playbackId].transform;
			if (transf.parent != null)
				transf.localPosition = pos;
			else
				transf.position = pos;
		}

		[SteamCall]
		public void SetAudioMaxDistance(CSteamID sender, int playbackId, float maxDistance)
		{
			if (!Channel.checkServer(sender) || !_streams.ContainsKey(playbackId))
				return;

			GetStream(playbackId).Audio.maxDistance = maxDistance;
		}

		[SteamCall]
		public void SetAudioMinDistance(CSteamID sender, int playbackId, float minDistance)
		{
			if (!Channel.checkServer(sender) || !_streams.ContainsKey(playbackId))
				return;

			GetStream(playbackId).Audio.minDistance = minDistance;
		}

		[SteamCall]
		public void ResumeAudio(CSteamID sender, int playbackId)
		{
			if (!Channel.checkServer(sender) || !_streams.ContainsKey(playbackId))
				return;

			GetStream(playbackId).Resume();
		}

		[SteamCall]
		public void PauseAudio(CSteamID sender, int playbackId)
		{
			if (!Channel.checkServer(sender) || !_streams.ContainsKey(playbackId))
				return;

			GetStream(playbackId).Pause();
		}

		/*
		[SteamCall]
		public void StopAudio(CSteamID sender, int playbackId)
		{
			if (!Channel.checkServer(sender) || !_streams.ContainsKey(playbackId))
				return;

			GetStream(playbackId).Stop();
		}
		*/

		[SteamCall]
		public void RemoveAudio(CSteamID sender, int playbackId)
		{
			if (!Channel.checkServer(sender) || !_streams.ContainsKey(playbackId))
				return;

			StopAndDispose(playbackId, true);
		}

		private void StopAndDispose(int playbackId, bool remove = false)
		{
			if (!_streams.ContainsKey(playbackId))
				return;

			GetStream(playbackId).Dispose();
			Destroy(_streams[playbackId]);

			if (remove)
				_streams.Remove(playbackId);
		}
	}
}
