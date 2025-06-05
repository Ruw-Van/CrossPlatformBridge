// Assets/Scripts/Test/NetworkTestUI.cs
using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using System.Text;
using TMPro; // TextMeshProUGUI を使用する場合に必要
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Windows;

namespace CrossPlatformBridge.Test
{
	/// <summary>
	/// Network クラスの動作を確認するためのUIテストスクリプト。
	/// UI要素をスクリプト内で動的に作成します。
	/// </summary>
	public class NetworkTestUI : MonoBehaviour
	{
		[Header("Network Manager Reference")]
		[SerializeField] private Network.Network _network;

		[Header("UI Prefabs")]
		[SerializeField] private TMP_Dropdown _dropdownPrefab; // ★追加: ドロップダウンプレハブ

		[Header("UI Parent References")]
		[SerializeField] private Canvas _canvas;
		[SerializeField] private RectTransform _uiPanelRect; // UI要素を配置する親RectTransform

		// 動的に生成されるUI要素の親となるTransform
		private Transform _uiInputParent;
		private Transform _uiButtonParent;
		private Transform _uiStatusParent;
		private Transform _uiLobbyRoomButtonsParent; // ロビー/ルーム関連ボタンの新しい親

		// 動的に生成されるUI要素の参照
		private TMP_InputField _userNameInputField;
		private TMP_InputField _lobbyRoomIdInputField;
		private TMP_InputField _sendDataInputField;
		private TMP_Dropdown _networkHandlerDropdown; // ★追加: ネットワークハンドラ選択用ドロップダウン

		private Button _initializeButton;
		private Button _connectButton;
		private Button _createLobbyButton;
		private Button _joinLobbyButton;
		private Button _leaveLobbyButton; // ★追加: ロビー離脱ボタン
		private Button _createRoomButton; // ★追加: ルーム作成ボタン (既存のCreateLobbyと区別)
		private Button _joinRoomButton;   // ★追加: ルーム参加ボタン (既存のJoinLobbyと区別)
		private Button _leaveRoomButton;  // ★追加: ルーム離脱ボタン
		private Button _searchLobbyButton;
		private Button _searchRoomButton; // ★追加: ルーム検索ボタン
		private Button _sendDataButton;
		private Button _disconnectButton;
		private Button _shutdownButton;

		private TextMeshProUGUI _statusText;
		private TextMeshProUGUI _accountIdText;
		private TextMeshProUGUI _nicknameText;
		private TextMeshProUGUI _stationIdText;
		private TextMeshProUGUI _receivedDataText;
		private TextMeshProUGUI _connectedPlayersText;
		private TextMeshProUGUI _lobbyListText;
		private TextMeshProUGUI _roomListText; // ★追加: ルームリスト表示用

