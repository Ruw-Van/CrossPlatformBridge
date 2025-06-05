#if UNITY_SWITCH || (UNITY_STANDALONE_WIN && NN_PLUGIN_ENABLE)

using nn.account;

public static class NNAccount
{
	public static bool IsOpened { get; private set; }

	public static nn.account.UserHandle NXCurrentUserHandle => _NXCurrentUserHandle;
	private static nn.account.UserHandle _NXCurrentUserHandle;
	public static nn.account.Uid NXCurrentUid => _NXCurrentUid;
	private static nn.account.Uid _NXCurrentUid;
	public static nn.account.Nickname NXCurrentNickName => _NXCurrentNickName;
	private static nn.account.Nickname _NXCurrentNickName;
	private static nn.Result UserHandlingResult;

	static NNAccount()
	{
#if UNITY_EDITOR
		_NXCurrentUid = new Uid() { _data0 = 0x22580, _data1 = 0x10010010 };
		_NXCurrentUserHandle = new UserHandle() { _data0 = 0x22580, _data1 = 0x10010010, _context = 2 };
		_NXCurrentNickName = new Nickname() { name = System.DateTime.Now.ToString("HH:mm:ss") };
#endif
	}

	public static bool GetUserAccount()
	{
		if (IsOpened) return true;

		nn.account.Account.Initialize();
		if (!nn.account.Account.TryOpenPreselectedUser(ref _NXCurrentUserHandle))
		{
			return false;
		}
		UserHandlingResult = nn.account.Account.GetUserId(ref _NXCurrentUid, _NXCurrentUserHandle);
		if (!UserHandlingResult.IsSuccess())
		{
			return false;
		}
		UserHandlingResult = nn.account.Account.GetNickname(ref _NXCurrentNickName, _NXCurrentUid);
		if (!UserHandlingResult.IsSuccess())
		{
			return false;
		}

		IsOpened = true;
		return true;
	}

	public static void CloseUserAccount()
	{
		if (!IsOpened) return;
		nn.account.Account.CloseUser(_NXCurrentUserHandle);
		IsOpened = false;
	}
}
#endif
