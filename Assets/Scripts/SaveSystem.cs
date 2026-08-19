using System;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

[Serializable]
public sealed class SaveData
{
    public string sceneName;
    public Vector3 playerPosition;
    public Quaternion playerRotation;
    public string savedAtUtc;
}

public static class SaveSystem
{
    private const string SaveFileName = "roaming-save.json";

    private static SaveData pendingLoad;

    private static string SavePath => Path.Combine(Application.persistentDataPath, SaveFileName);

    public static bool HasSave => File.Exists(SavePath);

    public static bool SaveCurrentGame()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null)
        {
            Debug.LogWarning("SaveSystem: nenhum objeto com a tag Player foi encontrado.");
            return false;
        }

        SaveData data = new SaveData
        {
            sceneName = SceneManager.GetActiveScene().name,
            playerPosition = player.transform.position,
            playerRotation = player.transform.rotation,
            savedAtUtc = DateTime.UtcNow.ToString("O")
        };

        try
        {
            File.WriteAllText(SavePath, JsonUtility.ToJson(data, true));
            Debug.Log($"Jogo salvo em: {SavePath}");
            return true;
        }
        catch (Exception exception)
        {
            Debug.LogError($"SaveSystem: não foi possível salvar o jogo. {exception.Message}");
            return false;
        }
    }

    public static bool PrepareLoad()
    {
        if (!HasSave)
        {
            Debug.Log("SaveSystem: não há nenhum save local.");
            return false;
        }

        try
        {
            pendingLoad = JsonUtility.FromJson<SaveData>(File.ReadAllText(SavePath));
            if (pendingLoad == null || string.IsNullOrWhiteSpace(pendingLoad.sceneName))
            {
                Debug.LogWarning("SaveSystem: o arquivo de save é inválido.");
                pendingLoad = null;
                return false;
            }            SaveLoadRuntime.EnsureInstance();
            return true;
        }
        catch (Exception exception)
        {
            Debug.LogWarning($"SaveSystem: não foi possível carregar o save. {exception.Message}");
            pendingLoad = null;
            return false;
        }
    }

    public static string GetSceneToLoad(string fallbackScene)
    {
        return pendingLoad != null ? pendingLoad.sceneName : fallbackScene;
    }

public static bool HasPendingLoadFor(string sceneName)
    {
        return pendingLoad != null && pendingLoad.sceneName == sceneName;
    }


    public static void ApplyPendingLoad()
    {
        if (pendingLoad == null || pendingLoad.sceneName != SceneManager.GetActiveScene().name)
            return;

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null)
        {
            Debug.LogWarning("SaveSystem: jogador não encontrado para aplicar o save.");
            return;
        }

        Rigidbody body = player.GetComponent<Rigidbody>();
        if (body != null)
        {
            body.position = pendingLoad.playerPosition;
            body.rotation = pendingLoad.playerRotation;
            body.linearVelocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
        }
        else
        {
            player.transform.SetPositionAndRotation(pendingLoad.playerPosition, pendingLoad.playerRotation);
        }

        pendingLoad = null;
    }
}