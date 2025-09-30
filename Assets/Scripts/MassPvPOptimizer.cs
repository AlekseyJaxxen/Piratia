using UnityEngine;
using Mirror;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

[RequireComponent(typeof(NetworkIdentity))]
public class MassPvPOptimizer : NetworkBehaviour
{
    [Header("Mass PvP Settings")]
    public int maxPlayersInBattle = 200;
    public float battleZoneRadius = 500f;
    public float lodUpdateInterval = 0.5f;
    public float networkBatchInterval = 0.1f;
    
    [Header("LOD Distances")]
    public float highDetailDistance = 50f;
    public float mediumDetailDistance = 100f;
    public float lowDetailDistance = 200f;
    
    [Header("Performance Limits")]
    public int maxNetworkCallsPerSecond = 1000;
    public int maxPhysicsQueriesPerFrame = 50;
    
    private Dictionary<PlayerCore, PlayerLOD> playerLODs = new Dictionary<PlayerCore, PlayerLOD>();
    private List<NetworkCommand> commandBatch = new List<NetworkCommand>();
    private Coroutine batchCoroutine;
    private Coroutine lodCoroutine;
    
    public static MassPvPOptimizer Instance;
    
    public override void OnStartServer()
    {
        base.OnStartServer();
        Instance = this;
        StartOptimizationSystems();
    }
    
    private void StartOptimizationSystems()
    {
        // Запускаем систему батчинга команд
        batchCoroutine = StartCoroutine(ProcessCommandBatch());
        
        // Запускаем систему LOD
        lodCoroutine = StartCoroutine(UpdateLODSystem());
        
        Debug.Log("[MassPvPOptimizer] Mass PvP optimization systems started");
    }
    
    private IEnumerator ProcessCommandBatch()
    {
        while (true)
        {
            yield return new WaitForSeconds(networkBatchInterval);
            
            if (commandBatch.Count > 0)
            {
                ProcessBatchedCommands();
                commandBatch.Clear();
            }
        }
    }
    
    private void ProcessBatchedCommands()
    {
        // Группируем команды по типам
        var groupedCommands = commandBatch.GroupBy(cmd => cmd.CommandType);
        
        foreach (var group in groupedCommands)
        {
            switch (group.Key)
            {
                case CommandType.Movement:
                    ProcessMovementBatch(group.ToList());
                    break;
                case CommandType.Combat:
                    ProcessCombatBatch(group.ToList());
                    break;
                case CommandType.Animation:
                    ProcessAnimationBatch(group.ToList());
                    break;
            }
        }
    }
    
    private void ProcessMovementBatch(List<NetworkCommand> commands)
    {
        // Объединяем движения игроков в одну команду
        var movementData = new List<PlayerMovementData>();
        
        foreach (var cmd in commands)
        {
            movementData.Add(new PlayerMovementData
            {
                playerId = cmd.PlayerId,
                position = cmd.Position,
                rotation = cmd.Rotation
            });
        }
        
        // Отправляем батч движений
        RpcBatchMovementUpdate(movementData.ToArray());
    }
    
    private void ProcessCombatBatch(List<NetworkCommand> commands)
    {
        // Обрабатываем боевые действия
        foreach (var cmd in commands)
        {
            // Выполняем команду на сервере
            ExecuteCombatCommand(cmd);
        }
    }
    
    private void ProcessAnimationBatch(List<NetworkCommand> commands)
    {
        // Объединяем анимации
        var animationData = new List<PlayerAnimationData>();
        
        foreach (var cmd in commands)
        {
            animationData.Add(new PlayerAnimationData
            {
                playerId = cmd.PlayerId,
                animationName = cmd.AnimationName,
                isPlaying = cmd.IsPlaying
            });
        }
        
        RpcBatchAnimationUpdate(animationData.ToArray());
    }
    
    [ClientRpc]
    private void RpcBatchMovementUpdate(PlayerMovementData[] movements)
    {
        foreach (var movement in movements)
        {
            var player = FindPlayerById(movement.playerId);
            if (player != null)
            {
                player.transform.position = movement.position;
                player.transform.rotation = movement.rotation;
            }
        }
    }
    
