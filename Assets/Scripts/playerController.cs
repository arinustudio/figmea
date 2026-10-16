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

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        animator = GetComponent<Animator>();

        //permitidos: z+, z-, x+, x-, x+z+,x+z-, x-z-, x+z+
        currentAxis = currentAxis == "" ? "z+" : currentAxis;
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
        if (nextAxis != "")
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
            if (leftTimer == 0 && leftCounter < 2)
            {
                leftTimer = player_data.movementDuration;
                //player_rigidbody.linearVelocity = transform.right * -1 * boostedSpeed; //pendiente de adaptar
                leftCounter++;
                rightCounter--;
                calcMoveTo("left");
                animator.SetBool("left", true);
                currentComposeRailIndex--;
            }
            else
            {
                leftTimer -= Time.deltaTime;
                if (leftTimer <= 0)
                {
                    //player_rigidbody.linearVelocity = avanzar();
                    leftTimer = 0;
                    animator.SetBool("left", false);
                }

            }
        }
        else if ((currentInput == "right" || rightTimer > 0) && leftTimer == 0 && slideTimer == 0)
        {
            if (rightTimer == 0 && rightCounter < 2)
            {
                rightTimer = player_data.movementDuration; ;
                //player_rigidbody.linearVelocity = transform.right * boostedSpeed; //pendiente de adaptar
                rightCounter++;
                leftCounter--;
                calcMoveTo("right");
                animator.SetBool("right", true);
                currentComposeRailIndex++;
            }
            else
            {
                rightTimer -= Time.deltaTime;
                if (rightTimer <= 0)
                {
                    //player_rigidbody.linearVelocity = avanzar();
                    rightTimer = 0;
                    animator.SetBool("right", false);
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

    private void OnTriggerEnter(Collider other)
    {
        if(other.gameObject.CompareTag("turnCollider"))
        {
            GameMaster.Instance.activeInput = false;

            turnController tc = other.gameObject.GetComponent<turnController>();

            if (tc != null)
            {
                var options = tc.axis
                    ;

                if(options.Length == 1)
                {
                    if (options[0].Length == 4) // Diagonal
                    {
                        nextAxis = options[0];
                        nextComposeCenter = getNewComposeCenter(nextAxis, other.gameObject.transform);
                        calcComposeRails();
                    }
                    else
                    {
                        nextAxis = options[0];
                        nextCenter = getNewCenter(nextAxis, other.gameObject.transform.position);
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

    public Vector3 getNewComposeCenter(string axis, Transform newSidewalk)
    {
        // Punto sobre la recta actual (posición del jugador)
        Vector2 A = new Vector2(transform.position.x, transform.position.z);
        // Punto sobre la siguiente recta (centro del collider)
        Vector2 B = new Vector2(newSidewalk.position.x, newSidewalk.position.z);

        // Ángulos según tu convención (z+ adelante, x+ derecha)
        float angleA = axisToDegree(currentAxis) * Mathf.Deg2Rad;
        float angleB = axisToDegree(axis) * Mathf.Deg2Rad;

        // Direcciones unitarias de cada recta
        Vector2 dA = new Vector2(Mathf.Cos(angleA), Mathf.Sin(angleA)); // rail actual
        Vector2 dB = new Vector2(Mathf.Cos(angleB), Mathf.Sin(angleB)); // rail siguiente

        // Determinante (sabemos que no será 0 porque nunca son paralelas)
        float det = dA.x * dB.y - dA.y * dB.x;

        Vector2 diff = B - A;
        float t = (diff.x * dB.y - diff.y * dB.x) / det;

        Vector2 P = A + dA * t; // Punto de intersección

        return new Vector3(P.x, 0f, P.y);
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

    private Vector3 calcMoveToFrom(string move, Vector3 position)
    {
        float x = position.x;
        float z = position.z;
        Vector2 linePoint = new Vector2(x, z);
        float angle = axisToDegree(nextAxis);

        float angleRad = Mathf.Deg2Rad * angle;

        float perpendicularAngleRad = angleRad + (move == "left" ? Mathf.PI / 2 : -Mathf.PI / 2);

        Vector2 perpendicularDirection = new Vector2(Mathf.Cos(perpendicularAngleRad), Mathf.Sin(perpendicularAngleRad));

        Vector2 nextRail = linePoint + perpendicularDirection * 2f;

        return new Vector3(nextRail.x, 0f, nextRail.y);
    }

    private void calcComposeRails()
    {
        Vector3 center = new Vector3(nextComposeCenter.x, 0f, nextComposeCenter.z);
        Vector3 rightA = calcMoveToFrom("right", center);
        Vector3 rightB = calcMoveToFrom("right", rightA);
        Vector3 leftA = calcMoveToFrom("left", center);
        Vector3 leftB = calcMoveToFrom("left", leftA);

        composeCenters[0] = leftB;
        composeCenters[1] = leftA;
        composeCenters[2] = center;
        composeCenters[3] = rightA;
        composeCenters[4] = rightB;

    }

    private void calcMoveTo(string move)
    {
        if(currentAxis.Length == 2)
        {
            if (((currentAxis == "z+" || currentAxis == "x-") && move == "left") || ((currentAxis == "z-" || currentAxis == "x+") && move == "right"))
            {
                previousRail = currentRail;
                currentRail = currentRail - 2f;
            }
            else
            {
                previousRail = currentRail;
                currentRail = currentRail + 2f;
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

            Vector2 nextRail = projectedPoint + perpendicularDirection * 2f;

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
        if(nextAxis.Length == 4) // Diagonal
        {
            Vector3 currentDirectionPosition = new Vector3(transform.position.x, 0f, transform.position.z);
            Vector3 composeNextCenter = new Vector3(nextComposeCenter.x, 0f, nextComposeCenter.z);
            const float umbral = 0.1f;
            float distancia = Vector3.Distance(currentDirectionPosition, composeNextCenter);

            if (distancia < umbral)
            {
                rotationDegrees = getRotationDegrees();
                //Quaternion targetRotation = Quaternion.Euler(transform.rotation.eulerAngles.x, transform.rotation.eulerAngles.y + rotationDegrees, transform.rotation.eulerAngles.z);
                //player_rigidbody.MoveRotation(targetRotation);
                forceStop = true;
                currentAxis = nextAxis;
                StartCoroutine(TurnRotation());
                currentRail = 0;
                previousRail = 0;
                currentComposeRail = nextComposeCenter;
                previousComposeRail = currentComposeRail;
                currentComposeRailIndex = 2;
                leftCounter = 0;
                rightCounter = 0;
                GameMaster.Instance.cameraRotation = rotationDegrees;
                nextAxis = "";
            }
        }
        else
        {
            var direction = nextAxis[0];
            var currentDirectionPosition = 0f;
            if (direction == 'x')
            {
                currentDirectionPosition = transform.position.z;
            }
            else
            //else if(direction == 'z')
            {
                currentDirectionPosition = transform.position.x;
            }
            const float umbral = 0.1f;

            if (Mathf.Abs(currentDirectionPosition - nextCenter) < umbral)
            {
                Vector3 exactPosition;
                if (direction == 'x')
                {
                    exactPosition = new Vector3(transform.position.x, transform.position.y, nextCenter);
                }
                else
                //else if(direction == 'z')
                {
                    exactPosition = new Vector3(nextCenter, transform.position.y, transform.position.z);
                }
                transform.position = exactPosition;
                rotationDegrees = getRotationDegrees();
                //Quaternion targetRotation = Quaternion.Euler(transform.rotation.eulerAngles.x, transform.rotation.eulerAngles.y + rotationDegrees, transform.rotation.eulerAngles.z);
                //player_rigidbody.MoveRotation(targetRotation);
                forceStop = true;
                currentAxis = nextAxis;
                StartCoroutine(TurnRotation());
                currentRail = nextCenter;
                previousRail = currentRail;
                leftCounter = 0;
                rightCounter = 0;
                GameMaster.Instance.cameraRotation = rotationDegrees;
                previousComposeRail = Vector3.zero;
                currentComposeRail = Vector3.zero;
                nextAxis = "";
            }
        }
            
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

        if (currentAxis.Length == 2) 
        {
            transform.position = new Vector3(
                currentAxis[0] == 'z' ? nextCenter : transform.position.x,
                transform.position.y,
                currentAxis[0] == 'x' ? nextCenter : transform.position.z
            ); 
        }
        else
        {
            transform.position = new Vector3(nextComposeCenter.x, transform.position.y, nextComposeCenter.z);
        }
        forceStop = false;
        nextCenter = 0f;
        nextComposeCenter = Vector3.zero;
        GameMaster.Instance.activeInput = true;
    }


}
