#if USE_CROSSPLATFORMBRIDGE_PHOTONFUSION
using Fusion;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace CrossPlatformBridge.Platform.PhotonFusion.Network
{
	/// <summary>
	/// Photon Fusion の SessionInfo リストを管理するクラス。
	/// OnSessionListUpdated で受け取ったセッション一覧を保持し、
	/// 検索・フィルタ機能を提供する。
	/// </summary>
	public class RoomList : IEnumerable<SessionInfo>
	{
		private List<SessionInfo> _sessions = new List<SessionInfo>();

		/// <summary>セッションリストを更新します。</summary>
		public void Update(List<SessionInfo> newSessions)
		{
			_sessions = newSessions ?? new List<SessionInfo>();
		}

		/// <summary>セッションリストをクリアします。</summary>
		public void Clear()
		{
			_sessions.Clear();
		}

		/// <summary>現在保持しているセッション数。</summary>
		public int Count => _sessions.Count;

		public IEnumerator<SessionInfo> GetEnumerator() => _sessions.GetEnumerator();
		IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

		/// <summary>満員でないルームのみ返します。</summary>
		public IEnumerable<SessionInfo> GetAvailableRooms()
		{
			return _sessions.Where(s => s.PlayerCount < s.MaxPlayers);
		}

		/// <summary>名前で検索します（完全一致）。</summary>
		public SessionInfo FindByName(string name)
		{
			return _sessions.FirstOrDefault(s => s.Name == name);
		}

		/// <summary>名前の部分一致で検索します。</summary>
		public IEnumerable<SessionInfo> SearchByName(string query)
		{
			if (string.IsNullOrEmpty(query)) return _sessions;
			return _sessions.Where(s => s.Name.Contains(query));
		}
	}
}

#endif