		private void Awake()
		{
			if (_network == null)
			{
				_network = FindObjectOfType<Network.Network>();
				if (_network == null)
				{
					Debug.LogError("NetworkTestUI: シーンに Network MonoBehaviour が見つかりません。");
					return;
				}
			}

			// Canvasの作成または取得
			if (_canvas == null)
			{
				_canvas = FindObjectOfType<Canvas>();
				if (_canvas == null)
				{
					GameObject canvasObj = new GameObject("Canvas");
					_canvas = canvasObj.AddComponent<Canvas>();
					_canvas.renderMode = RenderMode.ScreenSpaceOverlay;
					canvasObj.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
					canvasObj.AddComponent<GraphicRaycaster>();
					Debug.Log("NetworkTestUI: シーンに Canvas が見つからなかったため、新しく作成しました。");
				}
			}

			// メインUIパネルの作成または取得
			if (_uiPanelRect == null)
			{
				GameObject panelObj = new GameObject("UIPanel");
				_uiPanelRect = panelObj.AddComponent<RectTransform>();
				_uiPanelRect.SetParent(_canvas.transform, false);
				_uiPanelRect.anchorMin = new Vector2(0, 0);
				_uiPanelRect.anchorMax = new Vector2(1, 1);
				_uiPanelRect.offsetMin = new Vector2(10, 10); // 左下からのオフセット
				_uiPanelRect.offsetMax = new Vector2(-10, -10); // 右上からのオフセット

				// メインパネルにHorizontalLayoutGroupを追加
				HorizontalLayoutGroup mainLayoutGroup = panelObj.AddComponent<HorizontalLayoutGroup>();
				mainLayoutGroup.padding = new RectOffset(10, 10, 10, 10);
				mainLayoutGroup.spacing = 10; // セクション間のスペース
				mainLayoutGroup.childAlignment = TextAnchor.UpperLeft;
				mainLayoutGroup.childControlHeight = false;
				mainLayoutGroup.childForceExpandHeight = false;
				mainLayoutGroup.childForceExpandWidth = true;

				panelObj.AddComponent<Image>().color = new Color(0, 0, 0, 0.5f); // 背景に半透明の黒
			}

			// 各UI要素の親Transformを作成し、レイアウトグループを設定
			_uiInputParent = CreateSectionPanel("InputSection", _uiPanelRect);
			_uiButtonParent = CreateSectionPanel("MainButtonsSection", _uiPanelRect);
			_uiLobbyRoomButtonsParent = CreateSectionPanel("LobbyRoomButtonsSection", _uiPanelRect); // 新しいセクション
			_uiStatusParent = CreateSectionPanel("StatusSection", _uiPanelRect);


			BuildUI(); // UI要素を動的に作成

			// UIボタンのイベントリスナーを登録
			_initializeButton.onClick.AddListener(() => InitializeNetwork().Forget());
			_connectButton.onClick.AddListener(() => ConnectNetwork().Forget());
			_createLobbyButton.onClick.AddListener(() => CreateLobby().Forget());
			_joinLobbyButton.onClick.AddListener(() => JoinLobby().Forget());
			_leaveLobbyButton.onClick.AddListener(() => LeaveLobby().Forget());
			_createRoomButton.onClick.AddListener(() => CreateRoom().Forget());
			_joinRoomButton.onClick.AddListener(() => JoinRoom().Forget());
			_leaveRoomButton.onClick.AddListener(() => LeaveRoom().Forget());
			_searchLobbyButton.onClick.AddListener(() => SearchLobby().Forget());
			_searchRoomButton.onClick.AddListener(() => SearchRoom().Forget()); // ★追加
			_sendDataButton.onClick.AddListener(() => SendData().Forget());
			_disconnectButton.onClick.AddListener(() => DisconnectNetwork().Forget());
			_shutdownButton.onClick.AddListener(() => ShutdownNetwork().Forget());

			// Network イベントの購読
			_network.OnNetworkConnectionStatusChanged += UpdateConnectionStatusUI;
			_network.OnHostStatusChanged += UpdateHostStatusUI;
			_network.OnPlayerConnected += HandlePlayerConnected;
			_network.OnPlayerDisconnected += HandlePlayerDisconnected;
			_network.OnDataReceived += HandleDataReceived;
			_network.OnLobbyOperationCompleted += HandleLobbyOperationCompleted;
			_network.OnRoomOperationCompleted += HandleRoomOperationCompleted;

			UpdateUI(); // 初期UI更新

			// LayoutRebuilder.ForceRebuildLayoutImmediate(_uiPanelRect);
			Canvas.ForceUpdateCanvases();
		}

		private void Update()
		{
			if(UnityEngine.Input.GetKeyUp(KeyCode.R))
			{
				// Rキーが押されたらUIを再構築
				LayoutRebuilder.MarkLayoutForRebuild(_uiPanelRect);
			}
		}

		private void OnDestroy()
		{
			// イベントリスナーの解除
			if (_network != null)
			{
				_network.OnNetworkConnectionStatusChanged -= UpdateConnectionStatusUI;
				_network.OnHostStatusChanged -= UpdateHostStatusUI;
				_network.OnPlayerConnected -= HandlePlayerConnected;
				_network.OnPlayerDisconnected -= HandlePlayerDisconnected;
				_network.OnDataReceived -= HandleDataReceived;
				_network.OnLobbyOperationCompleted -= HandleLobbyOperationCompleted;
				_network.OnRoomOperationCompleted -= HandleRoomOperationCompleted;
			}

			// 動的に生成されたUI要素のイベントリスナーも解除
			if (_initializeButton != null) _initializeButton.onClick.RemoveAllListeners();
			if (_connectButton != null) _connectButton.onClick.RemoveAllListeners();
			if (_createLobbyButton != null) _createLobbyButton.onClick.RemoveAllListeners();
			if (_joinLobbyButton != null) _joinLobbyButton.onClick.RemoveAllListeners();
			if (_leaveLobbyButton != null) _leaveLobbyButton.onClick.RemoveAllListeners();
			if (_createRoomButton != null) _createRoomButton.onClick.RemoveAllListeners();
			if (_joinRoomButton != null) _joinRoomButton.onClick.RemoveAllListeners();
			if (_leaveRoomButton != null) _leaveRoomButton.onClick.RemoveAllListeners();
			if (_searchLobbyButton != null) _searchLobbyButton.onClick.RemoveAllListeners();
			if (_searchRoomButton != null) _searchRoomButton.onClick.RemoveAllListeners(); // ★追加
			if (_sendDataButton != null) _sendDataButton.onClick.RemoveAllListeners();
			if (_disconnectButton != null) _disconnectButton.onClick.RemoveAllListeners();
			if (_shutdownButton != null) _shutdownButton.onClick.RemoveAllListeners();
		}

