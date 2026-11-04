using UnityEngine;

public class coordinates : MonoBehaviour
{
    public void updatePosition(float coordinateX, float coordinateY, float coordinateZ)
    {
        transform.position = new Vector3(coordinateX, coordinateY, coordinateZ);
    }

    public void updateRotation(Quaternion rotation)
    {
        transform.rotation = rotation;
    }
}
