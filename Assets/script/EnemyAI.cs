using UnityEngine;
using UnityEngine.AI;
using System.Threading.Tasks;


public class EnemyAI : MonoBehaviour
{
    [Header("Detection Settings")]
    public float detectionRange = 10f;
    public float fieldOfViewAngle = 60f;
    public LayerMask playerLayer = 1;
    public LayerMask obstacleLayer = 1;

    [Header("Chase Settings")]
    public float chaseSpeed = 5f;
    public float catchDistance = 2f;
    public float loseTargetTime = 5f;

    [Header("Patrol Settings")]
    public Transform[] patrolPoints;
    public float patrolSpeed = 2f;
    public float waitTime = 2f;

    [Header("Animation")]
    public Animator animator;

    [Header("Debug")]
    public bool showDebugLogs = true;

    [Header("Player")]
    public PlayerMovement _PlayerMovement;

        [Header("Sounds")]
    [SerializeField] private AudioClip walkingSound;
    [SerializeField] private AudioClip chasingSound; // Renamed for consistency
    public AudioSource audioSource;

    private NavMeshAgent agent;
    private Transform player;
    private EnemyState currentState = EnemyState.Patrolling;
    private int currentPatrolIndex = 0;
    private float lastSeenTimer = 0f;
    private float waitTimer = 0f;
    private Vector3 lastKnownPlayerPosition;
    private bool hasValidPatrolPoints = false;