		/// <summary>
		/// 各UIセクションの親パネルを作成します。
		/// </summary>
		private Transform CreateSectionPanel(string name, RectTransform parent)
		{
			GameObject panelObj = new GameObject(name);
			RectTransform rectTransform = panelObj.AddComponent<RectTransform>();
			rectTransform.SetParent(parent, false);
			rectTransform.sizeDelta = new Vector2(0, 0); // ContentSizeFitterで調整

			VerticalLayoutGroup layoutGroup = panelObj.AddComponent<VerticalLayoutGroup>();
			layoutGroup.padding = new RectOffset(5, 5, 5, 5);
			layoutGroup.spacing = 20;
			layoutGroup.childAlignment = TextAnchor.UpperLeft;
			layoutGroup.childForceExpandHeight = false;
			layoutGroup.childForceExpandWidth = true;

			panelObj.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
			return panelObj.transform;
		}

		/// <summary>
		/// UI要素を動的に作成し、配置します。
		/// </summary>
		private void BuildUI()
		{
			// Input Fields
			if (_dropdownPrefab == null)
			{
				Debug.LogError("NetworkTestUI: Dropdown Prefab が Inspector で割り当てられていません。");
				// 必要であれば、ここで処理を中断するか、デフォルトの動的生成ロジックにフォールバックします。
				// この例ではエラーログのみとします。
			}

			CreateText("NetworkHandlerLabel", "ネットワークハンドラ:", TextAlignmentOptions.MidlineLeft, 18, _uiInputParent, true);
			_networkHandlerDropdown = CreateDropdown("NetworkHandlerDropdown", new List<string> { "Dummy", "Netcode" }, _uiInputParent); // ★追加: ドロップダウン作成

			CreateText("UserNameLabel", "ユーザー名:", TextAlignmentOptions.MidlineLeft, 18, _uiInputParent, true);
			_userNameInputField = CreateInputField("UserNameInput", "ユーザー名を入力", "Player1", _uiInputParent);
			CreateText("LobbyRoomIdLabel", "ロビー/ルームID:", TextAlignmentOptions.MidlineLeft, 18, _uiInputParent, true);

			_lobbyRoomIdInputField = CreateInputField("LobbyRoomIdInput", "ロビー/ルームIDまたは検索クエリ", "MyLobby", _uiInputParent);
			CreateText("SendDataLabel", "送信データ:", TextAlignmentOptions.MidlineLeft, 18, _uiInputParent, true);
			_sendDataInputField = CreateInputField("SendDataInput", "Hello World!", "Hello World!", _uiInputParent); // PlaceholderとDefaultTextを修正

			// Main Buttons
			_initializeButton = CreateButton("InitializeButton", "Initialize Network", _uiButtonParent);
			_connectButton = CreateButton("ConnectButton", "Connect Network", _uiButtonParent);
			_disconnectButton = CreateButton("DisconnectButton", "Disconnect Network", _uiButtonParent);
			_shutdownButton = CreateButton("ShutdownButton", "Shutdown Network", _uiButtonParent);
			_sendDataButton = CreateButton("SendDataButton", "Send Data", _uiButtonParent);

			// Lobby/Room Buttons
			_createLobbyButton = CreateButton("CreateLobbyButton", "Create Lobby", _uiLobbyRoomButtonsParent);
			_joinLobbyButton = CreateButton("JoinLobbyButton", "Join Lobby", _uiLobbyRoomButtonsParent);
			_leaveLobbyButton = CreateButton("LeaveLobbyButton", "Leave Lobby", _uiLobbyRoomButtonsParent);
			_createRoomButton = CreateButton("CreateRoomButton", "Create Room", _uiLobbyRoomButtonsParent);
			_joinRoomButton = CreateButton("JoinRoomButton", "Join Room", _uiLobbyRoomButtonsParent);
			_leaveRoomButton = CreateButton("LeaveRoomButton", "Leave Room", _uiLobbyRoomButtonsParent);
			_searchLobbyButton = CreateButton("SearchLobbyButton", "Search Lobbies", _uiLobbyRoomButtonsParent);
			_searchRoomButton = CreateButton("SearchRoomButton", "Search Rooms", _uiLobbyRoomButtonsParent); // ★追加

			// Status Texts
			_statusText = CreateText("StatusText", "Status: N/A", TextAlignmentOptions.TopLeft, 20, _uiStatusParent);
			_accountIdText = CreateText("AccountIdText", "Account ID: N/A", TextAlignmentOptions.TopLeft, 18, _uiStatusParent);
			_nicknameText = CreateText("NicknameText", "Nickname: N/A", TextAlignmentOptions.TopLeft, 18, _uiStatusParent);
			_stationIdText = CreateText("StationIdText", "Station ID: N/A", TextAlignmentOptions.TopLeft, 18, _uiStatusParent);
			_receivedDataText = CreateText("ReceivedDataText", "Received Data: ", TextAlignmentOptions.TopLeft, 18, _uiStatusParent);
			_connectedPlayersText = CreateText("ConnectedPlayersText", "Connected Players:\n", TextAlignmentOptions.TopLeft, 18, _uiStatusParent);
			_lobbyListText = CreateText("LobbyListText", "Found Lobbies:\n", TextAlignmentOptions.TopLeft, 18, _uiStatusParent);
			_roomListText = CreateText("RoomListText", "Found Rooms:\n", TextAlignmentOptions.TopLeft, 18, _uiStatusParent);
		}

