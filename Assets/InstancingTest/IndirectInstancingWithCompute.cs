using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;


public class IndirectInstancingWithCompute : MonoBehaviour
{
    public InstancingParam param;
    public ComputeShader compute;

    public bool updateScale = false;

    private int _curMeshIndex = 0;
    private Material _material = null;

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

        initMaterial();

        argsBuffer = new ComputeBuffer(5, sizeof(uint), ComputeBufferType.IndirectArguments);
        createBuffers();
    }

    void initMaterial()
    {
        // 装入材质
        string fullname = "Material/IndirectInstancingWithCompute";
        _material = Resources.Load(fullname, typeof(Material)) as Material;
        if (_material == null)
        {
            Debug.LogError($"failed load material: {fullname}");
        }
    }

    void createBuffers()
    {
        int instanceCount = param.numberPerRow * param.numberPerCol;
 
        Shader.SetGlobalFloat("_Col", param.numberPerCol);
        Shader.SetGlobalFloat("_Row", param.numberPerRow);
        Shader.SetGlobalVector("_Region", new Vector4(param.StartX, param.EndX, param.StartZ, param.EndZ));

        MeshProperties[] properties = new MeshProperties[instanceCount];
        for (int i = 0; i < instanceCount; i++)
        {
            // 除颜色外，其余的均在computeshader CSInit中初始化
            //float x = (i / param.numberPerRow) / (float)param.numberPerCol;
            //float z = (i % param.numberPerRow) / (float)param.numberPerRow;
            //Vector3 pos = new Vector3(x * (param.EndX - param.StartX) + param.StartX, 0, z * (param.EndZ - param.StartZ) + param.StartZ);
            //Vector3 pos = Vector3.zero;
            //properties[i].mat = Matrix4x4.TRS(pos, Quaternion.identity, Vector3.one);
            properties[i].color = UnityEngine.Random.ColorHSV();
        }

        meshPropertiesBuffer = new ComputeBuffer(instanceCount, MeshProperties.Size());
        meshPropertiesBuffer.SetData(properties);

        _material.SetBuffer("_Properties", meshPropertiesBuffer);

        int kernel = compute.FindKernel(updateScale ? "CSUpdateScale" : "CSUpdateY");
        compute.SetBuffer(kernel, "_Properties", meshPropertiesBuffer);

        kernel = compute.FindKernel("CSInit");
        compute.SetBuffer(kernel, "_Properties", meshPropertiesBuffer);
        // 初始化buffer中的位置数据
        compute.Dispatch(kernel, Mathf.CeilToInt(param.numberPerRow * param.numberPerCol / 64f), 1, 1);

        updateArgsBuffer();
    }

    void updateArgsBuffer() 
    {
        args[0] = (uint)param.meshes[_curMeshIndex].GetIndexCount(0);
        args[1] = (uint)(param.numberPerRow * param.numberPerCol);
        args[2] = (uint)param.meshes[_curMeshIndex].GetIndexStart(0);
        args[3] = (uint)param.meshes[_curMeshIndex].GetBaseVertex(0);
        argsBuffer.SetData(args);
    }

    void Update()
    {
        if (_curMeshIndex != param.curMesh)
        {
            _curMeshIndex = param.curMesh;
            updateArgsBuffer();
        }

        int kernel = compute.FindKernel(updateScale ? "CSUpdateScale" : "CSUpdateY");
        compute.Dispatch(kernel, Mathf.CeilToInt(param.numberPerRow * param.numberPerCol / 64f), 1, 1);

        Graphics.DrawMeshInstancedIndirect(param.meshes[_curMeshIndex], 0, _material, bounds, argsBuffer);
    }

    void OnDestroy()
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
}