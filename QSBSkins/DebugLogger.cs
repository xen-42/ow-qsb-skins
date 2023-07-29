namespace QSBSkins;

public static class DebugLogger
{
	public static void Write(string message) => QSBSkins.Instance.ModHelper.Console.WriteLine(message);
	public static void WriteError(string message) => QSBSkins.Instance.ModHelper.Console.WriteLine(message, OWML.Common.MessageType.Error);
}
