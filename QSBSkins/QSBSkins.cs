using OWML.Common;
using OWML.ModHelper;

namespace QSBSkins
{
	public class QSBSkins : ModBehaviour
	{
		public static QSBSkins Instance { get; private set; }

		private void Awake()
		{
			Instance = this;
		}

		private void Start()
		{
			this.gameObject.AddComponent<SkinHandler>();
		}
	}
}