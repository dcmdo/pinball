using UnityEngine;

public class SlingshotController : MonoBehaviour
{
    [Header("弹弓参数")]
    public float maxDragDistance = 2f;
    public float forceMultiplier = 10f;
    public LineRenderer elasticLine;

    private Vector3 startPos;
    private Rigidbody rb;
    // private bool isDragging;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        startPos = transform.position;
        elasticLine.positionCount = 2;
        elasticLine.SetPosition(0, startPos);
    }

    void OnMouseDown()
    {
        // isDragging = true;
        rb.isKinematic = true;
    }

    void OnMouseDrag()
    {
        Vector3 mousePos = GetMouseWorldPos();
        mousePos.z = startPos.z; // 保持Z轴一致
        
        // 限制拖拽范围
        Vector3 dragOffset = mousePos - startPos;
        if (dragOffset.magnitude > maxDragDistance)
        {
            mousePos = startPos + dragOffset.normalized * maxDragDistance;
        }

        transform.position = mousePos;
        UpdateElasticLine();
    }

    void OnMouseUp()
    {
        // isDragging = false;
        rb.isKinematic = false;
        
        Vector3 releaseVector = startPos - transform.position;
        rb.AddForce(releaseVector * forceMultiplier, ForceMode.Impulse);
        
        elasticLine.enabled = false;
    }

    void UpdateElasticLine()
    {
        elasticLine.enabled = true;
        elasticLine.SetPosition(1, transform.position);
    }

    Vector3 GetMouseWorldPos()
    {
        Vector3 mousePos = Input.mousePosition;
        mousePos.z = Camera.main.WorldToScreenPoint(transform.position).z;
        return Camera.main.ScreenToWorldPoint(mousePos);
    }
}
