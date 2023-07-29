﻿using OWML.Common;
using OWML.ModHelper;
using QSB.Messaging;
using QSB.Player;
using System.Collections.Generic;
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
				Delay.FireInNUpdates(() => ChangePlayerSkin(QSBPlayerManager.LocalPlayer, LocalSkin), 30);
			}
		}

		private void OnPlayerAdded(PlayerInfo player)
		{
			// Send them info about our skin
			new ChangeSkinMessage(LocalSkin) { To = player.PlayerId }.Send();
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

			if (player.IsLocalPlayer)
			{
				new ChangeSkinMessage(skinName).Send();
			}

			var mesh = SkinReplacer.ReplaceSkin(player.Body, skinName, !player.IsLocalPlayer, true);
			_skins[player.PlayerId] = (skinName, mesh);
		}
	}
}