		/// <summary>
		/// 動的に TMP_InputField を作成します。
		/// </summary>
		private TMP_InputField CreateInputField(string name, string placeholderText, string defaultText, Transform parent)
		{
			GameObject inputFieldObj = new GameObject(name);
			inputFieldObj.transform.SetParent(parent, false);

			// RectTransform を明示的に追加する（親がRectTransformでも念のため）
			RectTransform rectTransform = inputFieldObj.AddComponent<RectTransform>();
			rectTransform.sizeDelta = new Vector2(0, 30); // 高さ固定

			TMP_InputField inputField = inputFieldObj.AddComponent<TMP_InputField>();
			LayoutElement layoutElement = inputFieldObj.AddComponent<LayoutElement>();
			layoutElement.preferredHeight = 30; // レイアウトグループでの高さ

			// 背景画像を追加 (Optional)
			Image bgImage = inputFieldObj.AddComponent<Image>();
			bgImage.color = new Color(0.1f, 0.1f, 0.1f, 0.8f);

			// Text Component (Placeholder)
			GameObject placeholderObj = new GameObject("Placeholder");
			placeholderObj.transform.SetParent(inputField.transform, false);
			TextMeshProUGUI placeholder = placeholderObj.AddComponent<TextMeshProUGUI>();
			placeholder.rectTransform.anchorMin = Vector2.zero;
			placeholder.rectTransform.anchorMax = Vector2.one;
			placeholder.rectTransform.offsetMin = new Vector2(5, 0);
			placeholder.rectTransform.offsetMax = new Vector2(-5, 0);
			placeholder.text = placeholderText;
			placeholder.color = new Color(0.7f, 0.7f, 0.7f, 0.5f);
			placeholder.fontSize = 18;
			placeholder.alignment = TextAlignmentOptions.MidlineLeft;
			inputField.placeholder = placeholder;

			// Text Component (Input Text)
			GameObject textObj = new GameObject("Text");
			textObj.transform.SetParent(inputField.transform, false);
			TextMeshProUGUI inputText = textObj.AddComponent<TextMeshProUGUI>();
			inputText.rectTransform.anchorMin = Vector2.zero;
			inputText.rectTransform.anchorMax = Vector2.one;
			inputText.rectTransform.offsetMin = new Vector2(5, 0);
			inputText.rectTransform.offsetMax = new Vector2(-5, 0);
			inputText.color = Color.white;
			inputText.fontSize = 18;
			inputText.alignment = TextAlignmentOptions.MidlineLeft;
			inputField.textComponent = inputText;

			inputField.text = defaultText;

			return inputField;
		}

