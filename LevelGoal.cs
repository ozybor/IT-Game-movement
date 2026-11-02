using UnityEngine;
using TMPro;

public class LevelGoalPaper : MonoBehaviour
{
    [Header("Visual Effects")]
    [SerializeField] private float bobSpeed = 2.0f;
    [SerializeField] private float bobHeight = 0.12f;
    [SerializeField] private float rotateSpeed = 45.0f;
    [SerializeField] private Transform visualTransform;

    [Header("Shield & Targets")]
    [SerializeField] private GameObject shieldObj;
    [SerializeField] private TextMeshPro statusText;

    [Header("UI Reference")]
    [SerializeField] private LevelCompleteUI completeUI;

    private Vector3 initialPos;
    private bool isCollected = false;
    private int totalTargets = 0;
    private int remainingTargets = 0;
    private bool isShielded = false;
    private float denialTimer = 0f;

    public int TotalTargets => totalTargets;
    public int RemainingTargets => remainingTargets;
    public bool IsShielded => isShielded;

    private void Start()
    {
        if (visualTransform == null)
        {
            visualTransform = transform.childCount > 0 ? transform.GetChild(0) : transform;
        }

        initialPos = visualTransform.localPosition;

        if (completeUI == null)
        {
            completeUI = FindAnyObjectByType<LevelCompleteUI>();
        }

        InitializeTargetsAndShield();
    }

    private void InitializeTargetsAndShield()
    {
        var targets = FindObjectsByType<ShootableTarget>(FindObjectsInactive.Exclude);
        totalTargets = targets != null ? targets.Length : 0;
        remainingTargets = totalTargets;

        // Create shield sphere if not provided
        if (shieldObj == null)
        {
            shieldObj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            shieldObj.name = "EnergyShield";
            shieldObj.transform.SetParent(transform, false);
            shieldObj.transform.localPosition = new Vector3(0f, 1.2f, 0f);
            shieldObj.transform.localScale = Vector3.one * 1.5f;

            Shader urpLit = Shader.Find("Universal Render Pipeline/Lit");
            if (urpLit == null) urpLit = Shader.Find("Standard");
            Material shieldMat = new Material(urpLit);
            shieldMat.color = new Color(0.1f, 0.6f, 1f, 0.45f);
            shieldMat.SetFloat("_Mode", 3);
            shieldMat.EnableKeyword("_EMISSION");
            shieldMat.SetColor("_EmissionColor", new Color(0.2f, 0.7f, 1.0f) * 1.2f);
            shieldObj.GetComponent<MeshRenderer>().sharedMaterial = shieldMat;

            var col = shieldObj.GetComponent<Collider>();
            if (col != null) Destroy(col);
        }

        if (statusText == null)
        {
            GameObject txtObj = new GameObject("StatusFloatingText");
            txtObj.transform.SetParent(transform, false);
            txtObj.transform.localPosition = new Vector3(0f, 2.2f, 0f);
            statusText = txtObj.AddComponent<TextMeshPro>();
            statusText.font = TMP_Settings.defaultFontAsset;
            statusText.fontSize = 3;
            statusText.alignment = TextAlignmentOptions.Center;
        }

        if (totalTargets > 0)
        {
            isShielded = true;
            if (shieldObj != null) shieldObj.SetActive(true);
            UpdateStatusText();
        }
        else
        {
            isShielded = false;
            if (shieldObj != null) shieldObj.SetActive(false);
            if (statusText != null)
            {
                statusText.text = "<color=#55FF55>Ready</color>\nCollect the paper";
            }
        }
    }

    private void Update()
    {
        if (isCollected) return;

        if (denialTimer > 0f)
            denialTimer -= Time.deltaTime;

        // floating bob and spin
        float newY = initialPos.y + Mathf.Sin(Time.time * bobSpeed) * bobHeight;
        visualTransform.localPosition = new Vector3(initialPos.x, newY, initialPos.z);
        visualTransform.Rotate(Vector3.up, rotateSpeed * Time.deltaTime, Space.World);

        // Face text toward active player camera
        if (statusText != null && Camera.main != null)
        {
            statusText.transform.rotation = Quaternion.LookRotation(statusText.transform.position - Camera.main.transform.position);
        }

        // shield
        if (isShielded && shieldObj != null)
        {
            float pulse = 1.45f + Mathf.Sin(Time.time * 4f) * 0.08f;
            shieldObj.transform.localScale = Vector3.one * pulse;
        }
    }

