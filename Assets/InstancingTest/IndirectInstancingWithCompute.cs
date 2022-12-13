using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;


public class IndirectInstancingWithCompute : MonoBehaviour
{
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
        public Vector4 crrs;        // col/row/r/scale
        public Vector4 color;

        public static int Size()
        {
            return
                sizeof(float) * 4 * 4 + // matrix;
                sizeof(float) * 4 +      // crrs (uv, r, scale)
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
        string fullname = "Material/IndirectInstancingWithCompute";
        _material = Resources.Load(fullname, typeof(Material)) as Material;
        if (_material == null)
        {
            Debug.LogError($"failed load material: {fullname}");
        }

        // 装入 mesh
        _meshes = instancingParam.loadMeshes(this.transform);
    }

    void createBuffers()
    {
        int instanceCount = instancingParam.numberPerRow * instancingParam.numberPerCol;

        DebugUI.DisplayMessage = $"InstanceCount: {instanceCount}";

        Shader.SetGlobalFloat("_Col", instancingParam.numberPerCol);
        Shader.SetGlobalFloat("_Row", instancingParam.numberPerRow);
        Shader.SetGlobalVector("_Region", new Vector4(instancingParam.StartX, instancingParam.EndX, instancingParam.StartZ, instancingParam.EndZ));

        MeshProperties[] properties = new MeshProperties[instanceCount];
        for (int i = 0; i < instanceCount; i++)
        {
            // 除颜色外，其余的均在computeshader CSInit中初始化
            //float x = (i / instancingParam.numberPerRow) / (float)instancingParam.numberPerCol;
            //float z = (i % instancingParam.numberPerRow) / (float)instancingParam.numberPerRow;
            //Vector3 pos = new Vector3(x * (instancingParam.EndX - instancingParam.StartX) + instancingParam.StartX, 0, z * (instancingParam.EndZ - instancingParam.StartZ) + instancingParam.StartZ);
            //Vector3 pos = Vector3.zero;
            //properties[i].mat = Matrix4x4.TRS(pos, Quaternion.identity, Vector3.one);
            properties[i].color = UnityEngine.Random.ColorHSV();
        }

        meshPropertiesBuffer = new ComputeBuffer(instanceCount, MeshProperties.Size());
        meshPropertiesBuffer.SetData(properties);

        _material.SetBuffer("_Properties", meshPropertiesBuffer);

        int kernel = compute.FindKernel("CSUpdate");
        compute.SetBuffer(kernel, "_Properties", meshPropertiesBuffer);

        kernel = compute.FindKernel("CSInit");
        compute.SetBuffer(kernel, "_Properties", meshPropertiesBuffer);
        // 初始化buffer中的位置数据
        compute.Dispatch(kernel, Mathf.CeilToInt(instancingParam.numberPerRow * instancingParam.numberPerCol / 64f), 1, 1);

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
        int kernel = compute.FindKernel("CSUpdate");
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
        int space = (Screen.width - 200 * instancingParam.objTypes.Length )/(instancingParam.objTypes.Length+1);
        Rect rc = new Rect(space, Screen.height - 120, 200, 80);
        for( int i = 0; i < instancingParam.objTypes.Length; i++)
        {
            _btnStyle.normal.textColor = i == _curMeshIndex? Color.green : Color.gray;

            if (GUI.Button(rc, instancingParam.objTypes[i], _btnStyle))
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