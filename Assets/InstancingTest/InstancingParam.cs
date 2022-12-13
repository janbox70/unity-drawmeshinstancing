using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "InstancingParam", menuName = "ScriptableObjects/InstancingParam", order = 1)]
public class InstancingParam: ScriptableObject
{
    public string[] objTypes = { "Quad", "Cube", "Cylinder", "Sphere", "Capsule" };

    public int numberPerRow;
    public int numberPerCol;
    public float StartX;
    public float EndX;
    public float StartZ;
    public float EndZ;

    private void Awake()
    {
        Debug.Log("Awake");
    }

    public List<Mesh> loadMeshes(Transform parent)
    {
        List<Mesh> _meshes = new List<Mesh>(objTypes.Length);

        foreach (string objType in objTypes)
        {
            string fullname = "Prefabs/" + objType;
            GameObject prefab = Resources.Load<GameObject>(fullname);
            if (prefab == null)
            {
                Debug.LogError($"failed load prefabs: {fullname}");
            }
            else
            {
                GameObject obj = Instantiate(prefab, parent);
                obj.SetActive(false);
                Mesh mesh = obj.GetComponent<MeshFilter>().mesh;
                _meshes.Add(mesh);
                Debug.Log($"{fullname}: {mesh.bounds}");
            }
        }

        return _meshes;
    }
}
