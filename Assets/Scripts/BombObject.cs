using UnityEngine;
using Mirror;
using System.Collections;

public class BombObject : NetworkBehaviour
{
    [Header("Bomb Settings")]
    [SyncVar] private int baseDamage;
    [SyncVar] private float damageMultiplier;
    [SyncVar] private float explosionRadius;
    [SyncVar] private float explosionDelay;
    [SyncVar] private string explosionEffectName;
    [SyncVar] private string zoneIndicatorName;
    [SyncVar] private Color zoneColor;
    [SyncVar] private float zoneAlpha;
    [SyncVar] private int casterTeam;
    [SyncVar] private NetworkIdentity casterIdentity;
    
    // Кэшированные префабы
    private GameObject explosionEffect;
    private GameObject zoneIndicator;
    
    [Header("Visual Components")]
    private GameObject zoneIndicatorInstance;
    private Renderer bombRenderer;
    private float timer;
    private bool hasExploded = false;
    
    [Header("Animation")]
    public float pulseSpeed = 2f;
    public float pulseIntensity = 0.3f;
    public Color warningColor = Color.red;
    
    
    [Server]
    public void Initialize(int damage, float multiplier, float radius, float delay, 
                          GameObject explosion, GameObject zone, Color color, float alpha,
                          int team, NetworkIdentity caster)
    {
        Debug.Log($"[BombObject] Initialize called on server, explosionEffect={explosion != null}, zoneIndicator={zone != null}");
        if (explosion != null) Debug.Log($"[BombObject] Received explosionEffect name: {explosion.name}");
        if (zone != null) Debug.Log($"[BombObject] Received zoneIndicator name: {zone.name}");
        
        baseDamage = damage;
        damageMultiplier = multiplier;
        explosionRadius = radius;
        explosionDelay = delay;
        
        // Сохраняем имена префабов для синхронизации
        explosionEffectName = explosion != null ? explosion.name : "";
        zoneIndicatorName = zone != null ? zone.name : "";
        
        // Кэшируем префабы на сервере
        explosionEffect = explosion;
        zoneIndicator = zone;
        
        zoneColor = color;
        zoneAlpha = alpha;
        casterTeam = team;
        casterIdentity = caster;
        
        // Создаем индикатор зоны только на сервере
        if (zoneIndicator != null)
        {
            CreateZoneIndicator();
        }
    }
    
    // Вызывается на клиентах после синхронизации SyncVar
    public override void OnStartClient()
    {
        base.OnStartClient();
        
        // Загружаем префабы по именам на клиентах
        LoadPrefabsFromNames();
    }
    
    private void LoadPrefabsFromNames()
    {
        // Загружаем префабы по именам из Resources/VFX/
        if (!string.IsNullOrEmpty(explosionEffectName))
        {
            explosionEffect = Resources.Load<GameObject>($"VFX/{explosionEffectName}");
        }
        
        if (!string.IsNullOrEmpty(zoneIndicatorName))
        {
            zoneIndicator = Resources.Load<GameObject>($"VFX/{zoneIndicatorName}");
        }
    }
    
    private void CreateZoneIndicator()
    {
        if (zoneIndicator != null)
        {
            zoneIndicatorInstance = Instantiate(zoneIndicator, transform.position, Quaternion.identity);
            zoneIndicatorInstance.transform.SetParent(transform);
            
            // Настраиваем размер зоны (радиус * 2 для диаметра)
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
        
        // Префаб бомбы остается исходного размера
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
        
        bool casterAlive = casterIdentity != null && casterIdentity.GetComponent<PlayerCore>() != null && !casterIdentity.GetComponent<PlayerCore>().isDead;
        Debug.Log($"[BombObject] Exploding bomb, caster: {casterIdentity?.name}, caster alive: {casterAlive}");
        
        // Bomb exploding
        
        // Оптимизация: кэшируем LayerMask и используем более эффективный поиск
        int explosionLayerMask = LayerMask.GetMask("Player", "Ignore Raycast", "Monster", "Enemy");
        Collider[] hitColliders = new Collider[50]; // Предварительно выделенный массив
        int hitCount = Physics.OverlapSphereNonAlloc(transform.position, explosionRadius, hitColliders, explosionLayerMask);
        
        for (int i = 0; i < hitCount; i++)
        {
            Collider col = hitColliders[i];
            // Проверяем, что это не сам кастер (если кастер еще жив)
            if (casterIdentity != null && col.GetComponent<NetworkIdentity>() == casterIdentity) 
            {
                Debug.Log($"[BombObject] Skipping caster: {casterIdentity.name}");
                continue;
            }
            
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
        Debug.Log($"[BombObject] Calling RpcPlayExplosionEffect, explosionEffect: {explosionEffect != null}");
        RpcPlayExplosionEffect();
        
        // Уничтожаем бомбу с задержкой, чтобы RPC успел отправиться
        StartCoroutine(DestroyAfterDelay(0.1f));
    }
    
    private IEnumerator DestroyAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        NetworkServer.Destroy(gameObject);
    }
    
    [ClientRpc]
    private void RpcPlayExplosionEffect()
    {
        if (explosionEffect != null)
        {
            GameObject explosion = Instantiate(explosionEffect, transform.position, Quaternion.identity);
            
            if (explosion != null)
            {
                // Масштабируем эффект взрыва в соответствии с радиусом
                float baseRadius = 2.0f;
                float effectScale = explosionRadius / baseRadius;
                effectScale = Mathf.Clamp(effectScale, 0.5f, 3.0f);
                explosion.transform.localScale = Vector3.one * effectScale;
                
                // Запускаем частицы если они не играют
                ParticleSystem[] particles = explosion.GetComponentsInChildren<ParticleSystem>();
                foreach (var ps in particles)
                {
                    if (!ps.isPlaying)
                    {
                        ps.Play();
                    }
                }
                
                // Уничтожаем эффект через время
                Destroy(explosion, 5f);
            }
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
