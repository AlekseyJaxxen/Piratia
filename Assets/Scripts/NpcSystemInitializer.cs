using UnityEngine;

public class NpcSystemInitializer : MonoBehaviour
{
    [Header("NPC System Setup")]
    [SerializeField] private GameObject npcContextMenuPrefab;
    
    private void Start()
    {
        InitializeNpcSystem();
    }
    
    private void InitializeNpcSystem()
    {
        // Проверяем, есть ли уже NpcContextMenu в сцене
        NpcContextMenu existingMenu = FindObjectOfType<NpcContextMenu>();
        if (existingMenu == null)
        {
            // Создаем NpcContextMenu если его нет
            CreateNpcContextMenu();
        }
        
        Debug.Log("[NpcSystemInitializer] NPC system initialized");
    }
    
    private void CreateNpcContextMenu()
    {
        GameObject contextMenuObj;
        
        if (npcContextMenuPrefab != null)
        {
            // Используем префаб если он назначен
            contextMenuObj = Instantiate(npcContextMenuPrefab);
        }
        else
        {
            // Создаем базовый объект
            contextMenuObj = new GameObject("NpcContextMenu");
            contextMenuObj.AddComponent<NpcContextMenu>();
        }
        
        contextMenuObj.name = "NpcContextMenu";
        DontDestroyOnLoad(contextMenuObj);
        
        Debug.Log("[NpcSystemInitializer] Created NpcContextMenu");
    }
}
