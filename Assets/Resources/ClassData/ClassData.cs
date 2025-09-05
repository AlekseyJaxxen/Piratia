using UnityEngine;

[CreateAssetMenu(fileName = "ClassData", menuName = "SO/ClassData")]
public class ClassData : ScriptableObject
{
    public CharacterClass characterClass;
    public int strength = 5;
    public int agility = 5;
    public int constitution = 5;
    public int spirit = 5;
    public int accuracy = 5;
    public int maxHealth = 1000;
    public int maxMana = 100;
    public float movementSpeed = 8f;
    public GameObject modelPrefab; // Префаб модели для данного класса
    public RuntimeAnimatorController animatorController; // Контроллер анимации для данного класса
}