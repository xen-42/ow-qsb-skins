using QSB.PlayerBodySetup.Remote;
using System.Reflection;
using UnityEngine;

namespace QSBSkins;

/// <summary>
/// Accesses QSB values/methods using reflection
/// </summary>
public static class QSBHelper
{
	public static GameObject GetPlayerPrefab()
	{
		return typeof(RemotePlayerCreation).GetMethod("GetPrefab", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, null) as GameObject;
	}

	private static IQSBAPI _api;
	public static IQSBAPI API => _api ??= QSBSkins.Instance.ModHelper.Interaction.TryGetModApi<IQSBAPI>("Raicuparta.QuantumSpaceBuddies");
}
