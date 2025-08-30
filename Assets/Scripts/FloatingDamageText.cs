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
    public Color damageColor = Color.red; // Color for damage text
    public Color healColor = Color.green; // Color for heal text
    public Color criticalColor = Color.yellow; // Color for critical hits
    public float maxScale = 3f; // Maximum scale factor (appears to this)
    public float startOffsetY = 1f; // Initial vertical offset (appears higher)
    [Header("Randomness")]
    public float horizontalRandomness = 1f; // Random horizontal offset in direction
    public float verticalRandomness = 0.5f; // Random vertical offset in direction
    public float rotationRandomness = 10f; // Random rotation angle (± this value)
    [Header("Text Settings")]
    public int normalFontSize = 7; // Font size for normal text
    public int criticalFontSize = 8; // Font size for critical text
    private TextMeshPro _textMesh;
    private float _timer;
    private Vector3 _randomMoveDirection;
    private Vector3 _initialScale;
    private void Awake()
    {
        _textMesh = GetComponent<TextMeshPro>();
        if (_textMesh == null)
        {
            Debug.LogError("FloatingDamageText script requires a TextMeshPro component!");
            Destroy(gameObject);
            return;
        }
        _initialScale = transform.localScale;
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
    public void SetDamageText(int damage, bool isCritical = false)
    {
        _textMesh.text = damage.ToString(); // Just the number, no sign
        if (isCritical)
        {
            _textMesh.color = criticalColor;
            _textMesh.fontSize = criticalFontSize;
            _textMesh.fontStyle = FontStyles.Bold;
        }
        else
        {
            _textMesh.color = damageColor;
            _textMesh.fontSize = normalFontSize;
        }
    }
    public void SetHealText(int amount)
    {
        _textMesh.text = amount.ToString(); // Just the number, no sign
        _textMesh.color = healColor;
        _textMesh.fontSize = normalFontSize;
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
            _textMesh.color = new Color(_textMesh.color.r, _textMesh.color.g, _textMesh.color.b, alpha);
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