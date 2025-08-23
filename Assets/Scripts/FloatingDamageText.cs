using UnityEngine;
using TMPro;
using System.Collections;

public class FloatingDamageText : MonoBehaviour
{
    // ������������� ���������� � ����������
    [Header("Movement Settings")]
    public float moveSpeed = 2f;
    public float moveRandomness = 1.5f; // ��������� ��������� � ��������
    public Vector2 moveDirection = new Vector2(0f, 1f); // ����������� ��������

    [Header("Appearance Settings")]
    public float fadeOutTime = 0.5f;
    public Color damageColor = Color.red;
    public Color healColor = Color.green; // <-- ����� ���������� ��� ����� �������
    public float scaleUpTime = 0.2f; // ����� ��� ���������� ��������
    public float maxScale = 1.5f; // ������������ �������

    [Header("Randomness")]
    public float horizontalRandomness = 1f; // ��������� �������� �� �����������
    public float verticalRandomness = 0.5f; // ��������� �������� �� ���������
    public float rotationRandomness = 10f; // ��������� �������

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
            Destroy(gameObject); // ���������� ������, ���� ��������� �����������
            return;
        }

        _initialScale = transform.localScale;

        // ���������� ��������� ����������� ��������
        _randomMoveDirection = new Vector3(
            moveDirection.x + Random.Range(-horizontalRandomness, horizontalRandomness),
            moveDirection.y + Random.Range(-verticalRandomness, verticalRandomness),
            0f
        ).normalized;

        // ��������� ��������� �������
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

        // ������ ����������� �������
        if (_timer < scaleUpTime)
        {
            float scaleProgress = _timer / scaleUpTime;
            transform.localScale = Vector3.Lerp(_initialScale, _initialScale * maxScale, scaleProgress);
        }
        else
        {
            // ������ ��������� ������� ����� ���� ����������
            transform.localScale = Vector3.Lerp(_initialScale * maxScale, _initialScale, (_timer - scaleUpTime) / (fadeOutTime - scaleUpTime));
        }

        // ���������� �����
        transform.position += _randomMoveDirection * (moveSpeed + Random.Range(0, moveRandomness)) * Time.deltaTime;

        // ������ ��������� ������������
        float alpha = Mathf.Lerp(1f, 0f, _timer / fadeOutTime);
        _textMesh.color = new Color(_textMesh.color.r, _textMesh.color.g, _textMesh.color.b, alpha);

        // ���������� ������ ����� ������������
        if (_timer >= fadeOutTime)
        {
            Destroy(gameObject);
        }
    }
}