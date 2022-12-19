using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "InstancingParam", menuName = "ScriptableObjects/InstancingParam", order = 1)]
public class InstancingParam: ScriptableObject
{
    public Mesh[] meshes;
    public int curMesh;
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
}
