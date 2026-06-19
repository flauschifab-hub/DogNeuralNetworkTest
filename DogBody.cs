using UnityEngine;

public class DogBody : MonoBehaviour
{
    public HingeJoint2D frontLeftUpper;
    public HingeJoint2D frontRightUpper;
    public HingeJoint2D backLeftUpper;
    public HingeJoint2D backRightUpper;

    public HingeJoint2D frontLeftLower;
    public HingeJoint2D frontRightLower;
    public HingeJoint2D backLeftLower;
    public HingeJoint2D backRightLower;

    public Rigidbody2D torso;
    public Rigidbody2D head;

    public Collider2D frontLeftFoot;
    public Collider2D frontRightFoot;
    public Collider2D backLeftFoot;
    public Collider2D backRightFoot;


    public HingeJoint2D[] GetAllJoints()
    {
        return new HingeJoint2D[]
        {
            frontLeftUpper,  frontLeftLower,
            frontRightUpper, frontRightLower,
            backLeftUpper,   backLeftLower,
            backRightUpper,  backRightLower
        };
    }
}

