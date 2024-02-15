using HarmonyLib;
using OWML.Common;
using OWML.ModHelper;
using QSB;
using QSB.Animation.Player;
using QSB.Player;
using QSB.WorldSync;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace QSBSkins
{
	public class QSBSkins : ModBehaviour
	{
		public static QSBSkins Instance { get; private set; }

		private readonly Dictionary<uint, (string skinName, SkinnedMeshRenderer[] currentMesh)> _skins = new();

		public string LocalSkin { get; private set; } = SkinReplacer.PROTAGONIST;

		public static string ChangeSkinMessage => nameof(ChangeSkinMessage);

		public void Awake()
		{
			Instance = this;
		}

		public void Start()
		{
			Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly());

			_skins.Clear();
			LoadManager.OnCompleteSceneLoad += OnCompleteSceneLoad;
			QSBPlayerManager.OnAddPlayer += OnPlayerAdded;

			QSBHelper.API.RegisterHandler<string>(ChangeSkinMessage, OnReceiveChangeSkinMessage);
			QSBCore.RegisterNotRequiredForAllPlayers(this);
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
				() => SendChangeSkinMessage(LocalSkin, to: player.PlayerId)
			);
		}

		public void RefreshRemotePlayerHeadSync(PlayerInfo player, string skinName = null)
		{
			if (string.IsNullOrEmpty(skinName) && _skins.TryGetValue(player.PlayerId, out var skinInfo))
			{
				skinName = skinInfo.skinName;
			}

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

		public void OnReceiveChangeSkinMessage(uint From, string Data)
		{
			Delay.RunWhen(
			   () => QSBPlayerManager.GetPlayer(From).Body != null,
			   () => Instance.ChangePlayerSkin(QSBPlayerManager.GetPlayer(From), Data)
			);
		}

		public static void SendChangeSkinMessage(string skin, uint to = uint.MaxValue)
		{
			QSBHelper.API.SendMessage(ChangeSkinMessage, skin, to: to);
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
				// Immediately tell all other clients to alter our skin
				SendChangeSkinMessage(skinName);
			}
			else
			{
				RefreshRemotePlayerHeadSync(player, skinName);
			}

			var mesh = SkinReplacer.ReplaceSkin(player.Body, skinName, !player.IsLocalPlayer, true);
			_skins[player.PlayerId] = (skinName, mesh);
		}
	}
}