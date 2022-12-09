using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using static UnityEngine.Rendering.DebugUI.Table;

public class SeatGroupDirect : MonoBehaviour
{
    // 要实例化的游戏对象。

    public string[] objTypes = { "Quad", "Cube", "Cylinder", "Sphere", "Capsule" };

    public SeatData seatData;

    struct SeatsBatch
    {
        public MaterialPropertyBlock mpb;
        public Matrix4x4[] matrix;
        public int batchSize;
    };

    private SeatsBatch[] seatsBatch = null;

    private List<Mesh> _meshes = null;

    private int _curMeshIndex = 0;

    private Material _material = null;

    private GUIStyle _btnStyle = null;

    void Start()
    {
        Debug.Log("Start");
        initMesh();
        CreateSeats();
    }

    void initMesh()
    {
        // 装入 mesh
        _meshes = new List<Mesh>(objTypes.Length);

        foreach (string objType in objTypes)
        {
            string fullname = "Prefabs/Seat" + objType;
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
                if (_material == null)
                {
                    Renderer renderer = obj.GetComponent<Renderer>();
                    _material = renderer.material;
                }
            }
        }
    }

    void CreateSeats()
    {
        int number = seatData.numberPerRow * seatData.numberPerCol;

        int batchNumber = (number + 999) / 1000;

        seatsBatch = new SeatsBatch[batchNumber];

        int index = 0;
        int batchSize = 1000;
        for (int curBatch = 0; curBatch < batchNumber; curBatch++)
        {
            if (curBatch == batchNumber - 1)
            {
                batchSize = number - curBatch * batchSize;
            }

            seatsBatch[curBatch].batchSize = batchSize;
            seatsBatch[curBatch].matrix = new Matrix4x4[batchSize];

            Vector4[]colorArray = new Vector4[batchSize];
            Vector4[]posArray  = new Vector4[batchSize];

            
            for(int i = 0; i < batchSize; i++, index ++)
            {
                // x: is col, y is row,  z 半径， w: 方向角 
                posArray[i].x = (index / seatData.numberPerRow) / (float)seatData.numberPerCol;
                posArray[i].y = (index % seatData.numberPerRow) / (float)seatData.numberPerRow;
                posArray[i].z = Mathf.Sqrt((posArray[i].x - 0.5f) * (posArray[i].x - 0.5f) + (posArray[i].y - 0.5f) * (posArray[i].y - 0.5f));
                colorArray[i].Set(Random.Range(0f, 1f), Random.Range(0f, 1f), Random.Range(0f, 1f), 1f);

                Vector3 pos = new Vector3(posArray[i].x * (seatData.EndX - seatData.StartX) + seatData.StartX, 0, posArray[i].y * (seatData.EndZ - seatData.StartZ) + seatData.StartZ);
                seatsBatch[curBatch].matrix[i] = Matrix4x4.TRS(pos, Quaternion.identity, Vector3.one);
            }

            seatsBatch[curBatch].mpb = new MaterialPropertyBlock();
            seatsBatch[curBatch].mpb.SetVectorArray("_Pos", posArray);
            seatsBatch[curBatch].mpb.SetVectorArray("_Color", colorArray);
        }
    }

    void Update()
    {
        // 分批次执行
        for (int batch = 0; batch < seatsBatch.Length; batch++)
        {
            Graphics.DrawMeshInstanced(_meshes[_curMeshIndex], 0, _material, seatsBatch[batch].matrix, seatsBatch[batch].matrix.Length, seatsBatch[batch].mpb);
        }
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
            }

            rc.xMin += space + 200;
            rc.xMax += space + 200;
        }
    }
}