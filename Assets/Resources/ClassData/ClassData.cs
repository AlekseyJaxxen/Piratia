using UnityEngine;

[CreateAssetMenu(fileName = "ClassData", menuName = "SO/ClassData")]
public class ClassData : ScriptableObject
{
    public CharacterClass characterClass;
    [Header("Base Attributes")]
    public int strength = 5; // ��������� ����
    public int agility = 5; // ��������� ��������
    public int constitution = 5; // ��������� ������������
    public int spirit = 5; // ��������� ���
    public int accuracy = 5; // ��������� ��������
    public int intelligence = 5; // ��������� ���������
    [Header("Base Stats")]
    public int baseHealth = 1000; // ������� ��������
    public int baseMana = 100; // ������� ����
    public int baseMinAttack = 15; // ������� ����������� ����
    public int baseMaxAttack = 20; // ������� ������������ ����
    public float baseDef = 10f; // ������� ������
    public float baseMovementSpeed = 8f; // ������� �������� ������������
    [Header("Attribute Multipliers")]
    public float strengthMultiplier = 1f; // ��������� ��� strength
    public float agilityMultiplier = 1f; // ��������� ��� agility
    public float constitutionMultiplier = 1f; // ��������� ��� constitution
    public float spiritMultiplier = 1f; // ��������� ��� spirit
    public float accuracyMultiplier = 1f; // ��������� ��� accuracy
    public float intelligenceMultiplier = 1f; // ��������� ��� intelligence
    [Header("Visuals")]
    public GameObject modelPrefab; // ������ ������ ��� ������� ������
    public RuntimeAnimatorController animatorController; // ���������� �������� ��� ������� ������
}