		/// <summary>
		/// 動的に Button を作成します。
		/// </summary>
		private Button CreateButton(string name, string buttonText, Transform parent)
		{
			GameObject buttonObj = new GameObject(name);
			buttonObj.transform.SetParent(parent, false);

			// RectTransform を明示的に追加する
			RectTransform rectTransform = buttonObj.AddComponent<RectTransform>();
			rectTransform.sizeDelta = new Vector2(0, 40); // 高さ固定

			Button button = buttonObj.AddComponent<Button>();
			LayoutElement layoutElement = buttonObj.AddComponent<LayoutElement>();
			layoutElement.preferredHeight = 40; // レイアウトグループでの高さ

			// 背景画像を追加 (Optional)
			Image bgImage = buttonObj.AddComponent<Image>();
			bgImage.color = new Color(0.2f, 0.5f, 0.8f, 0.9f); // 青系の色

			// Text Component
			GameObject textObj = new GameObject("Text");
			textObj.transform.SetParent(button.transform, false);
			TextMeshProUGUI text = textObj.AddComponent<TextMeshProUGUI>();
			text.rectTransform.anchorMin = Vector2.zero;
			text.rectTransform.anchorMax = Vector2.one;
			text.text = buttonText;
			text.color = Color.white;
			text.fontSize = 20;
			text.alignment = TextAlignmentOptions.Center;

			return button;
		}

		/// <summary>
		/// 動的に TMP_Dropdown を作成します。
		/// </summary>
		private TMP_Dropdown CreateDropdown(string name, List<string> optionStrings, Transform parent)
		{
			if (_dropdownPrefab == null)
			{
				Debug.LogError("NetworkTestUI: Dropdown Prefab が設定されていません。CreateDropdown を実行できません。");
				// フォールバックとして空のGameObjectにTMP_Dropdownを無理やりつけることもできるが、
				// プレハブがないと見た目が整わないため、エラーを返してnullを返すのが無難。
				GameObject errorObj = new GameObject(name + "_ErrorNoPrefab");
				errorObj.transform.SetParent(parent, false);
				errorObj.AddComponent<RectTransform>().sizeDelta = new Vector2(200,30); // 仮サイズ
				var errorText = errorObj.AddComponent<TextMeshProUGUI>();
				errorText.text = "Dropdown Prefab Missing";
				errorText.color = Color.red;
				return null; 
			}

			TMP_Dropdown dropdownInstance = Instantiate(_dropdownPrefab, parent);
			dropdownInstance.name = name;
			dropdownInstance.gameObject.SetActive(true); // ★ コピーしたインスタンスを有効化

			// LayoutElement の設定 (プレハブに既にあれば不要な場合もあるが、コードで制御するならここで)
			LayoutElement layoutElement = dropdownInstance.GetComponent<LayoutElement>();
			if (layoutElement == null)
			{
				layoutElement = dropdownInstance.gameObject.AddComponent<LayoutElement>();
			}
			layoutElement.preferredHeight = 30; // またはプレハブの設定に合わせる

			// RectTransform の調整 (親に追従させるなど、必要に応じて)
			// dropdownInstance.GetComponent<RectTransform>().sizeDelta = new Vector2(0, 30); // これはLayoutElementと競合する可能性あり

			// Options
			dropdownInstance.ClearOptions(); // 既存のオプションをクリア (プレハブにテスト用オプションが入っている場合など)
			List<TMP_Dropdown.OptionData> options = new List<TMP_Dropdown.OptionData>();
			foreach (string optionStr in optionStrings)
			{
				options.Add(new TMP_Dropdown.OptionData(optionStr));
			}
			dropdownInstance.options = options;
			dropdownInstance.RefreshShownValue(); // 最初の値を表示に反映

			return dropdownInstance;
		}
		/// <summary>
		/// 動的に TextMeshProUGUI を作成します。
		/// </summary>
		private TextMeshProUGUI CreateText(string name, string initialText, TextAlignmentOptions alignment, float fontSize, Transform parent, bool isSingleLineLabel = false)
		{
			GameObject textObj = new GameObject(name);
			textObj.transform.SetParent(parent, false);

			// RectTransform を明示的に追加する
			RectTransform rectTransform = textObj.AddComponent<RectTransform>();
			// LayoutElementがサイズを制御するため、sizeDeltaは(0,0)で良い
			rectTransform.sizeDelta = Vector2.zero;

			TextMeshProUGUI text = textObj.AddComponent<TextMeshProUGUI>();
			LayoutElement layoutElement = textObj.AddComponent<LayoutElement>();

			if (isSingleLineLabel)
			{
				layoutElement.preferredHeight = fontSize + 10f; // ラベル用の適切な高さ (パディング込み)
				layoutElement.preferredWidth = 150; // ★修正: minWidthからpreferredWidthへ移動
				// layoutElement.minWidth = 150; // 削除
				text.enableWordWrapping = false; // 折り返しなし
				text.overflowMode = TextOverflowModes.Ellipsis; // はみ出した場合は省略記号
			}
			else
			{
				layoutElement.preferredHeight = fontSize * 5.0f; // 複数行テキスト用の高さ
				layoutElement.preferredWidth = 700; // ★修正: minWidthからpreferredWidthへ移動
				// layoutElement.minWidth = 700; // 削除
				text.enableWordWrapping = true; // 折り返しあり
			}
			// 必要に応じて layoutElement.minWidth = 0; など、適切な最小幅を設定することも検討してください。
			text.text = initialText;
			text.color = Color.white;
			text.fontSize = fontSize;
			text.alignment = alignment;

			return text;
		}

