using UnityEngine;
using TMPro;
using System.Collections;

public class FloatingDamageText : MonoBehaviour
{
    public float moveSpeed = 1f;
    public float fadeOutTime = 1f;
    public Color damageColor = Color.red;

    private TextMeshPro _textMesh;
    private float _timer;
    private Vector3 _moveDirection;

    private void Awake()
    {
        _textMesh = GetComponent<TextMeshPro>();
        if (_textMesh == null)
        {
            Debug.LogError("FloatingDamageText script requires a TextMeshPro component on the same GameObject!");
            return;
        }

        _textMesh.color = damageColor;
    }

    public void SetDamageText(int damage)
    {
        _textMesh.text = damage.ToString();
    }

    public void SetMoveDirection(Vector3 direction)
    {
        _moveDirection = direction;
    }

    private void Update()
    {
        _timer += Time.deltaTime;

        // Перемещаем текст по заданному направлению
        transform.position += _moveDirection * moveSpeed * Time.deltaTime;

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