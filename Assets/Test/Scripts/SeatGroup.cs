using System.Collections;
using System.Collections.Generic;
using System.Data;
using UnityEngine;

public class SeatGroup : MonoBehaviour
{
    // 要实例化的游戏对象。
    public GameObject seatPrefab;

    public SeatData seatData;

    //这将附加到创建的实体的名称，并在创建每个实体时递增。
    int instanceNumber = 1;

    MaterialPropertyBlock _mpb = null;

    void Start()
    {
        Debug.Log("Start");
        CreateSeats();
    }

    void CreateSeats()
    {
        //seatData.Init();

        GameObject parent = GameObject.Find("SeatGroup");
        for (int i = 0; i < seatData.numberPerRow * seatData.numberPerCol; i++)
        {
            //在当前生成点处创建预制件的实例。
            float col = (instanceNumber / seatData.numberPerRow) / (float)seatData.numberPerCol;
            float row = (instanceNumber % seatData.numberPerRow) / (float)seatData.numberPerRow;

            Vector3 pos = new Vector3(col * (seatData.EndX - seatData.StartX) + seatData.StartX, 0, row * (seatData.EndZ - seatData.StartZ) + seatData.StartZ);
            GameObject currentEntity = Instantiate(seatPrefab, pos, Quaternion.identity, parent.transform);

            //将实例化实体的名称设置为 ScriptableObject 中定义的字符串，然后为其附加一个唯一编号。
            currentEntity.name = seatPrefab.name + instanceNumber;

            SetPropertyBlockByGameObject(currentEntity, 0, row, col);

            instanceNumber++;
        }
    }

    private void FixedUpdate()
    {
        //float t = Time.realtimeSinceStartup * Mathf.PI / 180;
        //for (int i = 0;i<seatData.Numbers; i++)
        //{
        //    seatData.positions[i].y = 2f * Mathf.Sin(i * t);
        //}
    }

    //修改每个实例的PropertyBlock
    private bool SetPropertyBlockByGameObject(GameObject pGameObject, int group, float row, float col)
    {
        if (pGameObject == null)
        {
            return false;
        }
        if (_mpb == null)
        {
            _mpb = new MaterialPropertyBlock();
        }

        //随机每个对象的颜色
        _mpb.SetColor("_Color", new Color(Random.Range(0f, 1f), Random.Range(0f, 1f), Random.Range(0f, 1f), 1.0f));
        _mpb.SetFloat("_Phi", Random.Range(-40f, 40f));
        _mpb.SetInt("_Group", group);
        _mpb.SetFloat("_Row", row);
        _mpb.SetFloat("_Col", col);

        MeshRenderer meshRenderer = pGameObject.GetComponent<MeshRenderer>();
        if (meshRenderer == null)
        {
            return false;
        }

        meshRenderer.SetPropertyBlock(_mpb);

        return true;
    }
}