		/// <summary>
		/// UIの表示を更新します。
		/// </summary>
		private void UpdateUI()
		{
			if (_statusText != null) _statusText.text = $"Status: {(_network.IsConnected ? "Connected" : "Disconnected")} | {(_network.IsHost ? "Host" : "Client")}";
			if (_accountIdText != null) _accountIdText.text = $"Account ID: {_network.AccountId ?? "N/A"}";
			if (_nicknameText != null) _nicknameText.text = $"Nickname: {_network.NickName ?? "N/A"}";
			if (_stationIdText != null) _stationIdText.text = $"Station ID: {_network.StationId ?? "N/A"}";
			UpdateConnectedPlayersUI();
		}

		private void UpdateConnectionStatusUI(bool isConnected)
		{
			UpdateUI();
			Debug.Log($"NetworkTestUI: 接続状態が変更されました: {isConnected}");
		}

		private void UpdateHostStatusUI(bool isHost)
		{
			UpdateUI();
			Debug.Log($"NetworkTestUI: ホスト状態が変更されました: {isHost}");
		}

		private void HandlePlayerConnected(string playerId, string playerName)
		{
			Debug.Log($"NetworkTestUI: プレイヤー接続: {playerName} ({playerId})");
			UpdateConnectedPlayersUI();
		}

		private void HandlePlayerDisconnected(string playerId, string playerName)
		{
			Debug.Log($"NetworkTestUI: プレイヤー切断: {playerName} ({playerId})");
			UpdateConnectedPlayersUI();
		}

		private void HandleDataReceived(byte[] data)
		{
			string receivedMessage = Encoding.UTF8.GetString(data);
			if (_receivedDataText != null) _receivedDataText.text = $"Received: {receivedMessage}";
			Debug.Log($"NetworkTestUI: データ受信: {receivedMessage}");
		}

		private void HandleLobbyOperationCompleted(string operation, bool success, string message)
		{
			Debug.Log($"NetworkTestUI: ロビー操作 {operation} 完了: {(success ? "成功" : "失敗")} - {message}");
			if (_statusText != null) _statusText.text = $"Status: {operation} {(success ? "Success" : "Failed")} - {message}";
			UpdateUI();
		}

		private void HandleRoomOperationCompleted(string operation, bool success, string message)
		{
			Debug.Log($"NetworkTestUI: ルーム操作 {operation} 完了: {(success ? "成功" : "失敗")} - {message}");
			if (_statusText != null) _statusText.text = $"Status: {operation} {(success ? "Success" : "Failed")} - {message}";
			UpdateUI();
		}

		private void UpdateConnectedPlayersUI()
		{
			if (_connectedPlayersText == null) return;

			StringBuilder sb = new StringBuilder("Connected Players:\n");
			if (_network != null && _network.ConnectedList != null)
			{
				foreach (var player in _network.ConnectedList)
				{
					sb.AppendLine($"- {player}");
				}
			}
			_connectedPlayersText.text = sb.ToString();
		}

		/// <summary>
		/// Network ライブラリを初期化します。
		/// </summary>
		private async UniTask InitializeNetwork()
		{
			if (_statusText != null) _statusText.text = "Initializing Network...";

			// --- ここから修正 ---
			// ドロップダウンから選択されたハンドラ名を取得
			string selectedHandlerName = _networkHandlerDropdown.options[_networkHandlerDropdown.value].text;
			Network.IInternalNetworkHandler selectedHandler;

			switch (selectedHandlerName)
			{
				case "Dummy":
					selectedHandler = new Network.DummyNetworkHandler.DummyNetworkHandler();
					Debug.Log("NetworkTestUI: Using DummyNetworkHandler for initialization.");
					break;
				case "Netcode":
					selectedHandler = new Network.NetcodeNetworkHandler.NetcodeNetworkHandler();
					Debug.Log("NetworkTestUI: Using NetcodeNetworkHandler for initialization.");
					break;
				default:
					Debug.LogError($"NetworkTestUI: Unknown network handler selected: {selectedHandlerName}");
					if (_statusText != null) _statusText.text = $"Unknown handler: {selectedHandlerName}";
					return;
			}
			// --- ここまで修正 ---

			bool success = await _network.InitializeLibrary(selectedHandler); // ★ 修正: handlerを渡す
			if (success)
			{
				if (_statusText != null) _statusText.text = "Network Initialized Successfully.";
			}
			else
			{
				if (_statusText != null) _statusText.text = "Network Initialization Failed.";
			}
			UpdateUI();
		}

