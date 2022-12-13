using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;


public class IndirectInstancing : MonoBehaviour
{
    // 使用GPU初始化参数，并控制动画
    public string[] objTypes = { "Quad", "Cube", "Cylinder", "Sphere", "Capsule" };

    public InstancingParam instancingParam;
    public ComputeShader compute;
    private Vector4 _region;

    private List<Mesh> _meshes = null;
    private int _curMeshIndex = 0;
    private Material _material = null;
    private GUIStyle _btnStyle = null;

    private ComputeBuffer argsBuffer;
    private ComputeBuffer meshPropertiesBuffer;

    private uint[] args = new uint[5] { 0, 0, 0, 0, 0 };
    private Bounds bounds = new Bounds(Vector3.zero, Vector3.one * 10);

    private struct MeshProperties
    {
        public Matrix4x4 mat;
        public Vector4 color;

        public static int Size()
        {
            return
                sizeof(float) * 4 * 4 + // matrix;
                sizeof(float) * 4;      // color;
        }
    }

    void Start()
    {
        Debug.Log("Start");

        initMesh();

        argsBuffer = new ComputeBuffer(5, sizeof(uint), ComputeBufferType.IndirectArguments);
        createBuffers();
    }

    void initMesh()
    {
        // 装入材质
        string fullname = "Material/IndirectInstancing";
        _material = Resources.Load(fullname, typeof(Material)) as Material;
        if (_material == null)
        {
            Debug.LogError($"failed load material: {fullname}");
        }

        // 装入 mesh
        _meshes = new List<Mesh>(objTypes.Length);

        foreach (string objType in objTypes)
        {
            fullname = "Prefabs/" + objType;
            GameObject prefab = Resources.Load<GameObject>(fullname);
            if (prefab == null)
            {
                Debug.LogError($"failed load prefabs: {fullname}");
            } 
            else
            {
                GameObject obj = Instantiate(prefab, this.transform);
                obj.SetActive(false);
                Mesh mesh = obj.GetComponent<MeshFilter>().mesh;
                _meshes.Add(mesh);
                Debug.Log($"{fullname}: {mesh.bounds}");
            }
        }
    }

    void createBuffers()
    {
        int instanceCount = instancingParam.numberPerRow * instancingParam.numberPerCol;

        DebugUI.DisplayMessage = $"InstanceCount: {instanceCount}";

        MeshProperties[] properties = new MeshProperties[instanceCount];
        for (int i = 0; i < instanceCount; i++)
        {
            float x = (i / instancingParam.numberPerRow) / (float)instancingParam.numberPerCol;
            float z = (i % instancingParam.numberPerRow) / (float)instancingParam.numberPerRow;

            //Vector3 pos = new Vector3(x * (instancingParam.EndX - instancingParam.StartX) + instancingParam.StartX, 0, z * (instancingParam.EndZ - instancingParam.StartZ) + instancingParam.StartZ);
            Vector3 pos = Vector3.zero;
            properties[i].mat = Matrix4x4.TRS(pos, Quaternion.identity, Vector3.one);
            properties[i].color = UnityEngine.Random.ColorHSV();
        }

        meshPropertiesBuffer = new ComputeBuffer(instanceCount, MeshProperties.Size());
        meshPropertiesBuffer.SetData(properties);

        int kernel = compute.FindKernel("CSMain");
        compute.SetBuffer(kernel, "_Properties", meshPropertiesBuffer);

        _material.SetBuffer("_Properties", meshPropertiesBuffer);

        Shader.SetGlobalFloat("_Col", instancingParam.numberPerCol);
        Shader.SetGlobalFloat("_Row", instancingParam.numberPerRow);
        Shader.SetGlobalVector("_Region", new Vector4(instancingParam.StartX, instancingParam.EndX, instancingParam.StartZ, instancingParam.EndZ));

        updateArgsBuffer();
    }

    void updateArgsBuffer() 
    {
        args[0] = (uint)_meshes[_curMeshIndex].GetIndexCount(0);
        args[1] = (uint)(instancingParam.numberPerRow * instancingParam.numberPerCol);
        args[2] = (uint)_meshes[_curMeshIndex].GetIndexStart(0);
        args[3] = (uint)_meshes[_curMeshIndex].GetBaseVertex(0);
        argsBuffer.SetData(args);
    }

    void Update()
    {
        int kernel = compute.FindKernel("CSMain");
        compute.Dispatch(kernel, Mathf.CeilToInt(instancingParam.numberPerRow * instancingParam.numberPerCol / 64f), 1, 1);

        Graphics.DrawMeshInstancedIndirect(_meshes[_curMeshIndex], 0, _material, bounds, argsBuffer);
    }

    void OnDisable()
    {
        if (meshPropertiesBuffer != null)
        {
            meshPropertiesBuffer.Release();
            meshPropertiesBuffer = null;
        }

        if (argsBuffer != null)
        {
            argsBuffer.Release();
            argsBuffer = null;
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
                updateArgsBuffer();
            }

            rc.xMin += space + 200;
            rc.xMax += space + 200;
        }
    }
}