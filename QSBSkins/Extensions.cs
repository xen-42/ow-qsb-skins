using UnityEngine;

namespace QSBSkins;

public static class Extensions
{
	public static Transform SearchInChildren(this Transform parent, string target)
	{
		if (parent.name.Equals(target)) return parent;

		foreach (Transform child in parent)
		{
			var search = SearchInChildren(child, target);
			if (search != null) return search;
		}

		return null;
	}
}
