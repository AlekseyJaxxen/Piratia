using UnityEngine;
using Mirror;
using System.Collections;

public class BombObject : NetworkBehaviour
{
    [Header("Bomb Settings")]
    private int baseDamage;
    private float damageMultiplier;
    private float explosionRadius;
    private float explosionDelay;
    private GameObject explosionEffect;
    private GameObject zoneIndicator;
    private Color zoneColor;
    private float zoneAlpha;
    private int casterTeam;
    private NetworkIdentity casterIdentity;
    
    [Header("Visual Components")]
    private GameObject zoneIndicatorInstance;
    private Renderer bombRenderer;
    private float timer;
    private bool hasExploded = false;
    
    [Header("Animation")]
    public float pulseSpeed = 2f;
    public float pulseIntensity = 0.3f;
    public Color warningColor = Color.red;
    
    
    public void Initialize(int damage, float multiplier, float radius, float delay, 
                          GameObject explosion, GameObject zone, Color color, float alpha,
                          int team, NetworkIdentity caster)
    {
        Debug.Log($"[BombObject] Initialize called, isServer: {isServer}, isClient: {isClient}, zoneIndicator: {zone != null}");
        baseDamage = damage;
        damageMultiplier = multiplier;
        explosionRadius = radius;
        explosionDelay = delay;
        explosionEffect = explosion;
        zoneIndicator = zone;
        zoneColor = color;
        zoneAlpha = alpha;
        casterTeam = team;
        casterIdentity = caster;
        
        // Создаем индикатор зоны после инициализации
        if (zoneIndicator != null)
        {
            Debug.Log($"[BombObject] Creating zone indicator on {(isServer ? "server" : "client")}");
            CreateZoneIndicator();
        }
        else
        {
            Debug.LogWarning($"[BombObject] zoneIndicator is null!");
        }
    }
    
    private void CreateZoneIndicator()
    {
        Debug.Log($"[BombObject] CreateZoneIndicator called, zoneIndicator: {zoneIndicator != null}");
        if (zoneIndicator != null)
        {
            zoneIndicatorInstance = Instantiate(zoneIndicator, transform.position, Quaternion.identity);
            zoneIndicatorInstance.transform.SetParent(transform);
            Debug.Log($"[BombObject] Zone indicator instantiated: {zoneIndicatorInstance != null}");
            
            // Настраиваем размер зоны
            float scale = explosionRadius * 2f;
            zoneIndicatorInstance.transform.localScale = new Vector3(scale, 1f, scale);
            
            // Настраиваем цвет
            Renderer zoneRenderer = zoneIndicatorInstance.GetComponent<Renderer>();
            if (zoneRenderer != null)
            {
                Material zoneMaterial = zoneRenderer.material;
                zoneMaterial.color = new Color(zoneColor.r, zoneColor.g, zoneColor.b, zoneAlpha);
            }
        }
    }
    
    private IEnumerator ExplosionTimer()
    {
        yield return new WaitForSeconds(explosionDelay);
        
        if (!hasExploded)
        {
            Explode();
        }
    }
    