    public enum EnemyState
    {
        Patrolling,
        Chasing,
        Searching,
        Caught
    }

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        // Zoek naar player met tag
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            player = playerObj.transform;
        }

        if (agent == null)
        {
            Debug.LogError("NavMeshAgent component missing on " + gameObject.name);
            return;
        }

        if (player == null)
        {
            Debug.LogError("Player not found! Make sure player has 'Player' tag");
            return;
        }

        if (audioSource == null)
        {
            Debug.LogWarning("AudioSource component missing on " + gameObject.name);
        }

        // Valideer patrol points
        ValidatePatrolPoints();

        // Set initial patrol state
        if (hasValidPatrolPoints)
        {
            agent.speed = patrolSpeed;
            SetPatrolDestination();
            PlayWalkingSound(); // Start with walking sound
            //if (showDebugLogs) Debug.Log($"{gameObject.name}: Starting patrol");
        }
        else
        {
            Debug.LogWarning($"{gameObject.name}: No valid patrol points found, enemy will stay idle");
        }
    }

    void ValidatePatrolPoints()
    {
        hasValidPatrolPoints = false;
        if (patrolPoints != null && patrolPoints.Length > 0)
        {
            for (int i = 0; i < patrolPoints.Length; i++)
            {
                if (patrolPoints[i] != null)
                {
                    hasValidPatrolPoints = true;
                    break;
                }
            }
        }
    }

    void Update()
    {
        if (player == null || agent == null) return;

        // Check player detection first
        bool canSeePlayer = CanSeePlayer();

        // Handle state transitions
        HandleStateTransitions(canSeePlayer);

        // Handle current state behavior
        switch (currentState)
        {
            case EnemyState.Patrolling:
                HandlePatrolling();
                break;
            case EnemyState.Chasing:
                HandleChasing();
                break;
            case EnemyState.Searching:
                HandleSearching();
                break;
            case EnemyState.Caught:
                HandleCaught();
                break;
        }

        // Update animations
        UpdateAnimations();
    }

    void HandleStateTransitions(bool canSeePlayer)
    {
        switch (currentState)
        {
            case EnemyState.Patrolling:
                if (canSeePlayer)
                {
                    StartChasing();
                }
                break;

            case EnemyState.Chasing:
                if (canSeePlayer)
                {
                    // Reset timer when we can still see player
                    lastSeenTimer = 0f;
                    lastKnownPlayerPosition = player.position;
                }
                else
                {
                    // Increment timer when we can't see player
                    lastSeenTimer += Time.deltaTime;
                    if (lastSeenTimer >= loseTargetTime)
                    {
                        StartSearching();
                    }
                }
                break;

            case EnemyState.Searching:
                if (canSeePlayer)
                {
                    StartChasing();
                }
                break;
        }
    }

    bool CanSeePlayer()
    {
        if (player == null) return false;

        Vector3 directionToPlayer = (player.position - transform.position).normalized;
        float distanceToPlayer = Vector3.Distance(transform.position, player.position);

        // Check if player is within detection range
        if (distanceToPlayer > detectionRange) return false;

        // Check field of view
        float angleToPlayer = Vector3.Angle(transform.forward, directionToPlayer);
        if (angleToPlayer > fieldOfViewAngle / 2) return false;

        // Raycast to check for obstacles (alleen checken als er ook een playerLayer is ingesteld)
        if (playerLayer.value > 0)
        {
            Vector3 rayStart = transform.position + Vector3.up * 1.5f;
            Vector3 rayEnd = player.position + Vector3.up * 1.5f;
            Vector3 rayDirection = (rayEnd - rayStart).normalized;
            float rayDistance = Vector3.Distance(rayStart, rayEnd);

            // Check if we hit an obstacle before hitting the player
            RaycastHit hit;
            if (Physics.Raycast(rayStart, rayDirection, out hit, rayDistance, obstacleLayer))
            {
                // Check if we hit the player or an obstacle
                if (!hit.collider.CompareTag("Player"))
                {
                    return false; // Hit an obstacle
                }
            }
        }

        return true;
    }

    void HandlePatrolling()
    {
        if (!hasValidPatrolPoints) return;

        // Check if we've reached the destination
        if (!agent.pathPending && agent.remainingDistance < 0.5f)
        {
            waitTimer += Time.deltaTime;
            if (waitTimer >= waitTime)
            {
                waitTimer = 0f;
                MoveToNextPatrolPoint();
            }
        }
    }

    void MoveToNextPatrolPoint()
    {
        currentPatrolIndex = (currentPatrolIndex + 1) % patrolPoints.Length;

        // Find next valid patrol point
        int attempts = 0;
        while (patrolPoints[currentPatrolIndex] == null && attempts < patrolPoints.Length)
        {
            currentPatrolIndex = (currentPatrolIndex + 1) % patrolPoints.Length;
            attempts++;
        }

        if (patrolPoints[currentPatrolIndex] != null)
        {
            SetPatrolDestination();
        }
    }

    void SetPatrolDestination()
    {
        if (hasValidPatrolPoints && patrolPoints[currentPatrolIndex] != null)
        {
            agent.SetDestination(patrolPoints[currentPatrolIndex].position);
            //if (showDebugLogs) Debug.Log($"{gameObject.name}: Moving to patrol point {currentPatrolIndex}");
        }
    }

    void HandleChasing()
    {
        if (player == null) return;

        // Continuously update destination to player position
        agent.SetDestination(player.position);

        // Check if caught the player
        float distanceToPlayer = Vector3.Distance(transform.position, player.position);
        if (distanceToPlayer <= catchDistance)
        {
            CatchPlayer();
        }
    }

    void HandleSearching()
    {
        // Check if we've reached the last known position
        if (!agent.pathPending && agent.remainingDistance < 0.5f)
        {
            // Search complete, return to patrol
            StartPatrolling();
        }
    }


    async void HandleCaught()
    {
        // You can add caught behavior here
        if (showDebugLogs) Debug.Log("Player caught!");


        await Task.Delay(1000);
        transform.position = new Vector3(-15.97f, 1f, 115.54f);
        agent.ResetPath();

        StartPatrolling();
    }

    void StartChasing()
    {
        if (currentState != EnemyState.Chasing)
        {
            if (showDebugLogs) Debug.Log($"{gameObject.name}: Started chasing!");
            currentState = EnemyState.Chasing;
            agent.speed = chaseSpeed;
            PlayChasingSound(); // Play chasing sound
            agent.isStopped = false;
            lastSeenTimer = 0f;
            lastKnownPlayerPosition = player.position;
        }
    }

    void StartSearching()
    {
        if (showDebugLogs) Debug.Log($"{gameObject.name}: Lost target, searching...");
        currentState = EnemyState.Searching;
        agent.speed = patrolSpeed;
        agent.isStopped = false;
        agent.SetDestination(lastKnownPlayerPosition);
        PlayWalkingSound(); // Back to walking sound when searching
    }

    void StartPatrolling()
    {
        if (showDebugLogs) Debug.Log($"{gameObject.name}: Returning to patrol");
        currentState = EnemyState.Patrolling;
        agent.speed = patrolSpeed;
        agent.isStopped = false;

        PlayWalkingSound(); // Play walking sound when patrolling

        if (hasValidPatrolPoints)
        {
            SetPatrolDestination();
        }
    }

    void CatchPlayer()
    {
        if (currentState != EnemyState.Caught)
        {
            if (showDebugLogs) Debug.Log($"{gameObject.name}: Caught player!");
            currentState = EnemyState.Caught;

            _PlayerMovement.OnCaught();
        }
    }

    // Audio methods
    void PlayWalkingSound()
    {
        if (audioSource != null && walkingSound != null)
        {
            audioSource.clip = walkingSound;
            audioSource.loop = true; // Walking sound should loop
            if (!audioSource.isPlaying)
            {
                audioSource.Play();
            }
        }
    }

    void PlayChasingSound()
    {
        if (audioSource != null && chasingSound != null)
        {
            audioSource.clip = chasingSound;
            audioSource.loop = true; // Chasing sound should loop
            audioSource.Play(); // Always play immediately when starting chase
        }
    }

    void StopAllAudio()
    {
        if (audioSource != null && audioSource.isPlaying)
        {
            audioSource.Stop();
        }
    }

    void UpdateAnimations()
    {
        if (animator == null) return;

        // Set animation parameters based on state and movement
        float speed = agent.velocity.magnitude;
        animator.SetFloat("Speed", speed);
        animator.SetBool("IsChasing", currentState == EnemyState.Chasing);
        animator.SetBool("IsCaught", currentState == EnemyState.Caught);
        animator.SetBool("IsPatrolling", currentState == EnemyState.Patrolling);
        animator.SetBool("IsSearching", currentState == EnemyState.Searching);

        if (showDebugLogs && Time.frameCount % 60 == 0) // Print every second
        {
            string animState = currentState switch
            {
                EnemyState.Chasing => "Chasing Animation",
                EnemyState.Patrolling => "Patrolling Animation",
                EnemyState.Searching => "Searching Animation",
                EnemyState.Caught => "Caught Animation",
                _ => "Unknown Animation"
            };

        }
    }

    // Public method to reset the AI (useful for respawning)
    public void ResetAI()
    {
        currentState = EnemyState.Patrolling;
        agent.isStopped = false;
        agent.speed = patrolSpeed;
        currentPatrolIndex = 0;
        lastSeenTimer = 0f;
        waitTimer = 0f;

        if (hasValidPatrolPoints)
        {
            SetPatrolDestination();
        }

        // Reset audio to walking sound
        PlayWalkingSound();
    }

    // Gizmos for debugging in Scene view
    void OnDrawGizmosSelected()
    {
        // Detection range
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);

        // Field of view
        Vector3 leftBoundary = Quaternion.Euler(0, -fieldOfViewAngle / 2, 0) * transform.forward * detectionRange;
        Vector3 rightBoundary = Quaternion.Euler(0, fieldOfViewAngle / 2, 0) * transform.forward * detectionRange;

        Gizmos.color = Color.red;
        Gizmos.DrawLine(transform.position, transform.position + leftBoundary);
        Gizmos.DrawLine(transform.position, transform.position + rightBoundary);

        // Catch distance
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, catchDistance);

        // Patrol points
        if (patrolPoints != null)
        {
            Gizmos.color = Color.blue;
            for (int i = 0; i < patrolPoints.Length; i++)
            {
                if (patrolPoints[i] != null)
                {
                    Gizmos.DrawWireSphere(patrolPoints[i].position, 0.5f);
                    if (i < patrolPoints.Length - 1 && patrolPoints[i + 1] != null)
                    {
                        Gizmos.DrawLine(patrolPoints[i].position, patrolPoints[i + 1].position);
                    }
                }
            }
            // Connect last to first
            if (patrolPoints.Length > 1 && patrolPoints[0] != null && patrolPoints[patrolPoints.Length - 1] != null)
            {
                Gizmos.DrawLine(patrolPoints[patrolPoints.Length - 1].position, patrolPoints[0].position);
            }
        }

        // Show current target
        if (Application.isPlaying && agent != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawLine(transform.position, agent.destination);
            Gizmos.DrawWireSphere(agent.destination, 0.3f);
        }
    }
}