		/// <summary>
		/// ネットワークに接続します。
		/// </summary>
		private async UniTask ConnectNetwork()
		{
			string userName = _userNameInputField?.text;
			if (string.IsNullOrEmpty(userName))
			{
				if (_statusText != null) _statusText.text = "Please enter a username.";
				return;
			}

			if (_statusText != null) _statusText.text = $"Connecting as {userName}...";
			bool success = await _network.ConnectNetwork(_network.AccountId ?? System.Guid.NewGuid().ToString(), userName); // AccountIdがなければ生成
			if (success)
			{
				if (_statusText != null) _statusText.text = $"Connected as {userName}.";
			}
			else
			{
				if (_statusText != null) _statusText.text = "Connection Failed.";
			}
			UpdateUI();
		}

		/// <summary>
		/// ロビーを作成します。
		/// </summary>
		private async UniTask CreateLobby()
		{
			string lobbyName = _lobbyRoomIdInputField?.text;
			if (string.IsNullOrEmpty(lobbyName))
			{
				if (_statusText != null) _statusText.text = "Please enter a lobby name.";
				return;
			}

			if (_statusText != null) _statusText.text = $"Creating Lobby '{lobbyName}'...";
			bool success = await _network.CreateLobby(lobbyName);
			if (success)
			{
				if (_statusText != null) _statusText.text = $"Lobby '{lobbyName}' Created.";
			}
			else
			{
				if (_statusText != null) _statusText.text = "Lobby Creation Failed.";
			}
			UpdateUI();
		}

		/// <summary>
		/// ロビーに参加します。
		/// </summary>
		private async UniTask JoinLobby()
		{
			string lobbyId = _lobbyRoomIdInputField?.text;
			if (string.IsNullOrEmpty(lobbyId))
			{
				if (_statusText != null) _statusText.text = "Please enter a lobby ID or Code.";
				return;
			}

			if (_statusText != null) _statusText.text = $"Joining Lobby '{lobbyId}'...";
			bool success = await _network.ConnectLobby(lobbyId);
			if (success)
			{
				if (_statusText != null) _statusText.text = $"Joined Lobby '{lobbyId}'.";
			}
			else
			{
				if (_statusText != null) _statusText.text = "Lobby Joining Failed.";
			}
			UpdateUI();
		}

		/// <summary>
		/// ロビーから離脱します。
		/// </summary>
		private async UniTask LeaveLobby()
		{
			if (_statusText != null) _statusText.text = "Leaving Lobby...";
			await _network.DisconnectLobby();
			if (_statusText != null) _statusText.text = "Lobby Left.";
			UpdateUI();
		}

		/// <summary>
		/// ルームを作成します。
		/// </summary>
		private async UniTask CreateRoom()
		{
			string roomName = _lobbyRoomIdInputField?.text;
			if (string.IsNullOrEmpty(roomName))
			{
				if (_statusText != null) _statusText.text = "Please enter a room name.";
				return;
			}

			// 仮の最大プレイヤー数
			int maxPlayers = 4;
			if (_statusText != null) _statusText.text = $"Creating Room '{roomName}' (Max Players: {maxPlayers})...";
			var settings = _network.PrepareRoomSettings(); // ルーム設定を取得
			settings.MaxPlayers = maxPlayers; // 最大プレイヤー数を設定
			bool success = await _network.CreateRoom(roomName);
			if (success)
			{
				if (_statusText != null) _statusText.text = $"Room '{roomName}' Created.";
			}
			else
			{
				if (_statusText != null) _statusText.text = "Room Creation Failed.";
			}
			UpdateUI();
		}

		/// <summary>
		/// ルームに参加します。
		/// </summary>
		private async UniTask JoinRoom()
		{
			string roomId = _lobbyRoomIdInputField?.text;
			if (string.IsNullOrEmpty(roomId))
			{
				if (_statusText != null) _statusText.text = "Please enter a room ID.";
				return;
			}

			if (_statusText != null) _statusText.text = $"Joining Room '{roomId}'...";
			bool success = await _network.ConnectRoom(roomId);
			if (success)
			{
				if (_statusText != null) _statusText.text = $"Joined Room '{roomId}'.";
			}
			else
			{
				if (_statusText != null) _statusText.text = "Room Joining Failed.";
			}
			UpdateUI();
		}

