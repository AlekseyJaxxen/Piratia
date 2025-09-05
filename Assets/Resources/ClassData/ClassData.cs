using UnityEngine;

public enum AttackAttributeType
{
    Strength,
    Accuracy
}

[CreateAssetMenu(fileName = "ClassData", menuName = "SO/ClassData")]
public class ClassData : ScriptableObject
{
    public CharacterClass characterClass;
    [Header("Base Attributes")]
    public int strength = 5;
    public int agility = 5;
    public int constitution = 5;
    public int spirit = 5;
    public int accuracy = 5;
    public int intelligence = 5;
    [Header("Base Stats")]
    public int baseHealth = 1000;
    public int baseMana = 100;
    public int baseMinAttack = 15;
    public int baseMaxAttack = 20;
    public float baseDef = 10f;
    public float baseMovementSpeed = 8f;
    public float basePhysicalResistance = 0f;
    [Header("Attribute Multipliers")]
    public float strengthMultiplier = 1f;
    public float agilityMultiplier = 1f;
    public float constitutionMultiplier = 1f;
    public float spiritMultiplier = 1f;
    public float accuracyMultiplier = 1f;
    public float intelligenceMultiplier = 1f;
    [Header("Attack Settings")]
    public AttackAttributeType attackAttribute = AttackAttributeType.Strength; // Атрибут для расчета атаки
    [Header("Visuals")]
    public GameObject modelPrefab;
    public RuntimeAnimatorController animatorController;
}