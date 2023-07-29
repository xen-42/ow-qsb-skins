using HarmonyLib;
using QSB.Animation.Player.Messages;
using QSB.Player;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
			// Relevant for enabling/disabling special Chert behaviour
			QSBSkins.Instance.RefreshPlayerSkin(player);
		}
	}
}
