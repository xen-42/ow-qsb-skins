using HarmonyLib;
using QSB.Animation.Player.Messages;
using QSB.Player;

namespace QSBSkins;

[HarmonyPatch(typeof(PlayerSuitMessage))]
public static class PlayerSuitMessagePatch
{
	[HarmonyPostfix]
	[HarmonyPatch(nameof(PlayerSuitMessage.OnReceiveRemote))]
	public static void PlayerSuitMessage_OnReceiveRemote(PlayerSuitMessage __instance)
	{
		var player = QSBPlayerManager.GetPlayer(__instance.From);
		if (player.IsReady)
		{
			QSBSkins.Instance.RefreshRemotePlayerHeadSync(player);
		}
	}
}
