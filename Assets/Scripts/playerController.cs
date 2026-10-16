using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class playerController : MonoBehaviour
{
    public CharacterData player_data;

    public Rigidbody player_rigidbody;

    private Animator animator;

    public float boostTimer = 0.0f;
    public float leftTimer = 0.0f;
    public float rightTimer = 0.0f;
    public float slideTimer = 0.0f;

    public int leftCounter = 0;
    public int rightCounter = 0;

    public bool rotated = false;
    public bool forceStop = false;
    //public float originalXPos = 0f;
    public float originalRailPos = 0f;
    //public Vector3 lastPosition;

    public float previousRail;
    public float currentRail;

    public Vector3 previousComposeRail;
    public Vector3 currentComposeRail;
    public Vector3 originalComposeRailPosition;
    public int currentComposeRailIndex;

    public string currentAxis = "";
    //public float currentCenter;

    public string nextAxis = "";
    public float nextCenter;
    public Vector3 nextComposeCenter;
    public float rotationDegrees;

    public Vector3[] composeCenters = new Vector3[5];
    //public bool lookingLeft = false;
    //public bool lookingRight = false;

    private string[] axisRadial = { "z+", "x+z+", "x+", "x+z-", "z-", "x-z-", "x-", "x-z+"};
    private string[] axisRadialDegrees = { "x+", "x+z+", "z+", "x-z+", "x-", "x-z-", "z-", "x+z-"};

    public string currentInput = "";

    [SerializeField, Min(0.01f)] private float laneSpacing = 2f;
    [SerializeField, Min(0.001f)] private float turnTolerance = 0.1f;
    private Vector3[] nextComposeRails = new Vector3[5];
    private Vector3[] nextTurnPoints = new Vector3[5];
    private bool hasPendingTurn = false;
    private bool isTurning = false;
    private float previousTurnSignedDistance = 0f;
    private Vector2 previousTurnSamplePosition;
    private int previousTurnLaneIndex = -1;
    private bool hasPreviousTurnSample = false;
    private bool turnPointCrossed = false;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        animator = GetComponent<Animator>();

        //permitidos: z+, z-, x+, x-, x+z+,x+z-, x-z-, x+z+
        currentAxis = currentAxis == "" ? "z+" : currentAxis;
        hasPendingTurn = false;
        isTurning = false;
        ResetTurnPointTracking();

        if (currentAxis.Length == 4 && currentComposeRailIndex >= 0 && currentComposeRailIndex <= 4)
        {
            rightCounter = currentComposeRailIndex - 2;
        }
        else
        {
            rightCounter = Mathf.Clamp(rightCounter, -2, 2);
            currentComposeRailIndex = rightCounter + 2;
        }

        leftCounter = -rightCounter;
        currentRail = getCurrentRail();
        previousRail = currentRail;
    }

    void Update()
    {
        currentInput = GameMaster.Instance.currentInput;
        react();
    }

    void FixedUpdate()
    {
        if (hasPendingTurn && !isTurning)
        {
            cornerTurning();
        }

        if(currentAxis.Length == 2)
        {
            float curRail = getCurrentRail();
            if (curRail != currentRail && rotated)
            {
                char direction = currentAxis[0];
                char sign = currentAxis[1];

                if (direction == 'z')
                {
                    Vector3 newPositionZ = transform.position;
                    newPositionZ.x = currentRail;
                    transform.position = newPositionZ;
                }
                else
                {
                    Vector3 newPositionX = transform.position;
                    newPositionX.z = currentRail;
                    transform.position = newPositionX;
                }

                rotated = false;
            }
        }
        else
        {
            if (rotated)
            {
                Vector3 newPosition = getCurrentComposeRail();
                transform.position = newPosition;
                rotated = false;
            }
        }

        if (currentInput == "stop" || forceStop)
        {
            player_rigidbody.linearVelocity = transform.forward * 0;
        }
        else if (boostTimer > 0)
        {
            //player_rigidbody.linearVelocity = new Vector3(0f, player_rigidbody.linearVelocity.y, player_data.boostedSpeed);
            player_rigidbody.linearVelocity = transform.forward * player_data.boostedSpeed;
        }
        else if (leftTimer > 0)
        {
            player_rigidbody.linearVelocity = transform.right * -1 * player_data.boostedSpeed;
        }
        else if (rightTimer > 0)
        {
            player_rigidbody.linearVelocity = transform.right * player_data.boostedSpeed;
        }
        else if (slideTimer > 0)
        {
            player_rigidbody.linearVelocity = transform.right * player_data.boostedSpeed;
        }
        else
        {
            //player_rigidbody.linearVelocity = new Vector3(0f, player_rigidbody.linearVelocity.y, player_data.regularSpeed);

            if (previousRail != currentRail && currentAxis.Length == 2)
            {
                char direction = currentAxis[0];
                char sign = currentAxis[1];

                if (direction == 'z')
                {
                    Vector3 posZ = transform.position;
                    posZ.x = currentRail;
                    transform.position = posZ;
                    previousRail = currentRail;
                }
                else
                {
                    Vector3 posX = transform.position;
                    posX.z = currentRail;
                    transform.position = posX;
                    previousRail = currentRail;
                }
            }

            if(previousComposeRail != currentComposeRail && currentAxis.Length == 4)
            {
                Vector3 pos = transform.position;
                pos.x = currentComposeRail.x;
                pos.z = currentComposeRail.z;
                transform.position = pos;
                previousComposeRail = currentComposeRail;
            }
            player_rigidbody.linearVelocity = transform.forward * player_data.regularSpeed;
        }
    }

    private void react()
    {
        if (isTurning)
        {
            return;
        }

        if ((currentInput == "run" || boostTimer > 0) && leftTimer == 0 && rightTimer == 0 && slideTimer == 0)
        {
            if (boostTimer == 0)
            {
                boostTimer = player_data.boostDuration;
                animator.SetBool("running", true);
                //player_rigidbody.linearVelocity = avanzar("boosted");
            }
            else
            {
                boostTimer -= Time.deltaTime; // Restar tiempo del temporizador

                if (boostTimer <= 0)
                {
                    //player_rigidbody.linearVelocity = avanzar();
                    boostTimer = 0.0f;
                    animator.SetBool("running", false);
                }
            }
        }
        else if ((currentInput == "left" || leftTimer > 0) && rightTimer == 0 && slideTimer == 0)
        {
            if (leftTimer > 0)
            {
                leftTimer -= Time.deltaTime;
                if (leftTimer <= 0)
                {
                    //player_rigidbody.linearVelocity = avanzar();
                    leftTimer = 0;
                    animator.SetBool("left", false);
                }
            }
            else if (currentInput == "left")
            {
                int targetLaneIndex = rightCounter - 1;
                if (targetLaneIndex >= -2 && IsLaneTurnPointAhead(targetLaneIndex))
                {
                    calcMoveTo("left");
                    rightCounter = targetLaneIndex;
                    leftCounter = -rightCounter;
                    currentComposeRailIndex = rightCounter + 2;
                    leftTimer = player_data.movementDuration;
                    animator.SetBool("left", true);
                }
            }
        }
        else if ((currentInput == "right" || rightTimer > 0) && leftTimer == 0 && slideTimer == 0)
        {
            if (rightTimer > 0)
            {
                rightTimer -= Time.deltaTime;
                if (rightTimer <= 0)
                {
                    //player_rigidbody.linearVelocity = avanzar();
                    rightTimer = 0;
                    animator.SetBool("right", false);
                }
            }
            else if (currentInput == "right")
            {
                int targetLaneIndex = rightCounter + 1;
                if (targetLaneIndex <= 2 && IsLaneTurnPointAhead(targetLaneIndex))
                {
                    calcMoveTo("right");
                    rightCounter = targetLaneIndex;
                    leftCounter = -rightCounter;
                    currentComposeRailIndex = rightCounter + 2;
                    rightTimer = player_data.movementDuration;
                    animator.SetBool("right", true);
                }
            }
        }
        else if ((currentInput == "slide" || slideTimer > 0) && rightTimer == 0 && leftTimer == 0)
        {
            if (slideTimer == 0)
            {
                rotated = true;

                slideTimer = player_data.slideDuration;
                Quaternion targetRotation = Quaternion.Euler(transform.rotation.eulerAngles.x, transform.rotation.eulerAngles.y - 90f, transform.rotation.eulerAngles.z);
                player_rigidbody.MoveRotation(targetRotation);
                animator.SetBool("sliding", true);

            }
            else
            {
                slideTimer -= Time.deltaTime;
                if (slideTimer <= 0)
                {
                    Quaternion targetRotation = Quaternion.Euler(transform.rotation.eulerAngles.x, transform.rotation.eulerAngles.y + 90f, transform.rotation.eulerAngles.z);
                    player_rigidbody.MoveRotation(targetRotation);

                    rotated = true;
                    //rStart = false;
                    slideTimer = 0;
                    animator.SetBool("sliding", false);
                }

            }
        }
        else if (currentInput == "stop" && rightTimer == 0 && leftTimer == 0 && slideTimer == 0 && boostTimer == 0)
        {
            //player_rigidbody.linearVelocity = transform.forward * 0;
            animator.SetBool("stopped", true);


        }
        else
        {
            animator.SetBool("stopped", false);
        }
    }

    //private Vector3 walk(string velocity = "regular")
    //{
    //    float speed = player_data.regularSpeed;
    //    if (velocity == "boosted") speed = player_data.boostedSpeed;

    //    if(lookingDerecha)


    //}

    private void ResetTurnPointTracking()
    {
        previousTurnSignedDistance = 0f;
        previousTurnSamplePosition = Vector2.zero;
        previousTurnLaneIndex = -1;
        hasPreviousTurnSample = false;
        turnPointCrossed = false;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("turnCollider"))
        {
            if (isTurning || hasPendingTurn)
            {
                return;
            }

            turnController tc = other.gameObject.GetComponent<turnController>();
            if (tc != null && tc.axis != null)
            {
                var options = tc.axis;
                if (options.Length == 1)
                {
                    string candidateAxis = options[0];
                    if (Array.IndexOf(axisRadialDegrees, candidateAxis) == -1)
                    {
                        hasPendingTurn = false;
                        nextAxis = "";
                        ResetTurnPointTracking();
                        Debug.LogWarning("OnTriggerEnter: Invalid axis option: " + candidateAxis);
                        return;
                    }

                    nextAxis = candidateAxis;
                    nextComposeCenter = new Vector3(other.transform.position.x, 0f, other.transform.position.z);

                    bool railsOk = calcComposeRails();
                    bool turnsOk = railsOk && CalculateNextTurnPoints();

                    if (railsOk && turnsOk)
                    {
                        ResetTurnPointTracking();
                        hasPendingTurn = true;
                    }
                    else
                    {
                        hasPendingTurn = false;
                        nextAxis = "";
                        ResetTurnPointTracking();
                        Debug.LogWarning("OnTriggerEnter: Failed to calculate compose rails or turn points.");
                    }
                }
            }
        }
    }

    private float getNewCenter(string axis, Vector3 newSidewalk)
    {
        char direction = axis[0];
        char sign = axis[1];
        if (direction == 'z')
        {
            return newSidewalk.x;
        }
        else 
        //if (direction == 'x')
        {
            return newSidewalk.z;
        }
    }

    private Vector2 GetAxisDirection(string axis)
    {
        float angleRadians = axisToDegree(axis) * Mathf.Deg2Rad;
        Vector2 direction = new Vector2(Mathf.Cos(angleRadians), Mathf.Sin(angleRadians));
        return direction.normalized;
    }

    private Vector2[] GetCurrentRailAnchors()
    {
        if (currentAxis.Length == 4)
        {
            Vector2[] anchors = new Vector2[5];
            for (int i = 0; i < 5; i++)
            {
                anchors[i] = new Vector2(composeCenters[i].x, composeCenters[i].z);
                if (float.IsNaN(anchors[i].x) || float.IsNaN(anchors[i].y) || float.IsInfinity(anchors[i].x) || float.IsInfinity(anchors[i].y))
                {
                    Debug.LogWarning("GetCurrentRailAnchors: Compose anchor contains NaN or Infinity.");
                    return null;
                }
            }
            return anchors;
        }
        else if (currentAxis.Length == 2)
        {
            Vector2 direction = GetAxisDirection(currentAxis);
            Vector2 right = new Vector2(direction.y, -direction.x);
            Vector2 currentLanePoint = currentAxis[0] == 'z'
                ? new Vector2(currentRail, transform.position.z)
                : new Vector2(transform.position.x, currentRail);

            Vector2 centralAnchor = currentLanePoint - right * (rightCounter * laneSpacing);

            Vector2[] anchors = new Vector2[5];
            for (int i = 0; i < 5; i++)
            {
                anchors[i] = centralAnchor + right * ((i - 2) * laneSpacing);
                if (float.IsNaN(anchors[i].x) || float.IsNaN(anchors[i].y) || float.IsInfinity(anchors[i].x) || float.IsInfinity(anchors[i].y))
                {
                    Debug.LogWarning("GetCurrentRailAnchors: Orthogonal anchor contains NaN or Infinity.");
                    return null;
                }
            }
            return anchors;
        }
        else
        {
            Debug.LogWarning("GetCurrentRailAnchors: Invalid currentAxis: " + currentAxis);
            return null;
        }
    }

    private bool TryGetLineIntersection(Vector2 pointA, Vector2 directionA, Vector2 pointB, Vector2 directionB, out Vector2 intersection)
    {
        float det = directionA.x * directionB.y - directionA.y * directionB.x;
        if (Mathf.Abs(det) <= 0.0001f)
        {
            intersection = Vector2.zero;
            return false;
        }

        Vector2 diff = pointB - pointA;
        float t = (diff.x * directionB.y - diff.y * directionB.x) / det;
        intersection = pointA + directionA * t;

        if (float.IsNaN(intersection.x) || float.IsNaN(intersection.y) || float.IsInfinity(intersection.x) || float.IsInfinity(intersection.y))
        {
            intersection = Vector2.zero;
            return false;
        }

        return true;
    }

    private bool CalculateNextTurnPoints()
    {
        Vector2[] currentAnchors = GetCurrentRailAnchors();
        if (currentAnchors == null || currentAnchors.Length != 5)
        {
            return false;
        }

        Vector2 currentDirection = GetAxisDirection(currentAxis);
        Vector2 nextDirection = GetAxisDirection(nextAxis);
        Vector3[] calculatedPoints = new Vector3[5];

        for (int i = 0; i < 5; i++)
        {
            Vector2 nextRailPoint = new Vector2(nextComposeRails[i].x, nextComposeRails[i].z);
            Vector2 intersection;
            if (!TryGetLineIntersection(currentAnchors[i], currentDirection, nextRailPoint, nextDirection, out intersection))
            {
                return false;
            }
            calculatedPoints[i] = new Vector3(intersection.x, 0f, intersection.y);
        }

        for (int i = 0; i < 5; i++)
        {
            nextTurnPoints[i] = calculatedPoints[i];
        }

        return true;
    }

    private bool IsLaneTurnPointAhead(int targetLaneIndex)
    {
        if (!hasPendingTurn)
        {
            return true;
        }

        if (targetLaneIndex < -2 || targetLaneIndex > 2)
        {
            return false;
        }

        Vector3 target = nextTurnPoints[targetLaneIndex + 2];
        Vector2 targetXZ = new Vector2(target.x, target.z);
        Vector2 playerXZ = new Vector2(transform.position.x, transform.position.z);
        float signedDistance = Vector2.Dot(targetXZ - playerXZ, GetAxisDirection(currentAxis));

        return signedDistance >= -turnTolerance;
    }

    private float axisToDegree(string axis)
    {
        float degree = 0f;
        foreach (var a in axisRadialDegrees)
        {
            if (a == axis)
            {
                break;
            }
            degree += 45f;
        }
        return degree;
    }

    private float getRotationDegrees()
    {
        var first = "";
        var second = "";
        var steps = -1;

        foreach (var axis in axisRadial)
        {
            if (steps >= 0 && second == "")
            {
                steps++;
            }
            if (axis == currentAxis || axis == nextAxis)
            {
                if (first == "")
                {
                    first = axis;
                    steps = 0;
                }
                else
                {
                    second = axis;
                }
            }
        }

        var oppositeSteps = 8 - steps;

        if (steps <= oppositeSteps)
        {
            if(first == nextAxis)
            {
                return (steps * 45f) * -1;
            }
            else
            {
                return (steps * 45f);
            }
        }
        else
        {
            if (first == nextAxis)
            {
                return (oppositeSteps * 45f);
            }
            else
            {
                return (oppositeSteps * 45f) * -1;
            }
        }
    }

    private float getCurrentRail()
    {
        char direction = currentAxis[0];
        char sign = currentAxis[1];
        if (direction == 'z')
        {
            return transform.position.x;
        }
        else
        {
            return transform.position.z;
        }
    }

    private Vector3 getCurrentComposeRail()
    {
        Vector2 currentPoint = new Vector2(transform.position.x, transform.position.z);
        Vector2 point = new Vector2(composeCenters[currentComposeRailIndex].x, composeCenters[currentComposeRailIndex].z);
        float angle = axisToDegree(currentAxis);
       
        float pendiente = Mathf.Tan(angle * Mathf.Deg2Rad);

        Vector2 d = new Vector2(1f, pendiente).normalized;
        float t = Vector2.Dot(currentPoint - point, d) / Vector2.Dot(d, d);
        Vector2 closestPoint = point + t * d;

        return new Vector3(closestPoint.x, transform.position.y, closestPoint.y);
    }

    private bool calcComposeRails()
    {
        Vector2 centerXZ = new Vector2(nextComposeCenter.x, nextComposeCenter.z);
        Vector2 direction = GetAxisDirection(nextAxis);
        Vector2 right = new Vector2(direction.y, -direction.x);

        for (int i = 0; i < 5; i++)
        {
            float laneOffset = (i - 2) * laneSpacing;
            Vector2 railPoint = centerXZ + right * laneOffset;
            if (float.IsNaN(railPoint.x) || float.IsNaN(railPoint.y) || float.IsInfinity(railPoint.x) || float.IsInfinity(railPoint.y))
            {
                return false;
            }
            nextComposeRails[i] = new Vector3(railPoint.x, 0f, railPoint.y);
        }

        return true;
    }

    private void calcMoveTo(string move)
    {
        if(currentAxis.Length == 2)
        {
            if (((currentAxis == "z+" || currentAxis == "x-") && move == "left") || ((currentAxis == "z-" || currentAxis == "x+") && move == "right"))
            {
                previousRail = currentRail;
                currentRail = currentRail - laneSpacing;
            }
            else
            {
                previousRail = currentRail;
                currentRail = currentRail + laneSpacing;
            }
        }
        else
        {
            float originalX = composeCenters[currentComposeRailIndex].x;  // Punto original recta
            float originalZ = composeCenters[currentComposeRailIndex].z;
            Vector2 linePoint = new Vector2(originalX, originalZ);

            float x = transform.position.x;  // Posición actual objeto (avance + posible desviación)
            float z = transform.position.z;
            Vector2 worldPoint = new Vector2(x, z);

            float angle = axisToDegree(currentAxis);
            float angleRad = Mathf.Deg2Rad * angle;

            // 1. Proyecta posición actual sobre recta original → captura progreso sin desviación
            Vector2 projectedPoint = ProjectPointOnLine(linePoint, angleRad, worldPoint);

            // 2. Offset perpendicular desde el punto proyectado (mantiene avance)
            float perpendicularAngleRad = angleRad + (move == "left" ? Mathf.PI / 2 : -Mathf.PI / 2);
            Vector2 perpendicularDirection = new Vector2(Mathf.Cos(perpendicularAngleRad), Mathf.Sin(perpendicularAngleRad)).normalized;

            Vector2 nextRail = projectedPoint + perpendicularDirection * laneSpacing;

            previousComposeRail = currentComposeRail;
            currentComposeRail = new Vector3(nextRail.x, 0f, nextRail.y);

        }
    }

    Vector2 ProjectPointOnLine(Vector2 linePoint, float angleRad, Vector2 worldPoint)
    {
        Vector2 lineDir = new Vector2(Mathf.Cos(angleRad), Mathf.Sin(angleRad)).normalized;
        Vector2 toWorld = worldPoint - linePoint;
        float t = Vector2.Dot(toWorld, lineDir);
        return linePoint + lineDir * t;
    }

    private void cornerTurning()
    {
        if (!hasPendingTurn || isTurning)
        {
            return;
        }

        int laneIndex = rightCounter + 2;
        if (laneIndex < 0 || laneIndex > 4)
        {
            Debug.LogWarning("cornerTurning: laneIndex out of range: " + laneIndex);
            hasPendingTurn = false;
            nextAxis = "";
            ResetTurnPointTracking();
            return;
        }

        Vector3 target = nextTurnPoints[laneIndex];
        Vector2 targetXZ = new Vector2(target.x, target.z);
        Vector2 playerXZ = new Vector2(transform.position.x, transform.position.z);
        float signedDistance = Vector2.Dot(targetXZ - playerXZ, GetAxisDirection(currentAxis));

        bool sameLaneSample = hasPreviousTurnSample && previousTurnLaneIndex == laneIndex;

        if (!sameLaneSample)
        {
            ResetTurnPointTracking();
            previousTurnLaneIndex = laneIndex;
            previousTurnSignedDistance = signedDistance;
            previousTurnSamplePosition = playerXZ;
            hasPreviousTurnSample = true;

            if (signedDistance < -turnTolerance)
            {
                Debug.LogWarning("cornerTurning: Target intersection was already behind player at initial sampling (signedDistance=" + signedDistance + "). Cancelling pending turn.");
                hasPendingTurn = false;
                nextAxis = "";
                ResetTurnPointTracking();
                return;
            }

            if (signedDistance > turnTolerance)
            {
                return;
            }
        }
        else
        {
            float actual_step = Vector2.Distance(playerXZ, previousTurnSamplePosition);
            float configured_step = Mathf.Max(player_data.regularSpeed, player_data.boostedSpeed) * Time.fixedDeltaTime;
            float allowed_correction = Mathf.Min(actual_step + turnTolerance, configured_step + turnTolerance);
            float overshoot = Mathf.Max(0f, -signedDistance);

            bool condition_a = previousTurnSignedDistance > 0f && signedDistance <= 0f;
            bool condition_b = previousTurnSignedDistance >= -turnTolerance && previousTurnSignedDistance > signedDistance && signedDistance < -turnTolerance;

            if (condition_a || condition_b)
            {
                if (overshoot <= allowed_correction)
                {
                    turnPointCrossed = true;
                }
                else
                {
                    Debug.LogWarning("cornerTurning: Crossing overshoot exceeded permitted correction distance (overshoot=" + overshoot + ", allowed=" + allowed_correction + "). Cancelling pending turn.");
                    hasPendingTurn = false;
                    nextAxis = "";
                    ResetTurnPointTracking();
                    return;
                }
            }
            else if (!turnPointCrossed && signedDistance < -turnTolerance)
            {
                Debug.LogWarning("cornerTurning: Passed intersection without safe crossing (signedDistance=" + signedDistance + "). Cancelling pending turn.");
                hasPendingTurn = false;
                nextAxis = "";
                ResetTurnPointTracking();
                return;
            }
        }

        bool turnPointReached = Mathf.Abs(signedDistance) <= turnTolerance || turnPointCrossed;

        if (!turnPointReached)
        {
            if (signedDistance > turnTolerance)
            {
                previousTurnSignedDistance = signedDistance;
                previousTurnSamplePosition = playerXZ;
                previousTurnLaneIndex = laneIndex;
                return;
            }

            Debug.LogWarning("cornerTurning: Turn point not reached and signedDistance < -turnTolerance. Cancelling pending turn.");
            hasPendingTurn = false;
            nextAxis = "";
            ResetTurnPointTracking();
            return;
        }

        if (leftTimer > 0f || rightTimer > 0f || slideTimer > 0f)
        {
            previousTurnSignedDistance = signedDistance;
            previousTurnSamplePosition = playerXZ;
            previousTurnLaneIndex = laneIndex;
            return;
        }

        rotationDegrees = getRotationDegrees();
        transform.position = new Vector3(target.x, transform.position.y, target.z);
        isTurning = true;
        forceStop = true;
        GameMaster.Instance.activeInput = false;
        currentAxis = nextAxis;

        for (int i = 0; i < 5; i++)
        {
            composeCenters[i] = nextComposeRails[i];
        }

        leftCounter = -rightCounter;
        currentComposeRailIndex = rightCounter + 2;

        currentRail = getCurrentRail();
        previousRail = currentRail;
        currentComposeRail = getCurrentComposeRail();
        previousComposeRail = currentComposeRail;

        GameMaster.Instance.cameraRotation = rotationDegrees;
        hasPendingTurn = false;
        StartCoroutine(TurnRotation());
    }

    IEnumerator TurnRotation()
    {
        float startAngle = transform.rotation.eulerAngles.y;
        float elapsed = 0f;
        float duration = 0.3f;

        while (elapsed < duration)
        {
            float newY = Mathf.LerpAngle(startAngle, startAngle + rotationDegrees, elapsed / duration);
            Quaternion targetRotation = Quaternion.Euler(transform.rotation.eulerAngles.x, newY, transform.rotation.eulerAngles.z);
            player_rigidbody.MoveRotation(targetRotation);
            elapsed += Time.deltaTime;
            yield return null;
        }
        Quaternion finalRotation = Quaternion.Euler(transform.rotation.eulerAngles.x, startAngle + rotationDegrees, transform.rotation.eulerAngles.z);
        transform.rotation = finalRotation;

        forceStop = false;
        isTurning = false;
        GameMaster.Instance.activeInput = true;

        nextAxis = "";
        nextCenter = 0f;
        nextComposeCenter = Vector3.zero;
        hasPendingTurn = false;
        for (int i = 0; i < 5; i++)
        {
            nextTurnPoints[i] = Vector3.zero;
            nextComposeRails[i] = Vector3.zero;
        }
        ResetTurnPointTracking();
    }


}