		/// <summary>
		/// ルームから離脱します。
		/// </summary>
		private async UniTask LeaveRoom()
		{
			if (_statusText != null) _statusText.text = "Leaving Room...";
			await _network.DisconnectRoom();
			if (_statusText != null) _statusText.text = "Room Left.";
			UpdateUI();
		}

		/// <summary>
		/// ロビーを検索します。
		/// </summary>
		private async UniTask SearchLobby()
		{
			string query = _lobbyRoomIdInputField?.text; // 検索クエリとして使用
			if (string.IsNullOrEmpty(query))
			{
				if (_statusText != null) _statusText.text = "Please enter a search query for lobbies.";
				return;
			}

			if (_statusText != null) _statusText.text = $"Searching Lobbies with query '{query}'...";
			List<string> lobbies = await _network.SearchLobby(query);

			StringBuilder sb = new StringBuilder("Found Lobbies:\n");
			if (lobbies.Count > 0)
			{
				foreach (var lobby in lobbies)
				{
					sb.AppendLine($"- {lobby}");
				}
			}
			else
			{
				sb.AppendLine("No lobbies found.");
			}
			if (_lobbyListText != null) _lobbyListText.text = sb.ToString();
			if (_statusText != null) _statusText.text = $"Lobby Search Completed. Found {lobbies.Count} lobbies.";
		}

		/// <summary>
		/// ルームを検索します。
		/// </summary>
		private async UniTask SearchRoom() // ★追加
		{
			string query = _lobbyRoomIdInputField?.text; // 検索クエリとして使用
			if (string.IsNullOrEmpty(query))
			{
				if (_statusText != null) _statusText.text = "Please enter a search query for rooms.";
				return;
			}

			if (_statusText != null) _statusText.text = $"Searching Rooms with query '{query}'...";
			List<string> rooms = await _network.SearchRoom(query);

			StringBuilder sb = new StringBuilder("Found Rooms:\n");
			if (rooms.Count > 0)
			{
				foreach (var room in rooms)
				{
					sb.AppendLine($"- {room}");
				}
			}
			else
			{
				sb.AppendLine("No rooms found.");
			}
			if (_roomListText != null) _roomListText.text = sb.ToString();
			if (_statusText != null) _statusText.text = $"Room Search Completed. Found {rooms.Count} rooms.";
		}

		/// <summary>
		/// データを送信します。
		/// </summary>
		private async UniTask SendData()
		{
			string message = _sendDataInputField?.text;
			if (string.IsNullOrEmpty(message))
			{
				if (_statusText != null) _statusText.text = "Please enter data to send.";
				return;
			}

			byte[] data = Encoding.UTF8.GetBytes(message);
			if (_statusText != null) _statusText.text = $"Sending Data: '{message}'...";
			await _network.SendData(data);
			if (_statusText != null) _statusText.text = $"Data Sent: '{message}'.";
			if (_sendDataInputField != null) _sendDataInputField.text = ""; // 送信後クリア
		}

		/// <summary>
		/// ネットワークから切断します。
		/// </summary>
		private async UniTask DisconnectNetwork()
		{
			if (_statusText != null) _statusText.text = "Disconnecting Network...";
			await _network.DisconnectNetwork();
			if (_statusText != null) _statusText.text = "Network Disconnected.";
			if (_receivedDataText != null) _receivedDataText.text = "Received: "; // 受信データをクリア
			if (_lobbyListText != null) _lobbyListText.text = "Found Lobbies:\n"; // ロビーリストをクリア
			if (_roomListText != null) _roomListText.text = "Found Rooms:\n";     // ルームリストをクリア
			UpdateUI();
		}

		/// <summary>
		/// Network ライブラリをシャットダウンします。
		/// </summary>
		private async UniTask ShutdownNetwork()
		{
			if (_statusText != null) _statusText.text = "Shutting down Network Library...";
			await _network.ShutdownLibrary();
			if (_statusText != null) _statusText.text = "Network Library Shut Down.";
			if (_receivedDataText != null) _receivedDataText.text = "Received: "; // 受信データをクリア
			if (_lobbyListText != null) _lobbyListText.text = "Found Lobbies:\n"; // ロビーリストをクリア
			if (_roomListText != null) _roomListText.text = "Found Rooms:\n";     // ルームリストをクリア
			UpdateUI();
		}
	}
}
