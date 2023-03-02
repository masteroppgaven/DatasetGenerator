using UnityEngine;

public class DeformingPrefabs : MonoBehaviour
{
    private Rigidbody rigidbody;
    private MeshFilter meshFilter;
    private Mesh mesh;
    private Vector3[] originalVertices;
    private Vector3[] deformedVertices;
    private Vector3[] forces;

    [SerializeField] private float deformationStrength = 0.1f;
    [SerializeField] private float damping = 0.1f;

    private void Awake()
    {
        rigidbody = GetComponent<Rigidbody>();
        if (rigidbody == null)
        {
            rigidbody = gameObject.AddComponent<Rigidbody>();
        }

        rigidbody.useGravity = true;
        rigidbody.mass = 10f;
        rigidbody.drag = 0.0f;
        rigidbody.angularDrag = 0.05f;
        rigidbody.isKinematic = false;

        meshFilter = GetComponent<MeshFilter>();
        mesh = meshFilter.mesh;
        originalVertices = mesh.vertices;
        deformedVertices = new Vector3[originalVertices.Length];
        forces = new Vector3[originalVertices.Length];
    }

    private void FixedUpdate()
    {
        Vector3 velocity = rigidbody.velocity;
        Vector3 angularVelocity = rigidbody.angularVelocity;

        for (int i = 0; i < deformedVertices.Length; i++)
        {
            Vector3 vertex = transform.TransformPoint(originalVertices[i]);
            Vector3 force = Vector3.zero;
            force += -deformationStrength * (vertex - transform.TransformPoint(deformedVertices[i]));
            force += -damping * velocity;
            force += -damping * angularVelocity;
            forces[i] = force;
            deformedVertices[i] = transform.InverseTransformPoint(vertex + forces[i] * Time.fixedDeltaTime);
        }
        mesh.vertices = deformedVertices;
        mesh.RecalculateNormals();
        meshFilter.mesh = mesh;
    }

    private void OnCollisionEnter(Collision collision)
    {
        Debug.Log("fedf");
        for (int i = 0; i < collision.contactCount; i++)
        {
            ContactPoint contact = collision.GetContact(i);
            Vector3 collisionPoint = contact.point;
            Vector3 normal = contact.normal;
            for (int j = 0; j < deformedVertices.Length; j++)
            {
                Vector3 vertex = transform.TransformPoint(originalVertices[j]);
                if (Vector3.Dot(vertex - collisionPoint, normal) < 0)
                {
                    forces[j] += normal * (Vector3.Dot(forces[j], normal) + Vector3.Dot(rigidbody.velocity, normal)) * 2.0f;
                }
            }
        }
    }

}