using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Rendering;


public class DirectInstancing : MonoBehaviour
{
    public InstancingParam param;

    struct InstancingBatch
    {
        public MaterialPropertyBlock mpb;
        public Matrix4x4[] matrix;
        public int batchSize;
    };

    private InstancingBatch[] batchData = null;
    private Material _material = null;


    void Start()
    {
        Debug.Log("Start");
        initMaterial();
        prepareBuffer();
    }

    void initMaterial()
    {
        // 装入材质 "Assets/Resources/Material/DirectInstancing.mat"
        string fullname = "Material/DirectInstancing";
        _material = Resources.Load(fullname, typeof(Material)) as Material;
        if (_material == null)
        {
            Debug.LogError($"failed load material: {fullname}");
        }
    }

    void prepareBuffer()
    {
        int number = param.numberPerRow * param.numberPerCol;

        int batchNumber = (number + 999) / 1000;

        batchData = new InstancingBatch[batchNumber];

        int index = 0;
        int batchSize = 1000;
        for (int curBatch = 0; curBatch < batchNumber; curBatch++)
        {
            if (curBatch == batchNumber - 1)
            {
                batchSize = number - curBatch * batchSize;
            }

            batchData[curBatch].batchSize = batchSize;
            batchData[curBatch].matrix = new Matrix4x4[batchSize];

            Vector4[]colorArray = new Vector4[batchSize];
            Vector4[]posArray  = new Vector4[batchSize];

            
            for(int i = 0; i < batchSize; i++, index ++)
            {
                // x: is col, y is row,  z 半径， w: 方向角 
                posArray[i].x = (index / param.numberPerRow) / (float)param.numberPerCol;
                posArray[i].y = (index % param.numberPerRow) / (float)param.numberPerRow;
                posArray[i].z = Mathf.Sqrt((posArray[i].x - 0.5f) * (posArray[i].x - 0.5f) + (posArray[i].y - 0.5f) * (posArray[i].y - 0.5f));
                colorArray[i] = Random.ColorHSV();

                Vector3 pos = new Vector3(posArray[i].x * (param.EndX - param.StartX) + param.StartX, 0, posArray[i].y * (param.EndZ - param.StartZ) + param.StartZ);
                batchData[curBatch].matrix[i] = Matrix4x4.TRS(pos, Quaternion.identity, Vector3.one);
            }

            batchData[curBatch].mpb = new MaterialPropertyBlock();
            batchData[curBatch].mpb.SetVectorArray("_CRRS", posArray);
            batchData[curBatch].mpb.SetVectorArray("_Color", colorArray);
        }
    }

    void Update()
    {
        // 分批次执行
        for (int batch = 0; batch < batchData.Length; batch++)
        {
            Graphics.DrawMeshInstanced(param.meshes[param.curMesh], 0, _material, batchData[batch].matrix, batchData[batch].matrix.Length, batchData[batch].mpb);
        }
    }

    private void OnEnable()
    {
        Debug.Log("DirectInstancing::OnEnable");
    }

    private void OnDisable()
    {
        Debug.Log("DirectInstancing::OnDisable");

    }
}