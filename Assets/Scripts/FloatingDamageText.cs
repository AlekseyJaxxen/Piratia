using UnityEngine;
using TMPro;
using System.Collections;

public class FloatingDamageText : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 2f;
    public float moveRandomness = 1.5f;
    public Vector2 moveDirection = new Vector2(0f, 1f);

    [Header("Appearance Settings")]
    public float fadeOutTime = 1.5f;
    public Color damageColor = Color.red;
    public Color healColor = Color.green;
    public Color criticalColor = Color.yellow;
    public float scaleUpTime = 0.2f;
    public float maxScale = 1.5f;

    [Header("Randomness")]
    public float horizontalRandomness = 1f;
    public float verticalRandomness = 0.5f;
    public float rotationRandomness = 10f;

    [Header("Text Settings")]
    public int normalFontSize = 4;
    public int criticalFontSize = 6;

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
        transform.localScale = Vector3.zero;

        // Случайное направление движения
        _randomMoveDirection = new Vector3(
            moveDirection.x + Random.Range(-horizontalRandomness, horizontalRandomness),
            moveDirection.y + Random.Range(-verticalRandomness, verticalRandomness),
            0f
        ).normalized;

        // Случайный поворот
        transform.rotation = Quaternion.Euler(0, 0, Random.Range(-rotationRandomness, rotationRandomness));
    }

    public void SetDamageText(int damage, bool isCritical = false)
    {
        _textMesh.text = "-" + damage.ToString();

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
        _textMesh.text = "+" + amount.ToString();
        _textMesh.color = healColor;
        _textMesh.fontSize = normalFontSize;
    }

    private void Update()
    {
        _timer += Time.deltaTime;

        // Анимация появления (увеличение масштаба)
        if (_timer < scaleUpTime)
        {
            float scaleProgress = _timer / scaleUpTime;
            transform.localScale = Vector3.Lerp(Vector3.zero, _initialScale * maxScale, scaleProgress);
        }
        // Анимация исчезновения (уменьшение масштаба и прозрачности)
        else if (_timer > fadeOutTime - scaleUpTime)
        {
            float fadeProgress = (_timer - (fadeOutTime - scaleUpTime)) / scaleUpTime;
            transform.localScale = Vector3.Lerp(_initialScale * maxScale, Vector3.zero, fadeProgress);

            // Также уменьшаем прозрачность
            float alpha = Mathf.Lerp(1f, 0f, fadeProgress);
            _textMesh.color = new Color(_textMesh.color.r, _textMesh.color.g, _textMesh.color.b, alpha);
        }
        else
        {
            // Поддерживаем максимальный масштаб
            transform.localScale = _initialScale * maxScale;
        }

        // Постоянное движение
        transform.position += _randomMoveDirection * (moveSpeed + Random.Range(0, moveRandomness)) * Time.deltaTime;

        // Уничтожаем объект после завершения анимации
        if (_timer >= fadeOutTime)
        {
            Destroy(gameObject);
        }
    }
}