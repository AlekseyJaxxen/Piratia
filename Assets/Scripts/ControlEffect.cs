using UnityEngine;
using Mirror;

[System.Serializable]
public struct ControlEffect
{
    public ControlEffectType type;
    public float endTime;
    public float slowPercentage;

    public ControlEffect(ControlEffectType type, float endTime, float slowPercentage)
    {
        this.type = type;
        this.endTime = endTime;
        this.slowPercentage = slowPercentage;
    }

    // ������������
    public void Serialize(NetworkWriter writer)
    {
        writer.WriteInt((int)type); // �������� WriteInt32 �� WriteInt
        writer.WriteFloat(endTime); // �������� WriteSingle �� WriteFloat
        writer.WriteFloat(slowPercentage); // �������� WriteSingle �� WriteFloat
    }

    // ��������������
    public static ControlEffect Deserialize(NetworkReader reader)
    {
        return new ControlEffect
        {
            type = (ControlEffectType)reader.ReadInt(), // �������� ReadInt32 �� ReadInt
            endTime = reader.ReadFloat(), // �������� ReadSingle �� ReadFloat
            slowPercentage = reader.ReadFloat() // �������� ReadSingle �� ReadFloat
        };
    }
}