    public void OnTargetDestroyed(ShootableTarget target)
    {
        remainingTargets = Mathf.Max(0, remainingTargets - 1);
        UpdateStatusText();

        if (remainingTargets <= 0 && isShielded)
        {
            UnlockShield();
        }
    }

    private void UpdateStatusText()
    {
        if (statusText == null) return;

        if (isShielded && remainingTargets > 0)
        {
            statusText.text = $"<color=#FF5555>Locked</color>\nTargets left: {remainingTargets}";
        }
        else
        {
            statusText.text = "<color=#55FF55>Ready</color>\nCollect the paper";
        }
    }

    private void UnlockShield()
    {
        isShielded = false;
        if (shieldObj != null)
        {
            shieldObj.SetActive(false);
        }

        PlayUnlockFanfare();
        UpdateStatusText();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (isCollected) return;

        bool isPlayer = other.CompareTag("Player") ||
                        other.GetComponent<AdvancedPlayerController>() != null ||
                        other.GetComponentInParent<AdvancedPlayerController>() != null ||
                        other.GetComponent<CharacterController>() != null;

        if (!isPlayer) return;

        if (isShielded)
        {
            OnShieldDenial();
            return;
        }

        CollectPaper();
    }

    private void OnShieldDenial()
    {
        if (denialTimer > 0f) return;
        denialTimer = 0.5f;

        PlayDenialSound();

        if (statusText != null)
        {
            statusText.text = $"<color=#FF5555>Locked</color>\nTargets left: {remainingTargets}";
        }
    }

    private void CollectPaper()
    {
        isCollected = true;
        PlayChimeSound();

        if (visualTransform != null) visualTransform.gameObject.SetActive(false);
        if (statusText != null) statusText.gameObject.SetActive(false);

        if (completeUI == null) completeUI = FindAnyObjectByType<LevelCompleteUI>();
        if (completeUI != null) completeUI.ShowCompletion();
    }

    private void PlayUnlockFanfare()
    {
        int sampleRate = 44100;
        float duration = 0.7f;
        int numSamples = Mathf.FloorToInt(sampleRate * duration);
        float[] samples = new float[numSamples];

        float[] chord = new float[] { 523.25f, 659.25f, 783.99f, 1046.50f };
        for (int i = 0; i < numSamples; i++)
        {
            float t = (float)i / sampleRate;
            float wave = 0f;
            for (int c = 0; c < chord.Length; c++)
            {
                if (t >= c * 0.08f)
                {
                    float noteT = t - c * 0.08f;
                    wave += Mathf.Sin(2f * Mathf.PI * chord[c] * noteT) * Mathf.Exp(-noteT * 5f);
                }
            }
            samples[i] = wave * 0.25f;
        }

        AudioClip clip = AudioClip.Create("ShieldUnlock", numSamples, 1, sampleRate, false);
        clip.SetData(samples, 0);
        AudioSource.PlayClipAtPoint(clip, transform.position, 1.0f);
    }

    private void PlayDenialSound()
    {
        int sampleRate = 44100;
        float duration = 0.2f;
        int numSamples = Mathf.FloorToInt(sampleRate * duration);
        float[] samples = new float[numSamples];

        for (int i = 0; i < numSamples; i++)
        {
            float t = (float)i / sampleRate;
            float envelope = Mathf.Exp(-t * 12f);
            samples[i] = Mathf.Sin(2f * Mathf.PI * 160f * t) * envelope * 0.5f;
        }

        AudioClip clip = AudioClip.Create("ShieldDenial", numSamples, 1, sampleRate, false);
        clip.SetData(samples, 0);
        AudioSource.PlayClipAtPoint(clip, transform.position, 0.8f);
    }

    private void PlayChimeSound()
    {
        int sampleRate = 44100;
        float duration = 0.5f;
        int numSamples = Mathf.FloorToInt(sampleRate * duration);
        float[] samples = new float[numSamples];

        float freq1 = 880f;
        float freq2 = 1320f;
        for (int i = 0; i < numSamples; i++)
        {
            float t = (float)i / sampleRate;
            float envelope = Mathf.Exp(-t * 6f);
            float wave = (Mathf.Sin(2f * Mathf.PI * freq1 * t) * 0.6f +
                          Mathf.Sin(2f * Mathf.PI * freq2 * t) * 0.4f) * envelope;
            samples[i] = wave * 0.45f;
        }

        AudioClip chime = AudioClip.Create("PaperPickupChime", numSamples, 1, sampleRate, false);
        chime.SetData(samples, 0);
        AudioSource.PlayClipAtPoint(chime, transform.position, 1.0f);
    }
}


