using UnityEngine;
using TMPro; // Эта строка очень важна!

public class FloatingDamageText : MonoBehaviour
{
    public float moveSpeed = 1f;
    public float fadeOutTime = 1f;
    public Color damageColor = Color.red;

    private TextMeshPro _textMesh;
    private float _initialY;
    private float _timer;

    private void Awake()
    {
        // Проверяем, что компонент TextMeshPro существует на этом же GameObject
        _textMesh = GetComponent<TextMeshPro>();
        if (_textMesh == null)
        {
            Debug.LogError("FloatingDamageText script requires a TextMeshPro component on the same GameObject!");
            return;
        }

        _textMesh.color = damageColor;
        _initialY = transform.position.y;
    }

    public void SetDamageText(int damage)
    {
        if (_textMesh != null)
        {
            _textMesh.text = damage.ToString();
        }
    }

    private void Update()
    {
        _timer += Time.deltaTime;

        // Перемещаем текст вверх
        transform.position += new Vector3(0, moveSpeed * Time.deltaTime, 0);

        // Плавно уменьшаем прозрачность
        if (_textMesh != null)
        {
            float alpha = Mathf.Lerp(1f, 0f, _timer / fadeOutTime);
            _textMesh.color = new Color(_textMesh.color.r, _textMesh.color.g, _textMesh.color.b, alpha);
        }

        // Уничтожаем объект после исчезновения
        if (_timer >= fadeOutTime)
        {
            Destroy(gameObject);
        }
    }
}