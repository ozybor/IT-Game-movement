using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class PlayerWeapon : MonoBehaviour
{
    [Header("Weapon Settings")]
    [SerializeField] private float fireRate = 0.18f;
    [SerializeField] private float range = 80.0f;
    [SerializeField] private LayerMask hitLayers = ~0;

    [Header("Gun Model & Visuals")]
    [SerializeField] private Vector3 gunOffset = new Vector3(0.24f, -0.22f, 0.48f);
    [SerializeField] private Vector3 recoilKick = new Vector3(0f, 0.02f, -0.06f);
    [SerializeField] private float recoilReturnSpeed = 14.0f;

    private Camera cam;
    private AdvancedPlayerController player;
    private Transform gunRoot;
    private Transform muzzle;
    private Light muzzleFlash;
    private LineRenderer tracer;

    private float nextFireTime = 0f;
    private Vector3 recoilPos;
    private float recoilRot;
    private float flashTimer = 0f;
    private float tracerTimer = 0f;
    private float hitmarkerTimer = 0f;

    [Header("Sound")]
    [SerializeField] private AudioClip shootSound;
    [SerializeField, Range(0f, 1f)] private float shootVolume = 0.8f;

    private AudioSource gunAudio;
    private GUIStyle hudStyle;
    private LevelGoalPaper goal;

    private void Awake()
    {
        cam = GetComponentInChildren<Camera>();
        if (cam == null && Camera.main != null) cam = Camera.main;

        player = GetComponent<AdvancedPlayerController>();
        gunAudio = GetComponent<AudioSource>();
        if (gunAudio == null) gunAudio = gameObject.AddComponent<AudioSource>();
        gunAudio.playOnAwake = false;
        gunAudio.spatialBlend = 0f;
        BuildGunModel();
    }

    private void Start()
    {
        goal = FindAnyObjectByType<LevelGoalPaper>();
    }

    private void Update()
    {
        if (Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame)
        {
            RestartLevel();
            return;
        }

        // Gun recoil recovery
        recoilPos = Vector3.MoveTowards(recoilPos, Vector3.zero, recoilReturnSpeed * Time.deltaTime);
        recoilRot = Mathf.MoveTowards(recoilRot, 0f, recoilReturnSpeed * 40f * Time.deltaTime);
        if (gunRoot != null)
        {
            gunRoot.localPosition = gunOffset + recoilPos;
            gunRoot.localRotation = Quaternion.Euler(-recoilRot, 0f, 0f);
        }

        if (flashTimer > 0f)
        {
            flashTimer -= Time.deltaTime;
            if (flashTimer <= 0f && muzzleFlash != null)
                muzzleFlash.enabled = false;
        }

        if (tracerTimer > 0f)
        {
            tracerTimer -= Time.deltaTime;
            if (tracerTimer <= 0f && tracer != null)
                tracer.enabled = false;
        }

        if (hitmarkerTimer > 0f)
            hitmarkerTimer -= Time.deltaTime;

        HandleShootingInput();
    }

    private void HandleShootingInput()
    {
        var mouse = Mouse.current;
        if (mouse == null) return;

        bool shootPressed = mouse.leftButton.wasPressedThisFrame || mouse.leftButton.isPressed;

        if (Time.timeScale > 0f && shootPressed && Cursor.lockState == CursorLockMode.Locked && Time.time >= nextFireTime)
        {
            FireWeapon();
        }
    }

    private void FireWeapon()
    {
        nextFireTime = Time.time + fireRate;

        // Recoil kick & camera snap
        recoilPos = recoilKick;
        recoilRot = 4.5f;

        if (muzzleFlash != null)
        {
            muzzleFlash.enabled = true;
            flashTimer = 0.05f;
        }

        PlayLaserSound();

        Vector3 rayOrigin = cam != null ? cam.transform.position : transform.position + Vector3.up * 1.6f;
        Vector3 rayDir = cam != null ? cam.transform.forward : transform.forward;
        Vector3 endPoint = rayOrigin + rayDir * range;

        if (Physics.Raycast(rayOrigin, rayDir, out RaycastHit hit, range, hitLayers, QueryTriggerInteraction.Ignore))
        {
            endPoint = hit.point;
            var target = hit.collider.GetComponent<ShootableTarget>();
            if (target == null) target = hit.collider.GetComponentInParent<ShootableTarget>();

            if (target != null && !target.IsDestroyed)
            {
                target.TakeHit(hit.point, hit.normal);
                OnTargetHitFeedback();
            }
            else
            {
                SpawnImpactSpark(hit.point, hit.normal);
            }
        }

        if (tracer != null && muzzle != null)
        {
            tracer.SetPosition(0, muzzle.position);
            tracer.SetPosition(1, endPoint);
            tracer.enabled = true;
            tracerTimer = 0.05f;
        }
    }

    public void OnTargetHitFeedback()
    {
        hitmarkerTimer = 0.2f;
    }

    public void RestartLevel()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    private void BuildGunModel()
    {
        if (cam == null) return;

        Transform existing = cam.transform.Find("Weapon");
        if (existing != null) Destroy(existing.gameObject);

        // Weapon Root
        GameObject weaponObj = new GameObject("Weapon");
        weaponObj.transform.SetParent(cam.transform, false);
        weaponObj.transform.localPosition = gunOffset;
        weaponObj.transform.localRotation = Quaternion.identity;
        gunRoot = weaponObj.transform;

        // Materials
        Shader litShader = Shader.Find("Universal Render Pipeline/Lit");
        if (litShader == null) litShader = Shader.Find("Standard");

        Material bodyMat = new Material(litShader);
        bodyMat.color = new Color(0.12f, 0.14f, 0.18f);
        bodyMat.SetFloat("_Smoothness", 0.6f);

        Material neonMat = new Material(litShader);
        neonMat.color = new Color(0.1f, 0.85f, 1f);
        neonMat.EnableKeyword("_EMISSION");
        neonMat.SetColor("_EmissionColor", new Color(0.1f, 0.85f, 1f) * 2f);

        // Gun Receiver
        GameObject body = GameObject.CreatePrimitive(PrimitiveType.Cube);
        body.name = "Body";
        body.transform.SetParent(gunRoot, false);
        body.transform.localPosition = new Vector3(0f, 0f, 0.1f);
        body.transform.localScale = new Vector3(0.06f, 0.08f, 0.28f);
        body.GetComponent<MeshRenderer>().sharedMaterial = bodyMat;
        Destroy(body.GetComponent<Collider>());

        // Barrel
        GameObject barrel = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        barrel.name = "Barrel";
        barrel.transform.SetParent(gunRoot, false);
        barrel.transform.localPosition = new Vector3(0f, 0.02f, 0.28f);
        barrel.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        barrel.transform.localScale = new Vector3(0.035f, 0.09f, 0.035f);
        barrel.GetComponent<MeshRenderer>().sharedMaterial = bodyMat;
        Destroy(barrel.GetComponent<Collider>());

        // Trim
        GameObject neon = GameObject.CreatePrimitive(PrimitiveType.Cube);
        neon.name = "Trim";
        neon.transform.SetParent(gunRoot, false);
        neon.transform.localPosition = new Vector3(0f, 0.045f, 0.12f);
        neon.transform.localScale = new Vector3(0.02f, 0.015f, 0.18f);
        neon.GetComponent<MeshRenderer>().sharedMaterial = neonMat;
        Destroy(neon.GetComponent<Collider>());

        // Grip
        GameObject grip = GameObject.CreatePrimitive(PrimitiveType.Cube);
        grip.name = "Grip";
        grip.transform.SetParent(gunRoot, false);
        grip.transform.localPosition = new Vector3(0f, -0.07f, 0.04f);
        grip.transform.localRotation = Quaternion.Euler(20f, 0f, 0f);
        grip.transform.localScale = new Vector3(0.05f, 0.11f, 0.05f);
        grip.GetComponent<MeshRenderer>().sharedMaterial = bodyMat;
        Destroy(grip.GetComponent<Collider>());

        // Muzzle
        GameObject mObj = new GameObject("Muzzle");
        mObj.transform.SetParent(gunRoot, false);
        mObj.transform.localPosition = new Vector3(0f, 0.02f, 0.38f);
        muzzle = mObj.transform;

        // Flash
        GameObject flashLightObj = new GameObject("Flash");
        flashLightObj.transform.SetParent(muzzle, false);
        muzzleFlash = flashLightObj.AddComponent<Light>();
        muzzleFlash.type = LightType.Point;
        muzzleFlash.color = new Color(0.2f, 0.9f, 1f);
        muzzleFlash.range = 3.5f;
        muzzleFlash.intensity = 2.5f;
        muzzleFlash.enabled = false;

        // Tracer LineRenderer
        tracer = weaponObj.AddComponent<LineRenderer>();
        tracer.startWidth = 0.03f;
        tracer.endWidth = 0.015f;
        tracer.material = neonMat;
        tracer.positionCount = 2;
        tracer.enabled = false;
    }

    private void PlayLaserSound()
    {
        if (shootSound != null && gunAudio != null)
            gunAudio.PlayOneShot(shootSound, shootVolume);
    }

    private void SpawnImpactSpark(Vector3 pos, Vector3 normal)
    {
        GameObject spark = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        spark.name = "ImpactSpark";
        spark.transform.position = pos + normal * 0.05f;
        spark.transform.localScale = Vector3.one * 0.12f;

        Shader litShader = Shader.Find("Universal Render Pipeline/Lit");
        if (litShader == null) litShader = Shader.Find("Standard");
        Material mat = new Material(litShader);
        mat.color = new Color(0.2f, 0.8f, 1f);
        mat.EnableKeyword("_EMISSION");
        mat.SetColor("_EmissionColor", new Color(0.2f, 0.8f, 1f) * 2f);
        spark.GetComponent<MeshRenderer>().sharedMaterial = mat;

        Destroy(spark.GetComponent<Collider>());
        Destroy(spark, 0.08f);
    }

    private void OnGUI()
    {
        if (Cursor.lockState != CursorLockMode.Locked) return;

        // Crosshair in screen center
        float cx = Screen.width * 0.5f;
        float cy = Screen.height * 0.5f;
        float size = 8f;
        float thickness = 2f;
        float gap = 4f;

        Color chColor = hitmarkerTimer > 0f ? new Color(1f, 0.25f, 0.25f, 0.95f) : new Color(0.4f, 0.95f, 1f, 0.85f);

        Texture2D tex = Texture2D.whiteTexture;
        Color oldColor = GUI.color;
        GUI.color = chColor;

        GUI.DrawTexture(new Rect(cx - thickness * 0.5f, cy - gap - size, thickness, size), tex);
        GUI.DrawTexture(new Rect(cx - thickness * 0.5f, cy + gap, thickness, size), tex);
        GUI.DrawTexture(new Rect(cx - gap - size, cy - thickness * 0.5f, size, thickness), tex);
        GUI.DrawTexture(new Rect(cx + gap, cy - thickness * 0.5f, size, thickness), tex);
        GUI.DrawTexture(new Rect(cx - 1f, cy - 1f, 2f, 2f), tex);

        // Targets
        if (hudStyle == null)
        {
            hudStyle = new GUIStyle(GUI.skin.label);
            hudStyle.fontSize = 16;
            hudStyle.alignment = TextAnchor.MiddleRight;
            hudStyle.normal.textColor = Color.white;
        }

        GUI.color = oldColor;
        if (goal != null && goal.TotalTargets > 0)
        {
            string text = goal.RemainingTargets > 0
                ? $"Targets: {goal.TotalTargets - goal.RemainingTargets}/{goal.TotalTargets}"
                : "Collect the paper";
            GUI.Label(new Rect(Screen.width - 240, 20, 220, 30), text, hudStyle);
        }
}
}

