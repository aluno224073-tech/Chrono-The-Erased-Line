using UnityEngine;

public class coordinates : MonoBehaviour
{    
    public Camera playerCamera;
    public GameObject[] gameObjects;

    public void updatePosition(float coordinateX, float coordinateY, float coordinateZ)
    {
        transform.position = new Vector3(coordinateX, coordinateY, coordinateZ);
    }

    public void updateRotation(Quaternion rotation)
    {
        transform.rotation = rotation;
    }

    public void updateCameraRotation(Quaternion rotation)
    {
        playerCamera.transform.rotation = rotation;
    }

    // public void updateObjectPosition(int index, float coordinateX, float coordinateY, float coordinateZ)
    // {
    //     for (int i = 0; i < gameObjects.Length; i++)
    //     {
    //         gameObjects[i].transform.position = new Vector3(coordinateX, coordinateY, coordinateZ);
    //     }
    // }

    // public void updateObjectRotation(int index, Quaternion rotation)
    // {
    //     for (int i = 0; i < gameObjects.Length; i++)
    //     {
    //         gameObjects[i].transform.rotation = gameObjects[i].rotation;
    //     }
    // }
}
