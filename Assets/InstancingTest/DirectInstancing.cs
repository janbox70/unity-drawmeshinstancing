using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;


public class DirectInstancing : MonoBehaviour
{
    // 要实例化的游戏对象。

    public string[] objTypes = { "Quad", "Cube", "Cylinder", "Sphere", "Capsule" };

    public InstancingParam instancingParam;

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

    private CommandBuffer _cmdBuff = null;


    void Start()
    {
        Debug.Log("Start");

        initMesh();
        CreateSeats();

        //prepareCommandBuffer();
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

        // 装入 mesh
        _meshes = new List<Mesh>(objTypes.Length);

        foreach (string objType in objTypes)
        {
            fullname = "Prefabs/Seat" + objType;
            GameObject prefab = Resources.Load<GameObject>(fullname);
            if (prefab == null)
            {
                Debug.LogError($"failed load prefabs: {fullname}");
            } 
            else
            {
                GameObject obj = Instantiate(prefab, this.transform);
                obj.SetActive(false);
                _meshes.Add(obj.GetComponent<MeshFilter>().mesh);
            }
        }
    }

    void CreateSeats()
    {
        int number = instancingParam.numberPerRow * instancingParam.numberPerCol;

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
                posArray[i].x = (index / instancingParam.numberPerRow) / (float)instancingParam.numberPerCol;
                posArray[i].y = (index % instancingParam.numberPerRow) / (float)instancingParam.numberPerRow;
                posArray[i].z = Mathf.Sqrt((posArray[i].x - 0.5f) * (posArray[i].x - 0.5f) + (posArray[i].y - 0.5f) * (posArray[i].y - 0.5f));
                colorArray[i].Set(Random.Range(0f, 1f), Random.Range(0f, 1f), Random.Range(0f, 1f), 1f);

                Vector3 pos = new Vector3(posArray[i].x * (instancingParam.EndX - instancingParam.StartX) + instancingParam.StartX, 0, posArray[i].y * (instancingParam.EndZ - instancingParam.StartZ) + instancingParam.StartZ);
                batchData[curBatch].matrix[i] = Matrix4x4.TRS(pos, Quaternion.identity, Vector3.one);
            }

            batchData[curBatch].mpb = new MaterialPropertyBlock();
            batchData[curBatch].mpb.SetVectorArray("_Pos", posArray);
            batchData[curBatch].mpb.SetVectorArray("_Color", colorArray);
        }
    }

    void Update()
    {
        // 分批次执行
        for (int batch = 0; batch < batchData.Length; batch++)
        {
            Graphics.DrawMeshInstanced(_meshes[_curMeshIndex], 0, _material, batchData[batch].matrix, batchData[batch].matrix.Length, batchData[batch].mpb);
        }
    }

    void prepareCommandBuffer()
    {
        if (_cmdBuff != null)
        {
            Camera.main.RemoveCommandBuffer(CameraEvent.BeforeForwardOpaque, _cmdBuff);
            CommandBufferPool.Release(_cmdBuff);
        }
        _cmdBuff = CommandBufferPool.Get("DrawMeshInstanced");
        for (int batch = 0; batch < batchData.Length; batch++)
        {
            _cmdBuff.DrawMeshInstanced(_meshes[_curMeshIndex], 0, _material, -1, batchData[batch].matrix, batchData[batch].matrix.Length, batchData[batch].mpb);
        }

        Camera.main.AddCommandBuffer(CameraEvent.BeforeForwardOpaque, _cmdBuff);
    }

    private void OnGUI()
    {
        if(_btnStyle == null)
        {
            _btnStyle = new GUIStyle(GUI.skin.box);
            _btnStyle.fontSize = 40;
            _btnStyle.alignment= TextAnchor.MiddleCenter;
        }
        int space = (Screen.width - 200 * objTypes.Length )/(objTypes.Length+1);
        Rect rc = new Rect(space, Screen.height - 120, 200, 80);
        for( int i = 0; i < objTypes.Length; i++)
        {
            _btnStyle.normal.textColor = i == _curMeshIndex? Color.green : Color.gray;

            if (GUI.Button(rc, objTypes[i], _btnStyle))
            {
                // 点击 Button 时执行此代码
                _curMeshIndex = i;

                // mesh修改了，需要更新 commandBuffer
                prepareCommandBuffer();
            }

            rc.xMin += space + 200;
            rc.xMax += space + 200;
        }
    }
}