using QSB.Messaging;
using QSB.Player;

namespace QSBSkins;

public class ChangeSkinMessage : QSBMessage<string>
{
	public ChangeSkinMessage(string skin) : base(skin) { }

	public override void OnReceiveRemote()
	{
		if (From != QSBPlayerManager.LocalPlayerId)
		{
			QSBSkins.Instance.ChangePlayerSkin(QSBPlayerManager.GetPlayer(From), Data);
		}
	}
}
