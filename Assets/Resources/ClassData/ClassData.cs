using UnityEngine;

[CreateAssetMenu(fileName = "ClassData", menuName = "SO/ClassData")]
public class ClassData : ScriptableObject
{
    public CharacterClass characterClass;
    [Header("Base Attributes")]
    public int strength = 5; // Начальная сила
    public int agility = 5; // Начальная ловкость
    public int constitution = 5; // Начальная выносливость
    public int spirit = 5; // Начальный дух
    public int accuracy = 5; // Начальная точность
    public int intelligence = 5; // Начальный интеллект
    [Header("Base Stats")]
    public int baseHealth = 1000; // Базовое здоровье
    public int baseMana = 100; // Базовая мана
    public int baseMinAttack = 15; // Базовый минимальный урон
    public int baseMaxAttack = 20; // Базовый максимальный урон
    public float baseDef = 10f; // Базовая защита
    public float baseMovementSpeed = 8f; // Базовая скорость передвижения
    [Header("Attribute Multipliers")]
    public float strengthMultiplier = 1f; // Множитель для strength
    public float agilityMultiplier = 1f; // Множитель для agility
    public float constitutionMultiplier = 1f; // Множитель для constitution
    public float spiritMultiplier = 1f; // Множитель для spirit
    public float accuracyMultiplier = 1f; // Множитель для accuracy
    public float intelligenceMultiplier = 1f; // Множитель для intelligence
    [Header("Visuals")]
    public GameObject modelPrefab; // Префаб модели для данного класса
    public RuntimeAnimatorController animatorController; // Контроллер анимации для данного класса
}