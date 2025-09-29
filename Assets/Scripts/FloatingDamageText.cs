using UnityEngine;
using TMPro;
using System.Collections;

public class FloatingDamageText : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 2f; // Base speed of the text movement
    public float moveRandomness = 1.5f; // Random addition to speed (0 to this value)
    public Vector2 moveDirection = new Vector2(0f, 1f); // Base direction vector (x horizontal, y vertical)
    [Header("Appearance Settings")]
    public float lifetime = 0.7f; // Total time before destruction (adjust for overall duration)
    public float appearTime = 0.2f; // Time to appear from 0 to max scale (delay at start)
    public float holdTime = 0.3f; // Time to hold full size and visibility after appear, before fade
    public float fadeOutTime = 0.2f; // Time for quick fade out at the end (must be < lifetime - appearTime - holdTime)
    public float maxScale = 3f; // Maximum scale factor (appears to this)
    public float startOffsetY = 1f; // Initial vertical offset (appears higher)
    [Header("Color Settings")]
    public Color healColor = Color.green; // Color for heal text
    public Color otherPlayerDamageColor = Color.white; // Color for damage from other players
    public Color missColor = Color.cyan; // Color for miss text
    [Header("Gradient Settings")]
    public bool useDamageGradient = true; // Enable gradient for own damage
    public bool useHealGradient = false; // Enable gradient for heal text
    public bool useMissGradient = false; // Enable gradient for miss text
    public Color damageGradientTop = Color.red; // Top color for damage text gradient
    public Color damageGradientBottom = new Color(1f, 0.5f, 0f, 1f); // Orange color for damage text gradient
    public Color healGradientTop = Color.green; // Top color for heal text gradient
    public Color healGradientBottom = Color.black; // Bottom color for heal text gradient
    public Color missGradientTop = Color.cyan; // Top color for miss text gradient
    public Color missGradientBottom = Color.black; // Bottom color for miss text gradient
    [Header("Randomness")]
    public float horizontalRandomness = 1f; // Random horizontal offset in direction
    public float verticalRandomness = 0.5f; // Random vertical offset in direction
    public float rotationRandomness = 10f; // Random rotation angle (� this value)
    [Header("Text Settings")]
    public int normalFontSize = 7; // Font size for normal text
    public int criticalFontSize = 8; // Font size for critical text
    [Header("Outline Settings")]
    public Color ownDamageOutlineColor = Color.white; // Outline color for own damage
    public Color receivedDamageOutlineColor = Color.red; // Outline color for received damage
    public float outlineWidth = 0.2f; // Outline width
    private TextMeshPro _textMesh;
    private float _timer;
    private Vector3 _randomMoveDirection;
    private Vector3 _initialScale;
    // Убираем _originalColor, используем настройки из скрипта

    private void Awake()
    {
        _textMesh = GetComponent<TextMeshPro>();
        if (_textMesh == null)
        {
            Debug.LogError("FloatingDamageText script requires a TextMeshPro component!");
            Destroy(gameObject);
            return;
        }
        // Убираем сохранение цвета из префаба, используем настройки из скрипта
        _initialScale = transform.localScale;
        
        // Настраиваем outline
        _textMesh.outlineWidth = outlineWidth;
        transform.position += Vector3.up * startOffsetY; // Appear higher
        transform.localScale = Vector3.zero; // Start at zero scale
        // Random movement direction
        _randomMoveDirection = new Vector3(
            moveDirection.x + Random.Range(-horizontalRandomness, horizontalRandomness),
            moveDirection.y + Random.Range(-verticalRandomness, verticalRandomness),
            0f
        ).normalized;
        // Random rotation
        transform.rotation = Quaternion.Euler(0, 0, Random.Range(-rotationRandomness, rotationRandomness));
    }

    public void SetDamageText(int damage, bool isCritical = false, bool isOtherPlayer = false, bool isReceivedDamage = false)
    {
        _textMesh.text = damage.ToString(); // Just the number, no sign
        
        // Определяем тип урона и настраиваем цвет и outline
        if (isReceivedDamage)
        {
            // Урон, который получаю я - белый с красной обводкой
            _textMesh.color = Color.white;
            _textMesh.colorGradient = new VertexGradient(Color.white, Color.white, Color.white, Color.white);
            _textMesh.outlineColor = receivedDamageOutlineColor;
        }
        else if (isOtherPlayer)
        {
            // Урон, который наносят другие игроки другим - белый с красной обводкой
            _textMesh.color = Color.white;
            _textMesh.colorGradient = new VertexGradient(Color.white, Color.white, Color.white, Color.white);
            _textMesh.outlineColor = receivedDamageOutlineColor;
        }
        else
        {
            // Урон, который наношу я - градиент красный-оранжевый с белой обводкой
            if (useDamageGradient)
            {
                _textMesh.colorGradient = new VertexGradient(damageGradientTop, damageGradientTop, damageGradientBottom, damageGradientBottom);
                _textMesh.color = Color.white; // Сбрасываем цвет, чтобы градиент работал
            }
            else
            {
                _textMesh.color = damageGradientTop;
                _textMesh.colorGradient = new VertexGradient(Color.white, Color.white, Color.white, Color.white);
            }
            _textMesh.outlineColor = ownDamageOutlineColor;
        }
        
        if (isCritical)
        {
            _textMesh.fontSize = criticalFontSize;
            _textMesh.fontStyle = FontStyles.Bold;
        }
        else
        {
            _textMesh.fontSize = normalFontSize;
            _textMesh.fontStyle = FontStyles.Normal;
        }
        
        Debug.Log($"[FloatingDamageText] Set damage text: {damage}, isCritical: {isCritical}, isOtherPlayer: {isOtherPlayer}, isReceivedDamage: {isReceivedDamage}");
    }

    public void SetHealText(int amount)
    {
        _textMesh.text = amount.ToString(); // Just the number, no sign
        if (useHealGradient)
        {
            _textMesh.colorGradient = new VertexGradient(healGradientTop, healGradientTop, healGradientBottom, healGradientBottom);
            _textMesh.color = Color.white; // Сбрасываем цвет, чтобы градиент работал
            Debug.Log($"[FloatingDamageText] Set heal text with gradient");
        }
        else
        {
            _textMesh.color = healColor; // Зеленый для лечения
            _textMesh.colorGradient = new VertexGradient(Color.white, Color.white, Color.white, Color.white); // Сбрасываем градиент
            Debug.Log($"[FloatingDamageText] Set heal text with color: {healColor}");
        }
        _textMesh.fontSize = normalFontSize;
    }

    public void SetMissText()
    {
        _textMesh.text = "MISS";
        if (useMissGradient)
        {
            _textMesh.colorGradient = new VertexGradient(missGradientTop, missGradientTop, missGradientBottom, missGradientBottom);
            _textMesh.color = Color.white; // Сбрасываем цвет, чтобы градиент работал
            Debug.Log($"[FloatingDamageText] Set miss text with gradient");
        }
        else
        {
            _textMesh.color = missColor; // Голубой для промаха
            _textMesh.colorGradient = new VertexGradient(Color.white, Color.white, Color.white, Color.white); // Сбрасываем градиент
            Debug.Log($"[FloatingDamageText] Set miss text with color: {missColor}");
        }
        _textMesh.fontSize = normalFontSize;
        _textMesh.fontStyle = FontStyles.Bold;
    }

    private void Update()
    {
        _timer += Time.deltaTime;
        // Appear animation (scale up)
        if (_timer < appearTime)
        {
            float appearProgress = _timer / appearTime;
            transform.localScale = Vector3.Lerp(Vector3.zero, _initialScale * maxScale, appearProgress);
            return; // Delay movement during appear
        }
        // Hold phase (no change)
        else if (_timer < appearTime + holdTime)
        {
            transform.localScale = _initialScale * maxScale;
            // Constant movement during hold
            transform.position += _randomMoveDirection * (moveSpeed + Random.Range(0, moveRandomness)) * Time.deltaTime;
            return;
        }
        // Fade out animation (scale down and alpha fade)
        if (_timer > lifetime - fadeOutTime)
        {
            float fadeProgress = (_timer - (lifetime - fadeOutTime)) / fadeOutTime;
            transform.localScale = Vector3.Lerp(_initialScale * maxScale, Vector3.zero, fadeProgress);
            // Also fade alpha
            float alpha = Mathf.Lerp(1f, 0f, fadeProgress);
            if (useDamageGradient && !_textMesh.colorGradient.Equals(null))
            {
                VertexGradient gradient = _textMesh.colorGradient;
                gradient.topLeft.a = alpha;
                gradient.topRight.a = alpha;
                gradient.bottomLeft.a = alpha;
                gradient.bottomRight.a = alpha;
                _textMesh.colorGradient = gradient;
            }
            else
            {
                _textMesh.color = new Color(_textMesh.color.r, _textMesh.color.g, _textMesh.color.b, alpha);
            }
        }
        // Constant movement
        transform.position += _randomMoveDirection * (moveSpeed + Random.Range(0, moveRandomness)) * Time.deltaTime;
        // Destroy after lifetime
        if (_timer >= lifetime)
        {
            Destroy(gameObject);
        }
    }
}