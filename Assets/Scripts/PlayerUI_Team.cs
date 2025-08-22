using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Mirror;

public class PlayerUI_Team : MonoBehaviour
{
    public GameObject teamSelectionPanel;
    public Button redTeamButton;
    public Button blueTeamButton;
    public TMP_InputField nameInputField;
    public Button confirmNameButton;

    private PlayerCore _localPlayerCore;

    void Start()
    {

        if (redTeamButton != null)
        {
            redTeamButton.onClick.AddListener(() => OnTeamSelected(PlayerTeam.Red));
        }

        if (blueTeamButton != null)
        {
            blueTeamButton.onClick.AddListener(() => OnTeamSelected(PlayerTeam.Blue));
        }

        if (confirmNameButton != null)
        {
            confirmNameButton.onClick.AddListener(OnNameConfirmed);
        }

        if (teamSelectionPanel != null)
        {
            teamSelectionPanel.SetActive(false);
        }
    }

    public void ShowTeamSelectionUI()
    {
        if (teamSelectionPanel != null)
        {
            teamSelectionPanel.SetActive(true);
        }
    }

    private void OnTeamSelected(PlayerTeam selectedTeam)
    {
        _localPlayerCore = FindObjectOfType<PlayerCore>();
        if (_localPlayerCore != null)
        {
            _localPlayerCore.CmdSetPlayerInfo(_localPlayerCore.playerName, selectedTeam);
        }

        if (teamSelectionPanel != null)
        {
            teamSelectionPanel.SetActive(false);
        }
    }

    private void OnNameConfirmed()
    {
        if (nameInputField != null && !string.IsNullOrEmpty(nameInputField.text))
        {
            _localPlayerCore = FindObjectOfType<PlayerCore>();
            if (_localPlayerCore != null)
            {
                _localPlayerCore.CmdSetPlayerInfo(nameInputField.text, _localPlayerCore.team);
            }
        }
    }
}