    [ClientRpc]
    private void RpcBatchAnimationUpdate(PlayerAnimationData[] animations)
    {
        foreach (var anim in animations)
        {
            var player = FindPlayerById(anim.playerId);
            if (player != null)
            {
                var animSystem = player.GetComponent<PlayerAnimationSystem>();
                if (animSystem != null)
                {
                    // Используем существующий метод CmdPlayAnimation через рефлексию
                    var method = typeof(PlayerAnimationSystem).GetMethod("CmdPlayAnimation", 
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    if (method != null)
                    {
                        method.Invoke(animSystem, new object[] { anim.animationName });
                    }
                }
            }
        }
    }
    
    private IEnumerator UpdateLODSystem()
    {
        while (true)
        {
            yield return new WaitForSeconds(lodUpdateInterval);
            
            UpdatePlayerLODs();
        }
    }
    
    private void UpdatePlayerLODs()
    {
        var allPlayers = FindObjectsOfType<PlayerCore>();
        
        foreach (var player in allPlayers)
        {
            if (!playerLODs.ContainsKey(player))
            {
                playerLODs[player] = new PlayerLOD();
            }
            
            var lod = playerLODs[player];
            var distance = Vector3.Distance(transform.position, player.transform.position);
            
            // Определяем уровень детализации
            if (distance <= highDetailDistance)
            {
                lod.SetLODLevel(LODLevel.High);
            }
            else if (distance <= mediumDetailDistance)
            {
                lod.SetLODLevel(LODLevel.Medium);
            }
            else if (distance <= lowDetailDistance)
            {
                lod.SetLODLevel(LODLevel.Low);
            }
            else
            {
                lod.SetLODLevel(LODLevel.Culled);
            }
            
            // Применяем LOD к игроку
            ApplyLODToPlayer(player, lod);
        }
    }
    
    private void ApplyLODToPlayer(PlayerCore player, PlayerLOD lod)
    {
        switch (lod.CurrentLevel)
        {
            case LODLevel.High:
                // Полная детализация
                player.GetComponent<Renderer>().enabled = true;
                player.GetComponent<Animator>().enabled = true;
                player.GetComponent<Collider>().enabled = true;
                break;
                
            case LODLevel.Medium:
                // Средняя детализация
                player.GetComponent<Renderer>().enabled = true;
                player.GetComponent<Animator>().enabled = true;
                player.GetComponent<Collider>().enabled = true;
                // Уменьшаем частоту обновления анимаций
                break;
                
            case LODLevel.Low:
                // Низкая детализация
                player.GetComponent<Renderer>().enabled = true;
                player.GetComponent<Animator>().enabled = false;
                player.GetComponent<Collider>().enabled = true;
                break;
                
            case LODLevel.Culled:
                // Скрываем игрока
                player.GetComponent<Renderer>().enabled = false;
                player.GetComponent<Animator>().enabled = false;
                player.GetComponent<Collider>().enabled = false;
                break;
        }
    }
    
    // Публичные методы для добавления команд в батч
    public void AddCommandToBatch(NetworkCommand command)
    {
        commandBatch.Add(command);
    }
    
    public void AddMovementCommand(uint playerId, Vector3 position, Quaternion rotation)
    {
        var command = new NetworkCommand
        {
            CommandType = CommandType.Movement,
            PlayerId = playerId,
            Position = position,
            Rotation = rotation
        };
        AddCommandToBatch(command);
    }
    
    public void AddCombatCommand(uint playerId, string skillName, uint targetId)
    {
        var command = new NetworkCommand
        {
            CommandType = CommandType.Combat,
            PlayerId = playerId,
            SkillName = skillName,
            TargetId = targetId
        };
        AddCommandToBatch(command);
    }
    
    public void AddAnimationCommand(uint playerId, string animationName, bool isPlaying)
    {
        var command = new NetworkCommand
        {
            CommandType = CommandType.Animation,
            PlayerId = playerId,
            AnimationName = animationName,
            IsPlaying = isPlaying
        };
        AddCommandToBatch(command);
    }
    
    private PlayerCore FindPlayerById(uint playerId)
    {
        var allPlayers = FindObjectsOfType<PlayerCore>();
        return allPlayers.FirstOrDefault(p => p.netId == playerId);
    }
    
    private void ExecuteCombatCommand(NetworkCommand cmd)
    {
        var player = FindPlayerById(cmd.PlayerId);
        if (player != null)
        {
            var skills = player.GetComponent<PlayerSkills>();
            if (skills != null)
            {
                // Выполняем скилл на сервере
                skills.CmdExecuteSkill(player, null, cmd.TargetId, cmd.SkillName, 0);
            }
        }
    }
    
    public override void OnStopServer()
    {
        base.OnStopServer();
        
        if (batchCoroutine != null)
            StopCoroutine(batchCoroutine);
            
        if (lodCoroutine != null)
            StopCoroutine(lodCoroutine);
    }
}

[System.Serializable]
public class NetworkCommand
{
    public CommandType CommandType;
    public uint PlayerId;
    public Vector3 Position;
    public Quaternion Rotation;
    public string SkillName;
    public uint TargetId;
    public string AnimationName;
    public bool IsPlaying;
}

[System.Serializable]
public class PlayerMovementData
{
    public uint playerId;
    public Vector3 position;
    public Quaternion rotation;
}

[System.Serializable]
public class PlayerAnimationData
{
    public uint playerId;
    public string animationName;
    public bool isPlaying;
}

public enum CommandType
{
    Movement,
    Combat,
    Animation
}

public class PlayerLOD
{
    public LODLevel CurrentLevel = LODLevel.High;
    
    public void SetLODLevel(LODLevel level)
    {
        CurrentLevel = level;
    }
}

public enum LODLevel
{
    High,
    Medium,
    Low,
    Culled
}
