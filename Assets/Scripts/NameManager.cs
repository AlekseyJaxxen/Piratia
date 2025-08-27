using UnityEngine;
using System.Collections.Generic;
using System.Collections;

public class NameManager : MonoBehaviour
{
    public static NameManager Instance { get; private set; }
    private List<PlayerCore> players = new List<PlayerCore>();

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void RegisterPlayer(PlayerCore player)
    {
        if (!players.Contains(player))
        {
            players.Add(player);
            Debug.Log($"[NameManager] Registered player: {player.playerName}");
            StartCoroutine(UpdateNameTagsDelayed()); // Отложенное обновление
        }
    }

    public void UnregisterPlayer(PlayerCore player)
    {
        players.Remove(player);
        Debug.Log($"[NameManager] Unregistered player: {player.playerName}");
    }

    public void UpdateAllNameTags()
    {
        StartCoroutine(UpdateNameTagsDelayed());
    }

    private IEnumerator UpdateNameTagsDelayed()
    {
        yield return new WaitForSeconds(0.5f); // Ждем, пока все игроки инициализируются
        PlayerTeam localTeam = PlayerCore.localPlayerCoreInstance != null ? PlayerCore.localPlayerCoreInstance.team : PlayerTeam.None;
        foreach (PlayerCore player in players)
        {
            if (player.nameTagUI != null)
            {
                player.nameTagUI.UpdateNameAndTeam(player.playerName, player.team, localTeam);
                Debug.Log($"[NameManager] Updated NameTag for {player.playerName}, Team: {player.team}, LocalTeam: {localTeam}");
            }
            else
            {
                Debug.LogWarning($"[NameManager] NameTagUI is null for {player.playerName}");
            }
        }
    }
}