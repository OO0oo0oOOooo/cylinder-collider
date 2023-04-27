using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class CylinderCollider : MonoBehaviour
{
    public GameObject Cylinder;
    [SerializeField] private Rigidbody _rb;


    [Header("Collision Parameters")]
    [SerializeField] private float _slopeLimit = 45;
    public Vector3 GroundNormal;
    public bool OnGround = false;


    [Header("SphereCast Parameters")]
    [SerializeField] private LayerMask _layerMask;
    public float CapsuleHalfHeight;


    [Header("SphereCast Feet Parameters")]
    [SerializeField] private float _spherecastFeetStart = 0f;
    [SerializeField] private float _spherecastFeetRadius = 0.32f;
    private RaycastHit _feetHit;
    public RaycastHit FeetHit { get => _feetHit; }
    public bool SphereCastGrounded {get; private set; }


    [Header("SphereCast Head Parameters")]
    [SerializeField] private float _spherecastHeadStart = 0f;
    [SerializeField] private float _spherecastHeadRadius = 0.3f;
    private RaycastHit _headHit;
    public RaycastHit HeadHit { get => _headHit; }


    [Header("Steps")]
    public float _maxStepHeight = 0.4f;
    public float _stepSearchOvershoot = 0.01f;
    private List<ContactPoint> _contactPoints = new List<ContactPoint>();
    private Vector3 _lastVelocity;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();
    }

    private void Start()
    {
        CapsuleHalfHeight = Cylinder.transform.localScale.y;
    }

    private void FixedUpdate()
    {
        SphereCastFeet();
        Step();
    }

    public void SetHeight(float value)
    {
        Cylinder.transform.localScale = new Vector3(1, value, 1);
        CapsuleHalfHeight = value;
    }

    private void SphereCastFeet()
    {
        if (Physics.SphereCast(Cylinder.transform.position + (transform.up * _spherecastFeetStart), _spherecastFeetRadius, Vector3.down, out _feetHit, CapsuleHalfHeight, _layerMask))
            SphereCastGrounded = true;
        else
            SphereCastGrounded = false;
    }

    public bool SphereCastHead()
    {
        if (Physics.SphereCast(Cylinder.transform.position + (transform.up * _spherecastHeadStart), _spherecastHeadRadius, Vector3.up, out _headHit, CapsuleHalfHeight, _layerMask))
            return true;
        else
            return false;
    }

    void OnCollisionEnter(Collision col)
    {
        _contactPoints.AddRange(col.contacts);
    }
 
    private void OnCollisionStay(Collision other)
    {
        _contactPoints.AddRange(other.contacts);

        foreach (ContactPoint contact in other.contacts)
        {
            if (contact.normal.y > Mathf.Sin(_slopeLimit * Mathf.Deg2Rad + Mathf.PI / 2f))
            {
                GroundNormal = contact.normal;
                OnGround = true;
                return;
            }
        }
    }


    #region Steps
    private void Step()
    {
        Vector3 velocity = _rb.velocity;

        bool grounded = FindGround(out ContactPoint groundCP, _contactPoints);

        Vector3 stepUpOffset = default;
        bool stepUp = false;
        if(grounded)
            stepUp = FindStep(out stepUpOffset, _contactPoints, groundCP, velocity);
        
        if(stepUp)
        {
            _rb.position += stepUpOffset;
            _rb.velocity = _lastVelocity;
        }
        
        _contactPoints.Clear();
        _lastVelocity = velocity;
    }

    bool FindGround(out ContactPoint groundCP, List<ContactPoint> contactPoints)
    {
        groundCP = default;
        bool found = false;
        foreach(ContactPoint point in contactPoints)
        {   
            if(point.normal.y > 0.0001f && (found == false || point.normal.y > groundCP.normal.y))
            {
                groundCP = point;
                found = true;
            }
        }
        
        return found;
    }
    
    bool FindStep(out Vector3 stepUpOffset, List<ContactPoint> contactPoints, ContactPoint groundCP, Vector3 currVelocity)
    {
        stepUpOffset = default;
        
        Vector2 velocityXZ = new Vector2(currVelocity.x, currVelocity.z);
        if(velocityXZ.sqrMagnitude < 0.0001f)
            return false;
        
        foreach(ContactPoint cp in contactPoints)
        {
            bool test = ResolveStepUp(out stepUpOffset, cp, groundCP);
            if(test)
                return test;
        }
        return false;
    }
    
    bool ResolveStepUp(out Vector3 stepUpOffset, ContactPoint stepTestCP, ContactPoint groundCP)
    {
        stepUpOffset = default;
        Collider stepCol = stepTestCP.otherCollider;
        
        if(Mathf.Abs(stepTestCP.normal.y) >= 0.01f)
            return false;
        
        if( !(stepTestCP.point.y - groundCP.point.y < _maxStepHeight) )
            return false;

        float stepHeight = groundCP.point.y + _maxStepHeight + 0.0001f;
        Vector3 stepTestInvDir = new Vector3(-stepTestCP.normal.x, 0, -stepTestCP.normal.z).normalized;

        Vector3 origin = new Vector3(stepTestCP.point.x, stepHeight, stepTestCP.point.z) + (stepTestInvDir * _stepSearchOvershoot);
        Vector3 direction = Vector3.down;

        if(!stepCol.Raycast(new Ray(origin, direction), out RaycastHit hitInfo, _maxStepHeight))
            return false;

        Vector3 stepUpPoint = new Vector3(stepTestCP.point.x, hitInfo.point.y+0.0001f, stepTestCP.point.z) + (stepTestInvDir * _stepSearchOvershoot);
        Vector3 stepUpPointOffset = stepUpPoint - new Vector3(stepTestCP.point.x, groundCP.point.y, stepTestCP.point.z);
        
        stepUpOffset = stepUpPointOffset;
        return true;
    }
    #endregion

    [SerializeField] private bool debugGroundNormal = false;
    [SerializeField] private bool debugSphereCast = false;
    
    private void OnDrawGizmos()
    {
        if(debugSphereCast)
        {
            // SPHERECAST VISUAL
            Gizmos.color = Color.black;
            Gizmos.DrawWireSphere(Cylinder.transform.position + (transform.up * _spherecastFeetStart) + (Vector3.down * CapsuleHalfHeight), _spherecastFeetRadius);
            Gizmos.DrawWireSphere(Cylinder.transform.position + (transform.up * _spherecastHeadStart) + (Vector3.up * CapsuleHalfHeight), _spherecastHeadRadius);
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