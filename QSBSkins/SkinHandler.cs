using QSB.Player;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace QSBSkins;

internal class SkinHandler : MonoBehaviour
{
	private readonly Dictionary<uint, (string skinName, SkinnedMeshRenderer[] currentMesh)> _skins = new();

	public void Awake()
	{
		_skins.Clear();
		QSBPlayerManager.OnAddPlayer += OnPlayerAdded;
		LoadManager.OnCompleteSceneLoad += OnCompleteSceneLoad;
	}

	public void OnDestroy()
	{
		QSBPlayerManager.OnAddPlayer -= OnPlayerAdded;
		LoadManager.OnCompleteSceneLoad -= OnCompleteSceneLoad;
	}

	private void OnCompleteSceneLoad(OWScene originalScene, OWScene loadScene)
	{
		if (loadScene == OWScene.SolarSystem || loadScene == OWScene.EyeOfTheUniverse)
		{
			Delay.FireInNUpdates(() => ChangePlayerSkin(QSBPlayerManager.LocalPlayer, "Chert"), 30);
		}
	}

	private void OnPlayerAdded(PlayerInfo info)
	{
		Delay.RunWhen(() => info.Body != null, () => ChangePlayerSkin(info, "Gabbro"));
	}

	public void ChangePlayerSkin(PlayerInfo player, string skinName)
	{
		if (_skins.TryGetValue(player.PlayerId, out var skin))
		{
			if (skin.skinName != skinName && skin.currentMesh != null)
			{
				foreach (var skinnedMeshRenderer in skin.currentMesh)
				{
					GameObject.Destroy(skinnedMeshRenderer);
				}
			}
		}

		if (skinName == "Protagonist")
		{
			SkinReplacer.ResetSkin(player.Body);
			_skins[player.PlayerId] = (skinName, null);
		}
		else
		{
			var mesh = SkinReplacer.ReplaceSkin(player.Body, skinName, player != QSBPlayerManager.LocalPlayer, true);
			_skins[player.PlayerId] = (skinName, mesh);
		}
	}
}
