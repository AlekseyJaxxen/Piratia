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

    // Сериализация
    public void Serialize(NetworkWriter writer)
    {
        writer.WriteInt((int)type); // Заменено WriteInt32 на WriteInt
        writer.WriteFloat(endTime); // Заменено WriteSingle на WriteFloat
        writer.WriteFloat(slowPercentage); // Заменено WriteSingle на WriteFloat
    }

    // Десериализация
    public static ControlEffect Deserialize(NetworkReader reader)
    {
        return new ControlEffect
        {
            type = (ControlEffectType)reader.ReadInt(), // Заменено ReadInt32 на ReadInt
            endTime = reader.ReadFloat(), // Заменено ReadSingle на ReadFloat
            slowPercentage = reader.ReadFloat() // Заменено ReadSingle на ReadFloat
        };
    }
}