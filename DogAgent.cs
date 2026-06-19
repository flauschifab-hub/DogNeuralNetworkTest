using UnityEngine;

public class DogAgent : MonoBehaviour
{
    [SerializeField] private string dogPartLayerName = "DogPart";


    private DogBody body;
    private NeuralNetwork brain;

    private const int INPUT_COUNT = 28;
    private const int OUTPUT_COUNT = 8;

    [Header("Movement shaping")]

    [SerializeField] private bool limitMotorWindow = false;

    [SerializeField] private float motorWindowSeconds = 3f;

    private float aliveTime;

    [SerializeField] private float motorSpeedScale = 450f;

    [SerializeField] private float motorMaxTorque = 200f;

    [Tooltip("If true, outputs with low magnitude won't try to wiggle.")]
    [SerializeField] private bool deadZoneOutputs = true;

    [SerializeField] private float deadZone = 0.15f;

    public float fitness { get; private set; }
    public bool isDead { get; private set; }

    private Vector3 startPos;
    private Vector3 lastPos;
    private ContactFilter2D groundFilter;

    [Header("Reward milestones")]
    [SerializeField] private float milestoneStepX = 5f;

    [SerializeField] private float milestoneBonus = 100f;

    [SerializeField] private float forwardDeltaBonus = 5f;

    [SerializeField] private float backwardDeltaPenalty = 8f;

    [Header("Negative X penalties")]
    [SerializeField] private float headNegativeXPenalty = 15f;

    private float bestX;
    private int lastAwardedMilestoneIndex;

    private float[] lastJointAngles;
    private float[] lastJointAngleDeltas;
    private int[] footContactDurations;

    public void Init(NeuralNetwork network)
    {
        body = GetComponent<DogBody>();
        brain = network;
        startPos = transform.position;
        lastPos = transform.position;
        fitness = 0f;
        isDead = false;

        aliveTime = 0f;
        bestX = transform.position.x;
        lastAwardedMilestoneIndex = Mathf.FloorToInt(bestX / Mathf.Max(0.0001f, milestoneStepX));

        SetupSelfCollisionIgnore();
        ApplyUniqueColor();

        groundFilter = new ContactFilter2D();
        groundFilter.SetLayerMask(LayerMask.GetMask("Ground"));
        groundFilter.useLayerMask = true;

        HingeJoint2D[] joints = body.GetAllJoints();
        lastJointAngles = new float[joints.Length];
        lastJointAngleDeltas = new float[joints.Length];

        for (int i = 0; i < joints.Length; i++)
        {
            lastJointAngles[i] = joints[i].jointAngle;
            lastJointAngleDeltas[i] = 0f;
        }

        footContactDurations = new int[4];
    }

    void ApplyUniqueColor()
    {
        SpriteRenderer[] renderers = GetComponentsInChildren<SpriteRenderer>();
        if (renderers == null || renderers.Length == 0) return;

        uint seed = (uint)GetInstanceID();
        float h = (seed % 360u) / 360f;
        float s = 0.75f;
        float v = 1f;

        Color c = Color.HSVToRGB(h, s, v);
        c.a = 1f;

        for (int i = 0; i < renderers.Length; i++)
            renderers[i].color = c;
    }

    void SetupSelfCollisionIgnore()
    {
        if (body == null) return;

        int dogLayer = LayerMask.NameToLayer(dogPartLayerName);
        if (dogLayer < 0)
        {
            Debug.LogWarning($"[{nameof(DogAgent)}] Layer '{dogPartLayerName}' not found. Self-collision will not be ignored.");
            return;
        }

        Collider2D torsoCol = body.torso != null ? body.torso.GetComponent<Collider2D>() : null;
        Collider2D headCol = body.head != null ? body.head.GetComponent<Collider2D>() : null;

        Collider2D[] colliders = new Collider2D[]
        {
            torsoCol,
            headCol,
            body.frontLeftFoot,
            body.frontRightFoot,
            body.backLeftFoot,
            body.backRightFoot
        };

        for (int i = 0; i < colliders.Length; i++)
        {
            Collider2D c = colliders[i];
            if (c == null) continue;

            c.gameObject.layer = dogLayer;
        }

        Physics2D.IgnoreLayerCollision(dogLayer, dogLayer, true);
    }

