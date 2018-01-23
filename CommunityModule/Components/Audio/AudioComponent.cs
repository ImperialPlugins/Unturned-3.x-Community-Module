using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using NAudio.Wave;
using Steamworks;

namespace SDG.Unturned.Community.Components.Audio
{
	public class AudioComponent : ModuleComponent
	{
		public const int MAX_WAVEOUTS_PER_SERVER = 5;
		private readonly Dictionary<int, WaveOut> _waveOuts = new Dictionary<int, WaveOut>();
		private bool _run = true;
		private Thread _networkThread;
		private EventWaitHandle _eventHandle;

		private readonly List<QueuedAudio> _queuedAudio = new List<QueuedAudio>();

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
				PlayMp3FromUrl(item.Url, item.PlaybackId);
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
			foreach (var waveOut in _waveOuts.Values)
			{
				waveOut.Stop();
				waveOut.Dispose();
			}

			_waveOuts.Clear();
		}

		[SteamCall]
		public void SetVolume(CSteamID sender, int playbackId, float volume)
		{
			if (!Channel.checkServer(sender) || !_waveOuts.ContainsKey(playbackId))
				return;

			_waveOuts[playbackId].Volume = volume;
		}

		[SteamCall]
		public void PlayAudio(CSteamID sender, string url, int playbackId)
		{
			if (!Channel.checkServer(sender))
				return;

			if (!_waveOuts.ContainsKey(playbackId)
				&& (_waveOuts.Count + _queuedAudio.Count(c => !_waveOuts.ContainsKey(c.PlaybackId)))
					> MAX_WAVEOUTS_PER_SERVER)
				return;

			_queuedAudio.Add(new QueuedAudio(url, playbackId));
			_eventHandle.Set();
		}

		[SteamCall]
		public void ResumeAudio(CSteamID sender, int playbackId)
		{
			if (!Channel.checkServer(sender) || !_waveOuts.ContainsKey(playbackId))
				return;

			_waveOuts[playbackId].Resume();
		}

		[SteamCall]
		public void PauseAudio(CSteamID sender, int playbackId)
		{
			if (!Channel.checkServer(sender) || !_waveOuts.ContainsKey(playbackId))
				return;

			_waveOuts[playbackId].Pause();
		}

		[SteamCall]
		public void StopAudio(CSteamID sender, int playbackId)
		{
			if (!Channel.checkServer(sender) || !_waveOuts.ContainsKey(playbackId))
				return;

			_waveOuts[playbackId].Stop();
		}


		[SteamCall]
		public void RemoveAudio(CSteamID sender, int playbackId)
		{
			if (!Channel.checkServer(sender) || !_waveOuts.ContainsKey(playbackId))
				return;

			_waveOuts[playbackId].Stop();
			_waveOuts[playbackId].Dispose();
			_waveOuts.Remove(playbackId);
		}

		private void PlayMp3FromUrl(string url, int playbackId)
		{
			try
			{
				using (Stream ms = new MemoryStream())
				{
					using (Stream stream = WebRequest.Create(url)
						.GetResponse().GetResponseStream())
					{
						byte[] buffer = new byte[32768];
						int read;

						while ((read = stream.Read(buffer, 0, buffer.Length)) > 0)
						{
							ms.Write(buffer, 0, read);
						}
					}

					ms.Position = 0;
					using (WaveStream blockAlignedStream =
						new BlockAlignReductionStream(
							WaveFormatConversionStream.CreatePcmStream(
								new Mp3FileReader(ms))))
					{
						if (!_waveOuts.ContainsKey(playbackId))
						{
							_waveOuts.Add(playbackId, new WaveOut(WaveCallbackInfo.FunctionCallback()));
						}

						WaveOut waveOut = _waveOuts[playbackId];
						waveOut.Init(blockAlignedStream);
						waveOut.Play();
					}
				}
			}
			catch
			{
				if (_waveOuts.ContainsKey(playbackId))
				{
					_waveOuts[playbackId].Dispose();
					_waveOuts.Remove(playbackId);
				}
			}
		}
	}
}
