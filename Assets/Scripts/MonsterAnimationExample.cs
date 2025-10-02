using UnityEngine;

/// <summary>
/// Пример использования системы анимаций монстров с поддержкой ID
/// </summary>
public class MonsterAnimationExample : MonoBehaviour
{
    [Header("Animation Testing")]
    public Monster targetMonster;
    
    [Header("Animation IDs (for testing)")]
    public int idleId = 0;
    public int walkId = 1;
    public int attackId = 2;
    public int deathId = 3;
    
    private void Start()
    {
        if (targetMonster == null)
        {
            targetMonster = GetComponent<Monster>();
        }
        
        if (targetMonster != null)
        {
            // Выводим информацию о доступных анимациях
            LogAnimationInfo();
        }
    }
    
    private void LogAnimationInfo()
    {
        Debug.Log("=== Monster Animation Info ===");
        Debug.Log($"Monster Type: {(targetMonster.IsHumanoidMonster() ? "Humanoid" : "Non-Humanoid")}");
        Debug.Log($"Total Animations: {targetMonster.GetAnimationCount()}");
        
        // Выводим все анимации с их ID
        for (int i = 0; i < targetMonster.GetAnimationCount(); i++)
        {
            string animName = targetMonster.GetAnimationName(i);
            Debug.Log($"  ID {i}: {animName}");
        }
        
        // Проверяем стандартные анимации
        CheckStandardAnimations();
    }
    
    private void CheckStandardAnimations()
    {
        string[] standardAnims = { "Idle", "Walk", "Move", "Attack", "Death" };
        
        Debug.Log("=== Standard Animation Check ===");
        foreach (string animName in standardAnims)
        {
            int id = targetMonster.GetAnimationId(animName);
            if (id != -1)
            {
                Debug.Log($"✓ {animName} -> ID {id}");
            }
            else
            {
                Debug.Log($"✗ {animName} not found");
            }
        }
    }
    
    // Методы для тестирования в Inspector или через код
    [ContextMenu("Play Idle")]
    public void PlayIdle()
    {
        if (targetMonster != null)
        {
            targetMonster.PlayAnimation("Idle");
            Debug.Log("Playing Idle animation by name");
        }
    }
    
    [ContextMenu("Play Idle by ID")]
    public void PlayIdleById()
    {
        if (targetMonster != null)
        {
            targetMonster.PlayAnimationById(idleId);
            Debug.Log($"Playing Idle animation by ID: {idleId}");
        }
    }
    
    [ContextMenu("Play Walk")]
    public void PlayWalk()
    {
        if (targetMonster != null)
        {
            targetMonster.PlayAnimation("Walk");
            Debug.Log("Playing Walk animation by name");
        }
    }
    
    [ContextMenu("Play Walk by ID")]
    public void PlayWalkById()
    {
        if (targetMonster != null)
        {
            targetMonster.PlayAnimationById(walkId);
            Debug.Log($"Playing Walk animation by ID: {walkId}");
        }
    }
    
    [ContextMenu("Play Attack")]
    public void PlayAttack()
    {
        if (targetMonster != null)
        {
            targetMonster.PlayAnimation("Attack");
            Debug.Log("Playing Attack animation by name");
        }
    }
    
    [ContextMenu("Play Attack by ID")]
    public void PlayAttackById()
    {
        if (targetMonster != null)
        {
            targetMonster.PlayAnimationById(attackId);
            Debug.Log($"Playing Attack animation by ID: {attackId}");
        }
    }
    
    [ContextMenu("Play Death")]
    public void PlayDeath()
    {
        if (targetMonster != null)
        {
            targetMonster.PlayAnimation("Death");
            Debug.Log("Playing Death animation by name");
        }
    }
    
    [ContextMenu("Play Death by ID")]
    public void PlayDeathById()
    {
        if (targetMonster != null)
        {
            targetMonster.PlayAnimationById(deathId);
            Debug.Log($"Playing Death animation by ID: {deathId}");
        }
    }
    
    [ContextMenu("Stop All Animations")]
    public void StopAllAnimations()
    {
        if (targetMonster != null)
        {
            targetMonster.StopAllAnimations();
            Debug.Log("Stopped all animations");
        }
    }
    
    [ContextMenu("Test Random Animation")]
    public void TestRandomAnimation()
    {
        if (targetMonster != null)
        {
            int randomId = Random.Range(0, targetMonster.GetAnimationCount());
            string animName = targetMonster.GetAnimationName(randomId);
            
            Debug.Log($"Playing random animation: ID {randomId} ({animName})");
            targetMonster.PlayAnimationById(randomId);
        }
    }
    
    // Пример использования в коде
    private void Update()
    {
        if (targetMonster == null) return;
        
        // Пример: нажатие клавиш для тестирования
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            PlayIdleById();
        }
        else if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            PlayWalkById();
        }
        else if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            PlayAttackById();
        }
        else if (Input.GetKeyDown(KeyCode.Alpha4))
        {
            PlayDeathById();
        }
        else if (Input.GetKeyDown(KeyCode.Space))
        {
            TestRandomAnimation();
        }
    }
}
