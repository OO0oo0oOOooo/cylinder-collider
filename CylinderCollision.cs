using UnityEngine;

// TODO: Clean up
// TODO: Stepping up lips

public class CylinderCollision : MonoBehaviour
{
    public GameObject _cylinder;

    [Header("Collision Parameters")]
    [SerializeField] private float _slopeLimit = 45;

    public Vector3 groundNormal;
    public bool onGround = false;


    [Header("SphereCast Parameters")]
    [SerializeField] private LayerMask _layerMask;

    public float CapsuleHalfHeight;

    [Header("SphereCast Feet Parameters")]
    [SerializeField] private float _spherecastFeetStart = 0f;
    [SerializeField] private float _spherecastFeetRadius = 0.32f;

    public bool SphereCastGrounded {get; private set; }

    private RaycastHit _feetHit;
    public RaycastHit FeetHit { get => _feetHit; }

    
    [Header("SphereCast Head Parameters")]
    [SerializeField] private float _spherecastHeadStart = -0.2f;
    [SerializeField] private float _spherecastHeadRadius = 0.3f;

    private RaycastHit _headHit;
    public RaycastHit HeadHit { get => _headHit; }

    private void Awake()
    {
        CapsuleHalfHeight = _cylinder.transform.localScale.y;
    }

    void FixedUpdate()
    {
        SphereCastFeet();
    }

    private void SphereCastFeet()
    {
        if (Physics.SphereCast(_cylinder.transform.position + (transform.up * _spherecastFeetStart), _spherecastFeetRadius, Vector3.down, out _feetHit, CapsuleHalfHeight, _layerMask))
            SphereCastGrounded = true;
        else
            SphereCastGrounded = false;
    }

    public bool SphereCastHead()
    {
        if (Physics.SphereCast(_cylinder.transform.position + (transform.up * _spherecastHeadStart), _spherecastHeadRadius, Vector3.up, out _headHit, CapsuleHalfHeight, _layerMask))
            return true;
        else
            return false;
    }

    private void OnCollisionStay(Collision other)
    {
        foreach (ContactPoint contact in other.contacts)
        {
            if (contact.normal.y > Mathf.Sin(_slopeLimit * Mathf.Deg2Rad + Mathf.PI / 2f))
            {
                groundNormal = contact.normal;
                onGround = true;
                return;
            }
        }
    }

    [SerializeField] private bool debugGroundNormal = false;
    [SerializeField] private bool debugSphereCast = false;

    private void OnDrawGizmos()
    {
        if(debugSphereCast)
        {
            // SPHERECAST VISUAL
            Gizmos.color = Color.black;
            Gizmos.DrawWireSphere(_cylinder.transform.position + (transform.up * _spherecastFeetStart) + (Vector3.down * CapsuleHalfHeight), _spherecastFeetRadius);
            Gizmos.DrawWireSphere(_cylinder.transform.position + (transform.up * _spherecastHeadStart) + (Vector3.up * CapsuleHalfHeight), _spherecastHeadRadius);
        }

        if(debugGroundNormal)
        {
            // NORMAL VISUAL
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(_feetHit.point, 0.1f);
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(_feetHit.point, _feetHit.point + _feetHit.normal * 2);
        }
    }
}