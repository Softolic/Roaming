using UnityEngine; 

public class PlayerControle : MonoBehaviour
{
    [SerializeField] private Rigidbody _rb;
    [SerializeField] private float _speed = 5;
       private Vector3 _input;
       void Update()
    {
        GatherInput();
        look();
    }
    void FixedUpdate()
    {
        Move();
    }
   void GatherInput()
    {
        _input = new Vector3(Input.GetAxisRaw("Horizontal"), 0, Input.GetAxisRaw("Vertical"));
    }
    void look()
    {
        var relative = (transform.position + _input) - transform.position;
        var rotation = Quaternion.LookRotation(relative, Vector3.up);

        transform.rotation = rotation;
    }

    void Move() {
        _rb.MovePosition(transform.position + transform.forward * _speed * Time.deltaTime);
    }
}
