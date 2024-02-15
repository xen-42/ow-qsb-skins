using QSB.Animation.Player;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace QSBSkins;

/// <summary>
/// Taken from my skin implementation for OWO which was cannibalized from Half Life Overhaul
/// </summary>
public static class SkinReplacer
{
	private static AssetBundle _assetBundle;

	public const string PLAYER_PREFIX = "Traveller_Rig_v01:Traveller_";
	public const string PLAYER_SUFFIX = "_Jnt";

	public const string CHERT = nameof(CHERT);
	public const string GABBRO = nameof(GABBRO);
	public const string FELDSPAR = nameof(FELDSPAR);
	public const string PROTAGONIST = nameof(PROTAGONIST);

	private static readonly Dictionary<string, GameObject> _skins = new Dictionary<string, GameObject>()
	{
		{ CHERT, LoadPrefab("OW_Chert_Skin") },
		{ GABBRO, LoadPrefab("OW_Gabbro_Skin") },
		{ FELDSPAR, LoadPrefab("OW_Feldspar_Skin") },
	};

	private static readonly Dictionary<string, Func<string, string>> _boneMaps = new Dictionary<string, Func<string, string>>()
	{
		{ CHERT, (name) => name.Replace("Chert_Skin_02:Child_Rig_V01:", PLAYER_PREFIX) },
		{ GABBRO, (name) => name.Replace("gabbro_OW_V02:gabbro_rig_v01:", PLAYER_PREFIX) },
		{ FELDSPAR, (name) => name.Replace("Feldspar_Skin:Short_Rig_V01:", PLAYER_PREFIX) },
	};

	public static SkinnedMeshRenderer[] ReplaceSkin(GameObject playerBody, string skinName, bool isRemote, bool isSuited)
	{
		if (skinName.ToUpper() == PROTAGONIST)
		{
			ResetSkin(playerBody);
			return null;
		}

		var skin = _skins.GetValueOrDefault(skinName.ToUpper());
		var map = _boneMaps.GetValueOrDefault(skinName.ToUpper());

		if (skin == default || map == default)
		{
			DebugLogger.WriteError($"SKIN [{skinName}] WASN'T FOUND");
			return null;
		}

		if (playerBody == null)
		{
			DebugLogger.WriteError("TRIED TO REPLACE PLAYER SKIN BUT PLAYER BODY IS NULL");
		}

		// Returns the skinned mesh renderer so if you switch to a different skin you can destroy the old one
		var root = isRemote ?
			"REMOTE_Traveller_HEA_Player_v2" :
			"Traveller_HEA_Player_v2";

		var child = isSuited ?
			"Traveller_Mesh_v01:Traveller_Geo" :
			"player_mesh_noSuit:Traveller_HEA_Player";

		var originalSkin = playerBody.transform.Find(root + "/" + child).gameObject;

		// Turn off helmet animator
		if (isRemote) 
		{
			var helmetAnimator = playerBody.transform.Find("REMOTE_Traveller_HEA_Player_v2").GetComponent<HelmetAnimator>();
			helmetAnimator.enabled = false;
			helmetAnimator.FakeHelmet.gameObject.SetActive(false);
			helmetAnimator.FakeHead.gameObject.SetActive(false);
		}

		return Swap(originalSkin, skin, map);
	}