    [Server]
    private void Explode()
    {
        if (hasExploded) return;
        hasExploded = true;
        
        // Bomb exploding
        
        // Оптимизация: кэшируем LayerMask и используем более эффективный поиск
        int explosionLayerMask = LayerMask.GetMask("Player", "Ignore Raycast", "Monster", "Enemy");
        Collider[] hitColliders = new Collider[50]; // Предварительно выделенный массив
        int hitCount = Physics.OverlapSphereNonAlloc(transform.position, explosionRadius, hitColliders, explosionLayerMask);
        
        for (int i = 0; i < hitCount; i++)
        {
            Collider col = hitColliders[i];
            // Проверяем, что это не сам кастер
            if (col.GetComponent<NetworkIdentity>() == casterIdentity) continue;
            
            Health targetHealth = col.GetComponent<Health>();
            HealthMonster targetHealthMonster = col.GetComponent<HealthMonster>();
            
            if (targetHealthMonster != null)
            {
                PlayerCore targetCore = col.GetComponent<PlayerCore>();
                Monster targetMonster = col.GetComponent<Monster>();
                
                // Урон игрокам другой команды
                if (targetCore != null && (int)targetCore.team != casterTeam)
                {
                    targetHealthMonster.TakeDamage(baseDamage, DamageType.Physical, false, casterIdentity, damageMultiplier);
                    // Player damaged
                }
                // Урон монстрам (если кастер - игрок)
                else if (targetMonster != null && casterTeam != (int)PlayerTeam.None)
                {
                    targetHealthMonster.TakeDamage(baseDamage, DamageType.Physical, false, casterIdentity, damageMultiplier);
                    // Monster damaged
                }
            }
            else if (targetHealth != null)
            {
                PlayerCore targetCore = col.GetComponent<PlayerCore>();
                Monster targetMonster = col.GetComponent<Monster>();
                
                // Урон игрокам другой команды
                if (targetCore != null && (int)targetCore.team != casterTeam)
                {
                    targetHealth.TakeDamage(baseDamage, DamageType.Physical, false, casterIdentity, damageMultiplier);
                    // Player damaged
                }
                // Урон монстрам (если кастер - игрок)
                else if (targetMonster != null && casterTeam != (int)PlayerTeam.None)
                {
                    targetHealth.TakeDamage(baseDamage, DamageType.Physical, false, casterIdentity, damageMultiplier);
                    // Monster damaged
                }
            }
        }
        
        // Воспроизводим эффект взрыва на всех клиентах
        RpcPlayExplosionEffect();
        
        // Уничтожаем бомбу
        NetworkServer.Destroy(gameObject);
    }
    
    [ClientRpc]
    private void RpcPlayExplosionEffect()
    {
        if (explosionEffect != null)
        {
            GameObject explosion = Instantiate(explosionEffect, transform.position, Quaternion.identity);
            Destroy(explosion, 3f);
        }
        
        // Удаляем индикатор зоны
        if (zoneIndicatorInstance != null)
        {
            Destroy(zoneIndicatorInstance);
        }
    }
    
    // Оптимизация: используем корутину вместо Update для анимации
    private Coroutine _pulseCoroutine;
    
    private void Start()
    {
        bombRenderer = GetComponent<Renderer>();
        if (bombRenderer == null)
        {
            bombRenderer = GetComponentInChildren<Renderer>();
        }
        
        // Запускаем таймер взрыва
        if (isServer)
        {
            StartCoroutine(ExplosionTimer());
        }
        
        // Оптимизация: запускаем анимацию пульсации через корутину
        _pulseCoroutine = StartCoroutine(PulseAnimation());
    }
    
    private IEnumerator PulseAnimation()
    {
        while (!hasExploded)
        {
            // Анимация пульсации бомбы
            timer += Time.deltaTime * pulseSpeed;
            
            if (bombRenderer != null)
            {
                Color currentColor = Color.Lerp(Color.white, warningColor, Mathf.Sin(timer * 2f) * 0.5f + 0.5f);
                bombRenderer.material.color = currentColor;
            }
            
            // Пульсация индикатора зоны
            if (zoneIndicatorInstance != null)
            {
                Renderer zoneRenderer = zoneIndicatorInstance.GetComponent<Renderer>();
                if (zoneRenderer != null)
                {
                    float alpha = zoneAlpha + Mathf.Sin(timer * 3f) * 0.2f;
                    alpha = Mathf.Clamp01(alpha);
                    Color zoneColorWithPulse = new Color(zoneColor.r, zoneColor.g, zoneColor.b, alpha);
                    zoneRenderer.material.color = zoneColorWithPulse;
                }
            }
            
            yield return null;
        }
    }
    
    private void OnDestroy()
    {
        if (zoneIndicatorInstance != null)
        {
            Destroy(zoneIndicatorInstance);
        }
        
        // Останавливаем корутину анимации
        if (_pulseCoroutine != null)
        {
            StopCoroutine(_pulseCoroutine);
        }
    }
    
    // Визуализация радиуса в редакторе
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, explosionRadius);
    }
}
