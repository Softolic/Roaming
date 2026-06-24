using UnityEngine; 

public class PlayerControle : MonoBehaviour
{
    [SerializeField] private Rigidbody _rb;
    [SerializeField] private float _speed = 5;
    [SerializeField] private float _turnSpeed = 360;
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

        var matrix = Matrix4x4.Rotate(Quaternion.EuLer(0,45,0));

        var inputtorto = matrix.MultiplyPoint3x4(_input);

        if(_input != Vector3.zero){
        var relative = (transform.position + inputtorto) - transform.position;
        var rotation = Quaternion.LookRotation(relative, Vector3.up);

        transform.rotation = Quaternion.RotateTowards(transform.rotation, rotation, _turnSpeed * Time.deltaTime);
    }
    }

    void Move() {
        _rb.MovePosition(transform.position + (transform.forward * _input.magnitude) * _speed * Time.deltaTime);
    }
}
