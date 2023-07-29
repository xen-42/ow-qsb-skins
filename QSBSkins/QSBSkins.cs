using OWML.Common;
using OWML.ModHelper;
using QSB.Animation.Player;
using QSB.Messaging;
using QSB.Player;
using QSB.WorldSync;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace QSBSkins
{
	public class QSBSkins : ModBehaviour
	{
		public static QSBSkins Instance { get; private set; }

		private readonly Dictionary<uint, (string skinName, SkinnedMeshRenderer[] currentMesh)> _skins = new();

		public string LocalSkin { get; private set; } = SkinReplacer.PROTAGONIST;

		public void Awake()
		{
			Instance = this;
		}

		public void Start()
		{
			_skins.Clear();
			LoadManager.OnCompleteSceneLoad += OnCompleteSceneLoad;
			QSBPlayerManager.OnAddPlayer += OnPlayerAdded;
		}

		public void OnDestroy()
		{
			LoadManager.OnCompleteSceneLoad -= OnCompleteSceneLoad;
			QSBPlayerManager.OnAddPlayer -= OnPlayerAdded;
		}

		public override void Configure(IModConfig config)
		{
			base.Configure(config);

			LocalSkin = config.GetSettingsValue<string>("SuitSkin").ToUpperInvariant();

			var currentScene = SceneManager.GetActiveScene().name;
			if (currentScene == "SolarSystem" || currentScene == "EyeOfTheUniverse")
			{
				ChangePlayerSkin(QSBPlayerManager.LocalPlayer, LocalSkin);
			}
		}

		private void OnCompleteSceneLoad(OWScene originalScene, OWScene loadScene)
		{
			if (loadScene == OWScene.SolarSystem || loadScene == OWScene.EyeOfTheUniverse)
			{
				// Wait for QSB to finish connecting and syncing first
				Delay.RunWhen(
					() => QSBWorldSync.AllObjectsReady,
					() => ChangePlayerSkin(QSBPlayerManager.LocalPlayer, LocalSkin)
				);
			}
		}

		private void OnPlayerAdded(PlayerInfo player)
		{
			// Send them info about our skin
			// Make sure they've finished loading in first
			Delay.RunWhen(
				() => player.Body != null,
				() => new ChangeSkinMessage(LocalSkin) { To = player.PlayerId }.Send()
			);
		}

		public void RefreshPlayerSkin(PlayerInfo player)
		{
			var skinName = SkinReplacer.PROTAGONIST;
			if (_skins.TryGetValue(player.PlayerId, out var skin))
			{
				skinName = skin.skinName;
			}
			ChangePlayerSkin(player, skinName);
		}

		public void ChangePlayerSkin(PlayerInfo player, string skinName)
		{
			var previousSkin = string.Empty;

			// Revert old skin
			if (_skins.TryGetValue(player.PlayerId, out var skin) && skin.skinName != null)
			{
				if (skin.currentMesh != null)
				{
					foreach (var skinnedMeshRenderer in skin.currentMesh)
					{
						GameObject.Destroy(skinnedMeshRenderer);
					}
				}
				previousSkin = skin.skinName;
			}

			if (player.IsLocalPlayer)
			{
				// Immediately tell all other clients to alter our skin
				new ChangeSkinMessage(skinName).Send();
			}
			else
			{
				// For remote Chert players we do not want their head rotation to sync (looks really bad with that helmet)
				var headRotationSync = player.Body.GetComponentInChildren<PlayerHeadRotationSync>();

				if (skinName == SkinReplacer.CHERT && player.SuitedUp)
				{
					headRotationSync.enabled = false;
					headRotationSync.gameObject.GetComponent<Animator>().GetBoneTransform(HumanBodyBones.Head).localRotation = Quaternion.identity;
				}
				else
				{
					headRotationSync.enabled = true;
				}
			}

			var suitedMesh = SkinReplacer.ReplaceSkin(player.Body, LocalSkin, !player.IsLocalPlayer, true);
			_skins[player.PlayerId] = (skinName, suitedMesh);

			if (!string.IsNullOrEmpty(previousSkin))
			{
				// Jank cleanup of old skin
				try
				{
					var playerAnimationRoot = player.Body.GetComponentInChildren<PlayerAnimController>()?.gameObject ?? player.Body.GetComponentInChildren<PlayerHeadRotationSync>().gameObject;
					foreach (MeshRenderer renderer in playerAnimationRoot.GetComponentsInChildren<MeshRenderer>())
					{
						if (transform.name.IndexOf(skin.skinName, StringComparison.OrdinalIgnoreCase) >= 0)
						{
							GameObject.Destroy(transform.gameObject);
						}
					}
				}
				catch (Exception e)
				{
					DebugLogger.WriteError($"Couldn't delete remnants of old skin {skinName} on {player.Name}: {e}");
				}
			}
		}
	}
}