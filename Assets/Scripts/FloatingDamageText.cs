using UnityEngine;
using TMPro;
using System.Collections;

public class FloatingDamageText : MonoBehaviour
{
    // Настраиваемые переменные в инспекторе
    [Header("Movement Settings")]
    public float moveSpeed = 2f;
    public float moveRandomness = 1.5f; // Случайный множитель к скорости
    public Vector2 moveDirection = new Vector2(0f, 1f); // Направление движения

    [Header("Appearance Settings")]
    public float fadeOutTime = 0.5f;
    public Color damageColor = Color.red;
    public Color healColor = Color.green; // <-- Новая переменная для цвета лечения
    public float scaleUpTime = 0.2f; // Время для увеличения масштаба
    public float maxScale = 1.5f; // Максимальный масштаб

    [Header("Randomness")]
    public float horizontalRandomness = 1f; // Случайное смещение по горизонтали
    public float verticalRandomness = 0.5f; // Случайное смещение по вертикали
    public float rotationRandomness = 10f; // Случайный поворот

    private TextMeshPro _textMesh;
    private float _timer;
    private Vector3 _startPosition;
    private Vector3 _randomMoveDirection;
    private Vector3 _initialScale;

    private void Awake()
    {
        _textMesh = GetComponent<TextMeshPro>();
        if (_textMesh == null)
        {
            Debug.LogError("FloatingDamageText script requires a TextMeshPro component on the same GameObject!");
            Destroy(gameObject); // Уничтожаем объект, если компонент отсутствует
            return;
        }

        _initialScale = transform.localScale;

        // Определяем случайное направление движения
        _randomMoveDirection = new Vector3(
            moveDirection.x + Random.Range(-horizontalRandomness, horizontalRandomness),
            moveDirection.y + Random.Range(-verticalRandomness, verticalRandomness),
            0f
        ).normalized;

        // Применяем случайный поворот
        transform.rotation = Quaternion.Euler(0, 0, Random.Range(-rotationRandomness, rotationRandomness));
    }

    public void SetDamageText(int damage)
    {
        _textMesh.text = "-" + damage.ToString();
        _textMesh.color = damageColor;
    }

    public void SetHealText(int amount)
    {
        _textMesh.text = "+" + amount.ToString();
        _textMesh.color = healColor;
    }

    private void Update()
    {
        _timer += Time.deltaTime;

        // Плавно увеличиваем масштаб
        if (_timer < scaleUpTime)
        {
            float scaleProgress = _timer / scaleUpTime;
            transform.localScale = Vector3.Lerp(_initialScale, _initialScale * maxScale, scaleProgress);
        }
        else
        {
            // Плавно уменьшаем масштаб после фазы увеличения
            transform.localScale = Vector3.Lerp(_initialScale * maxScale, _initialScale, (_timer - scaleUpTime) / (fadeOutTime - scaleUpTime));
        }

        // Перемещаем текст
        transform.position += _randomMoveDirection * (moveSpeed + Random.Range(0, moveRandomness)) * Time.deltaTime;

        // Плавно уменьшаем прозрачность
        float alpha = Mathf.Lerp(1f, 0f, _timer / fadeOutTime);
        _textMesh.color = new Color(_textMesh.color.r, _textMesh.color.g, _textMesh.color.b, alpha);

        // Уничтожаем объект после исчезновения
        if (_timer >= fadeOutTime)
        {
            Destroy(gameObject);
        }
    }
}