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

    public void Serialize(NetworkWriter writer)
    {
        writer.WriteInt((int)type);
        writer.WriteFloat(endTime);
        writer.WriteFloat(slowPercentage);
    }

    public static ControlEffect Deserialize(NetworkReader reader)
    {
        return new ControlEffect
        {
            type = (ControlEffectType)reader.ReadInt(),
            endTime = reader.ReadFloat(),
            slowPercentage = reader.ReadFloat()
        };
    }
}