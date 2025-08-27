using UnityEngine;
using System.Collections.Generic;

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
            Debug.Log($"[UIManager] Registered player: {player.playerName}");
            UpdateAllNameTags();
        }
    }

    public void UnregisterPlayer(PlayerCore player)
    {
        players.Remove(player);
        Debug.Log($"[UIManager] Unregistered player: {player.playerName}");
    }

    public void UpdateAllNameTags()
    {
        PlayerTeam localTeam = PlayerCore.localPlayerCoreInstance != null ? PlayerCore.localPlayerCoreInstance.team : PlayerTeam.None;
        foreach (PlayerCore player in players)
        {
            if (player.nameTagUI != null)
            {
                player.nameTagUI.UpdateNameAndTeam(player.playerName, player.team, localTeam);
                Debug.Log($"[UIManager] Updated NameTag for {player.playerName}, Team: {player.team}, LocalTeam: {localTeam}");
            }
        }
    }
}