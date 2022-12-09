using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "SeatData", menuName = "ScriptableObjects/SeatData", order = 1)]
public class SeatData : ScriptableObject
{
    public string prefabName;

    public int numberPerRow;
    public int numberPerCol;
    public float StartX;
    public float StartZ;
    public float EndX;
    public float EndZ;
    
    //public int Numbers { get { return numberPerCol * numberPerRow; } }

    //public Vector3[] positions;
    //public Quaternion[] rotations;

    private void Awake()
    {
        Debug.Log("Awake");
    }

    public void Init()
    {
        //if (positions != null && positions.Length == Numbers)
        //{
        //    return;
        //}
        //positions = new Vector3[Numbers];
        //rotations = new Quaternion[Numbers];

        //float dist = 2f; 
        //for (int i = 0; i < Numbers; i++)
        //{
        //    positions[i].Set((i / numberPerRow - numberPerRow / 2)*dist, 10f * dist, (i % numberPerRow) * dist);
        //    rotations[i] = Quaternion.identity;

        //}
    }

}
