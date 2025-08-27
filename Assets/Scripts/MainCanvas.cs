using UnityEngine;

public class MainCanvas : MonoBehaviour
{
    public static Canvas Instance { get; private set; }

    void Awake()
    {
        Instance = GetComponent<Canvas>();
    }
}