using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

public class SeatGroupDirect : MonoBehaviour
{
    // 要实例化的游戏对象。

    public string[] objTypes = { "Quad", "Cube", "Cylinder", "Sphere", "Capsule" };

    public SeatData seatData;

    struct SeatsBatch
    {
        public MaterialPropertyBlock mpb;
        public Matrix4x4[] matrix;
        public Vector4[] colorArray;
        public float[] colArray;
        public float[] rowArray;
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
                GameObject obj = Instantiate(prefab);
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

        seatsBatch = new SeatsBatch[(number + 999) / 1000];

        int curBatch = 0;
        for (int i = 0; i < number; i++)
        {
            curBatch = i / 1000;
            if (i % 1000 == 0)
            {
                seatsBatch[curBatch].matrix = new Matrix4x4[number];
                seatsBatch[curBatch].colorArray = new Vector4[number];
                seatsBatch[curBatch].colArray = new float[number];
                seatsBatch[curBatch].rowArray = new float[number];
                seatsBatch[curBatch].batchSize = number - i > 1000 ? 1000 : number - i;
            }

            float col = (i / seatData.numberPerRow) / (float)seatData.numberPerCol;
            float row = (i % seatData.numberPerRow) / (float)seatData.numberPerRow;
            seatsBatch[curBatch].colArray[i%1000] = col;
            seatsBatch[curBatch].rowArray[i%1000] = row;

            seatsBatch[curBatch].colorArray[i % 1000].Set(Random.Range(0f, 1f), Random.Range(0f, 1f), Random.Range(0f, 1f), 1f);


            Vector3 pos = new Vector3(col * (seatData.EndX - seatData.StartX) + seatData.StartX, 0, row * (seatData.EndZ - seatData.StartZ) + seatData.StartZ);
            seatsBatch[curBatch].matrix[i%1000] = Matrix4x4.TRS(pos, Quaternion.identity, Vector3.one);
        }

        for (int i = 0; i < seatsBatch.Length; i++) 
        {
            seatsBatch[i].mpb = new MaterialPropertyBlock();
            seatsBatch[i].mpb.SetFloatArray("_Col", seatsBatch[i].colArray);
            seatsBatch[i].mpb.SetFloatArray("_Row", seatsBatch[i].rowArray);
            seatsBatch[i].mpb.SetVectorArray("_Color", seatsBatch[i].colorArray);

        }
    }

    void Update()
    {
        // 分批次执行
        for (int batch = 0; batch < seatsBatch.Length; batch++)
        {
            Graphics.DrawMeshInstanced(_meshes[_curMeshIndex], 0, _material, seatsBatch[batch].matrix, seatsBatch[batch].batchSize, seatsBatch[batch].mpb);
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