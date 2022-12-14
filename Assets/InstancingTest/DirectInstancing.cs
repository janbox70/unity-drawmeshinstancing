using System.Collections.Generic;
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
    private List<Mesh> _meshes = null;
    private int _curMeshIndex = 0;
    private Material _material = null;
    private GUIStyle _btnStyle = null;


    void Start()
    {
        Debug.Log("Start");

        initMesh();
        createObjects();
    }

    void initMesh()
    {
        // 装入材质 "Assets/Resources/Material/DirectInstancing.mat"
        string fullname = "Material/DirectInstancing";
        _material = Resources.Load(fullname, typeof(Material)) as Material;
        if (_material == null)
        {
            Debug.LogError($"failed load material: {fullname}");
        }
    }

    void createObjects()
    {
        int number = param.numberPerRow * param.numberPerCol;

        DebugUI.DisplayMessage = $"InstanceCount: {number}";

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
            Graphics.DrawMeshInstanced(param.meshes[_curMeshIndex], 0, _material, batchData[batch].matrix, batchData[batch].matrix.Length, batchData[batch].mpb);
        }
    }

    private void OnGUI()
    {
        if(_btnStyle == null)
        {
            _btnStyle = new GUIStyle(GUI.skin.box);
            _btnStyle.fontSize = param.fontSize;
            _btnStyle.alignment= TextAnchor.MiddleCenter;
        }
        int space = (Screen.width - param.buttonWidth * param.meshes.Length )/(param.meshes.Length+1);
        Rect rc = new Rect(space, Screen.height - param.buttonHeight*1.5f, param.buttonWidth, param.buttonHeight);
        for( int i = 0; i < param.meshes.Length; i++)
        {
            _btnStyle.normal.textColor = i == _curMeshIndex? Color.green : Color.gray;

            if (GUI.Button(rc, param.meshes[i].name, _btnStyle))
            {
                // 点击 Button 时执行此代码
                _curMeshIndex = i;
            }

            rc.xMin += space + param.buttonWidth;
            rc.xMax += space + param.buttonWidth;
        }
    }
}