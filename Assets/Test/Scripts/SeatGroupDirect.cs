using UnityEngine;
using UnityEngine.Experimental.Rendering;

public class SeatGroupDirect : MonoBehaviour
{
    // 要实例化的游戏对象。
    public GameObject seatPrefab;

    public SeatData seatData;

    MaterialPropertyBlock _mpb = null;
    struct SeatsBatch
    {
        public Matrix4x4[] matrix;
        public Vector4[] colorArray;
        public float[] colArray;
        public float[] rowArray;
        public int batchSize;
    };

    private SeatsBatch[] seatsBatch = null; 

    private Mesh _mesh = null;

    private Material _material = null;

    void Start()
    {
        Debug.Log("Start");

        CreateSeats();
    }

    void CreateSeats()
    {
        GameObject obj = Instantiate(seatPrefab);
        obj.SetActive(false);

        _mesh = obj.GetComponent<MeshFilter>().mesh;
        Renderer renderer = obj.GetComponent<Renderer>();
        _material = renderer.material;

        int number = seatData.numberPerRow * seatData.numberPerCol;

        seatsBatch = new SeatsBatch[(number + 999) / 1000];

        _mpb = new MaterialPropertyBlock();

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
    }

    void Update()
    {
        // 分批次执行
        for (int batch = 0; batch < seatsBatch.Length; batch++)
        {
            _mpb.SetFloatArray("_Col", seatsBatch[batch].colArray);
            _mpb.SetFloatArray("_Row", seatsBatch[batch].rowArray);
            _mpb.SetVectorArray("_Color", seatsBatch[batch].colorArray);

            Graphics.DrawMeshInstanced(_mesh, 0, _material, seatsBatch[batch].matrix, seatsBatch[batch].batchSize, _mpb);
        }
    }
}