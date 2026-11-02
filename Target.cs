using UnityEngine;

public class ShootableTarget : MonoBehaviour
{
    [Header("Target Properties")]
    [SerializeField] private int maxHealth = 1;
    [SerializeField] private float bobSpeed = 2.5f;
    [SerializeField] private float bobHeight = 0.2f;
    [SerializeField] private float rotateSpeed = 40.0f;
    [SerializeField] private Transform visualTransform;

    private int hp;
    private Vector3 initialPos;
    private bool isDead = false;

    public bool IsDestroyed => isDead;

    private void Awake()
    {
        hp = maxHealth;
        if (visualTransform == null)
        {
            visualTransform = transform.childCount > 0 ? transform.GetChild(0) : transform;
        }
        initialPos = visualTransform.localPosition;
    }

    private void Update()
    {
        if (isDead) return;

        // Hover bob and rotation
        float newY = initialPos.y + Mathf.Sin((Time.time + initialPos.x * 2.3f) * bobSpeed) * bobHeight;
        visualTransform.localPosition = new Vector3(initialPos.x, newY, initialPos.z);
        visualTransform.Rotate(Vector3.up, rotateSpeed * Time.deltaTime, Space.World);
    }

    public void TakeHit(Vector3 hitPoint, Vector3 hitNormal)
    {
        if (isDead) return;

        hp--;
        if (hp <= 0)
        {
            DestroyTarget(hitPoint);
        }
    }

    private void DestroyTarget(Vector3 hitPoint)
    {
        isDead = true;

        SpawnShatterParticles(hitPoint);

        var paper = FindAnyObjectByType<LevelGoalPaper>();
        if (paper != null)
        {
            paper.OnTargetDestroyed(this);
        }

        var hud = FindAnyObjectByType<PlayerWeapon>();
        if (hud != null)
        {
            hud.OnTargetHitFeedback();
        }

        var col = GetComponent<Collider>();
        if (col != null) col.enabled = false;

        if (visualTransform != null)
        {
            visualTransform.gameObject.SetActive(false);
        }

        Destroy(gameObject, 0.8f);
    }

    private void SpawnShatterParticles(Vector3 pos)
{
        for (int i = 0; i < 7; i++)
        {
            GameObject shard = GameObject.CreatePrimitive(PrimitiveType.Cube);
            shard.name = "TargetShard";
            shard.transform.position = pos + Random.insideUnitSphere * 0.2f;
            shard.transform.localScale = Vector3.one * Random.Range(0.08f, 0.16f);

            var mat = GetComponentInChildren<MeshRenderer>()?.sharedMaterial;
            if (mat != null) shard.GetComponent<MeshRenderer>().sharedMaterial = mat;

            var col = shard.GetComponent<Collider>();
            if (col != null) Destroy(col);

            var rb = shard.AddComponent<Rigidbody>();
            rb.linearVelocity = (Random.onUnitSphere + Vector3.up * 0.5f) * Random.Range(3f, 7f);
            rb.angularVelocity = Random.insideUnitSphere * 15f;

            Destroy(shard, 1.2f);
        }
    }
}