    void FixedUpdate()
    {
        if (isDead) return;

        CheckDeath();
        if (isDead) return;

        HingeJoint2D[] jointsForDelta = body.GetAllJoints();
        for (int i = 0; i < 8; i++)
        {
            float current = jointsForDelta[i].jointAngle;
            float delta = (current - lastJointAngles[i]);

            lastJointAngleDeltas[i] = Mathf.Clamp(delta / 20f, -1f, 1f);
            lastJointAngles[i] = current;
        }

        aliveTime += Time.fixedDeltaTime;

        float[] inputs = GetInputs();
        float[] outputs = brain.Forward(inputs);

        bool motorsAllowed = true;
        if (limitMotorWindow)
            motorsAllowed = aliveTime <= motorWindowSeconds;

        if (motorsAllowed)
            ApplyOutputs(outputs);
        else
            ApplyOutputs(new float[OUTPUT_COUNT]);

        float deltaX = transform.position.x - lastPos.x;
        lastPos = transform.position;

        if (Mathf.Abs(body.torso.rotation) > 120f)
        {
            isDead = true;
            return;
        }

        float torsoAngleNorm = body.torso.rotation / 180f;
        float torsoAngVelNorm = body.torso.angularVelocity / 360f;

        float effort = 0f;

        footContactDurations[0] = IsTouching(body.frontLeftFoot) ? Mathf.Min(footContactDurations[0] + 1, 30) : 0;
        footContactDurations[1] = IsTouching(body.frontRightFoot) ? Mathf.Min(footContactDurations[1] + 1, 30) : 0;
        footContactDurations[2] = IsTouching(body.backLeftFoot) ? Mathf.Min(footContactDurations[2] + 1, 30) : 0;
        footContactDurations[3] = IsTouching(body.backRightFoot) ? Mathf.Min(footContactDurations[3] + 1, 30) : 0;

        for (int i = 0; i < outputs.Length; i++)
            effort += Mathf.Abs(outputs[i]);

        bestX = Mathf.Max(bestX, transform.position.x);

        int currentMilestoneIndex = Mathf.FloorToInt(bestX / Mathf.Max(0.0001f, milestoneStepX));
        if (currentMilestoneIndex > lastAwardedMilestoneIndex)
        {
            int crossed = currentMilestoneIndex - lastAwardedMilestoneIndex;
            fitness += crossed * milestoneBonus;
            lastAwardedMilestoneIndex = currentMilestoneIndex;
        }

        const float stabilityReward = 1.5f;
        const float spinPenalty = 1.0f;
        const float effortPenalty = 0.3f;

        float uprightFactor = 1f - Mathf.Clamp01(Mathf.Abs(torsoAngleNorm));

        float forwardBackwardShaping = 0f;
        if (deltaX >= 0f)
            forwardBackwardShaping += deltaX * forwardDeltaBonus;
        else
            forwardBackwardShaping += deltaX * backwardDeltaPenalty;

        float headNegativePenalty = 0f;
        if (body != null && body.head != null)
        {
            float headVx = body.head.velocity.x;
            if (headVx < 0f)
                headNegativePenalty = (-headVx) * headNegativeXPenalty;
        }

        float progressPenalty = 0f;
        if (deltaX < 0.001f && deltaX >= 0f)
            progressPenalty = 0.2f;

        float baselineForwardReward = deltaX * 2f;

        float stepFitness = 0f;
        stepFitness += baselineForwardReward;
        stepFitness += uprightFactor * stabilityReward;
        stepFitness -= Mathf.Abs(torsoAngVelNorm) * spinPenalty;
        stepFitness -= effort * effortPenalty;
        stepFitness -= progressPenalty;
        stepFitness += forwardBackwardShaping;
        stepFitness -= headNegativePenalty;

        fitness += stepFitness;
    }

    float[] GetInputs()
    {
        HingeJoint2D[] joints = body.GetAllJoints();
        float[] inputs = new float[INPUT_COUNT];

        inputs[0] = body.torso.rotation / 180f;
        inputs[1] = body.torso.angularVelocity / 360f;

        Vector2 v = body.torso.velocity;
        inputs[2] = Mathf.Clamp(v.x / 10f, -1f, 1f);
        inputs[3] = Mathf.Clamp(v.y / 10f, -1f, 1f);

        for (int i = 0; i < 8; i++)
            inputs[4 + i] = joints[i].jointAngle / 180f;

        for (int i = 0; i < 8; i++)
            inputs[12 + i] = lastJointAngleDeltas[i];

        inputs[20] = IsTouching(body.frontLeftFoot) ? 1f : 0f;
        inputs[21] = IsTouching(body.frontRightFoot) ? 1f : 0f;
        inputs[22] = IsTouching(body.backLeftFoot) ? 1f : 0f;
        inputs[23] = IsTouching(body.backRightFoot) ? 1f : 0f;

        inputs[24] = Mathf.Clamp(footContactDurations[0] / 15f, 0f, 1f);
        inputs[25] = Mathf.Clamp(footContactDurations[1] / 15f, 0f, 1f);
        inputs[26] = Mathf.Clamp(footContactDurations[2] / 15f, 0f, 1f);
        inputs[27] = Mathf.Clamp(footContactDurations[3] / 15f, 0f, 1f);

        return inputs;
    }

    bool IsTouching(Collider2D col)
    {
        Collider2D[] results = new Collider2D[1];
        return col.Overlap(groundFilter, results) > 0;
    }

    void ApplyOutputs(float[] outputs)
    {
        HingeJoint2D[] joints = body.GetAllJoints();

        for (int i = 0; i < joints.Length; i++)
        {
            JointMotor2D motor = joints[i].motor;

            float outVal = outputs[i];
            if (deadZoneOutputs && Mathf.Abs(outVal) < deadZone)
                outVal = 0f;

            motor.motorSpeed = outVal * motorSpeedScale;
            motor.maxMotorTorque = motorMaxTorque;
            joints[i].motor = motor;
            joints[i].useMotor = true;
        }
    }

    void CheckDeath()
    {
        if (IsTouching(body.torso.GetComponent<Collider2D>()) || IsTouching(body.head.GetComponent<Collider2D>()))
        {
            isDead = true;
            return;
        }

        if (transform.position.y < -10f)
            isDead = true;

        if (body != null && body.head != null)
        {
            if (body.head.position.x <= -1f)
                isDead = true;
        }

        if (isDead)
        {
            Destroy(gameObject);
        }
    }
}

