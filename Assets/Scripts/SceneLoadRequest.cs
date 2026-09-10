public static class SceneLoadRequest
{
    private static string requestedScene;

    public static void Request(string sceneName)
    {
        requestedScene = sceneName;
    }

    public static string Consume(string fallbackScene)
    {
        if (string.IsNullOrWhiteSpace(requestedScene)) return fallbackScene;
        string sceneName = requestedScene;
        requestedScene = null;
        return sceneName;
    }

    public static void Clear()
    {
        requestedScene = null;
    }
}