	private static void ResetSkin(GameObject playerBody)
	{
		// Maybe you'll want to cache this dictionary
		var playerPrefab = QSBHelper.GetPlayerPrefab();
		var suitRenderers = playerPrefab.transform.Find("REMOTE_Traveller_HEA_Player_v2/Traveller_Mesh_v01:Traveller_Geo").GetComponentsInChildren<SkinnedMeshRenderer>();
		var suitlessRenderers = playerPrefab.transform.Find("REMOTE_Traveller_HEA_Player_v2/player_mesh_noSuit:Traveller_HEA_Player").GetComponentsInChildren<SkinnedMeshRenderer>();
		
		// Re-enable helmet animator
		var helmetAnimator = playerPrefab.transform.Find("REMOTE_Traveller_HEA_Player_v2").GetComponent<HelmetAnimator>();
		helmetAnimator.enabled = true;
		helmetAnimator.SetHelmetInstant(helmetAnimator.SuitGroup.activeSelf);

		var originalMeshs = new Dictionary<string, Mesh>();
		foreach (var skinnedMeshRenderer in suitRenderers.Concat(suitlessRenderers))
		{
			originalMeshs.Add(skinnedMeshRenderer.gameObject.name, skinnedMeshRenderer.sharedMesh);
		}

		foreach (var skinnedMeshRenderer in playerBody.GetComponentsInChildren<SkinnedMeshRenderer>(true))
		{
			if (originalMeshs.ContainsKey(skinnedMeshRenderer.gameObject.name))
			{
				skinnedMeshRenderer.sharedMesh = originalMeshs[skinnedMeshRenderer.gameObject.name];
			}
			else
			{
				DebugLogger.Write($"Couldn't find: [{skinnedMeshRenderer.gameObject.name}]");
			}
		}
	}

	/// <summary>
	/// Creates a copy of the skin and attaches all it's bones to the skeleton of the player
	/// boneMap maps from the bone name of the skin to the bone name of the original player prefab
	/// 
	/// Original is meant to be the actual game object of the skin we're replacing
	/// </summary>
	private static SkinnedMeshRenderer[] Swap(GameObject original, GameObject toCopy, Func<string, string> boneMap)
	{
		var newModel = GameObject.Instantiate(toCopy, original.transform.parent.transform);
		newModel.transform.localPosition = Vector3.zero;
		newModel.SetActive(true);

		// Disappear existing mesh renderers
		foreach (var skinnedMeshRenderer in original.GetComponentsInChildren<SkinnedMeshRenderer>())
		{
			if (!skinnedMeshRenderer.name.Contains("Props_HEA_Jetpack"))
			{
				skinnedMeshRenderer.sharedMesh = null;

				var owRenderer = skinnedMeshRenderer.gameObject.GetComponent<OWRenderer>();
				if (owRenderer != null) owRenderer.enabled = false;

				var streamingMeshHandle = skinnedMeshRenderer.gameObject.GetComponent<StreamingMeshHandle>();
				if (streamingMeshHandle != null) GameObject.Destroy(streamingMeshHandle);
			}
		}

		var skinnedMeshRenderers = newModel.transform.GetComponentsInChildren<SkinnedMeshRenderer>();
		foreach (var skinnedMeshRenderer in skinnedMeshRenderers)
		{
			var bones = skinnedMeshRenderer.bones;
			for (int i = 0; i < bones.Length; i++)
			{
				// Reparent the bone to the player skeleton
				var bone = bones[i];
				string matchingBone = boneMap(bone?.name);
				var newParent = original.transform.parent.SearchInChildren(matchingBone);
				if (newParent == null)
				{
					// This should never happen in a release, this is just for testing with new models
					DebugLogger.Write($"Couldn't find bone [{matchingBone}] matching [{bone}]");
				}
				else
				{
					bone.parent = newParent;
					bone.localPosition = Vector3.zero;
					bone.localRotation = Quaternion.identity;

					// Because the Remote Player is scaled by 0.1f for some reason so we have to offset this
					bone.localScale = Vector3.one * 10f;
				}
			}

			skinnedMeshRenderer.rootBone = original.transform.parent.SearchInChildren(PLAYER_PREFIX + "Trajectory" + PLAYER_SUFFIX);
			skinnedMeshRenderer.quality = SkinQuality.Bone4;
			skinnedMeshRenderer.updateWhenOffscreen = true;

			// Reparent the skinnedMeshRenderer to the original object.
			skinnedMeshRenderer.transform.parent = original.transform;
		}
		// Since we reparented everything to the player we don't need this anymore
		GameObject.Destroy(newModel);

		return skinnedMeshRenderers;
	}

	private static GameObject LoadPrefab(string name)
	{
		if (_assetBundle == null)
		{
			_assetBundle = QSBSkins.Instance.ModHelper.Assets.LoadBundle($"AssetBundles/skins");
		}

		var prefab = _assetBundle.LoadAsset<GameObject>($"Assets/Prefabs/{name}.prefab");

		return prefab;
	}
}