using UnityEngine;

public class OnEnter : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    // トリガーに入ったとき
    void OnTriggerEnter(Collider other)
    {
        if (other == null) return;
        Debug.Log($"[OnEnter] TriggerEnter -> Name: {other.name}, Tag: {other.tag}, Layer: {other.gameObject.layer}, Position: {other.transform.position}");
    }

    // トリガーから出たとき
    void OnTriggerExit(Collider other)
    {
        if (other == null) return;
        Debug.Log($"[OnEnter] TriggerExit  -> Name: {other.name}, Tag: {other.tag}, Layer: {other.gameObject.layer}");
    }

    // 衝突が開始したとき
    void OnCollisionEnter(Collision collision)
    {
        if (collision == null || collision.gameObject == null) return;
        Debug.Log($"[OnEnter] CollisionEnter -> Name: {collision.gameObject.name}, Tag: {collision.gameObject.tag}, Layer: {collision.gameObject.layer}, ContactCount: {collision.contactCount}");
    }

    // 衝突が終了したとき
    void OnCollisionExit(Collision collision)
    {
        if (collision == null || collision.gameObject == null) return;
        Debug.Log($"[OnEnter] CollisionExit  -> Name: {collision.gameObject.name}, Tag: {collision.gameObject.tag}, Layer: {collision.gameObject.layer}");
    }
}
