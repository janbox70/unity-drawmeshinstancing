using System.Collections;
using System.Collections.Generic;
using System.Data;
using UnityEngine;

public class SeatGroup : MonoBehaviour
{
    // Ҫʵ��������Ϸ����
    public GameObject seatPrefab;

    public SeatData seatData;

    //�⽫���ӵ�������ʵ������ƣ����ڴ���ÿ��ʵ��ʱ������
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
            //�ڵ�ǰ���ɵ㴦����Ԥ�Ƽ���ʵ����
            float col = (instanceNumber / seatData.numberPerRow) / (float)seatData.numberPerCol;
            float row = (instanceNumber % seatData.numberPerRow) / (float)seatData.numberPerRow;

            Vector3 pos = new Vector3(col * (seatData.EndX - seatData.StartX) + seatData.StartX, 0, row * (seatData.EndZ - seatData.StartZ) + seatData.StartZ);
            GameObject currentEntity = Instantiate(seatPrefab, pos, Quaternion.identity, parent.transform);

            //��ʵ����ʵ�����������Ϊ ScriptableObject �ж�����ַ�����Ȼ��Ϊ�丽��һ��Ψһ��š�
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

    //�޸�ÿ��ʵ����PropertyBlock
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

        //���ÿ���������ɫ
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