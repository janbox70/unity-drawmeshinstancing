using System;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;


public class IndirectInstancing : MonoBehaviour
{
    // ʹ��GPU��ʼ�������������ƶ���
    public InstancingParam instancingParam;
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
        public Vector4 crrs;            // col/row/r/scale
        public Vector4 color;

        public static int Size()
        {
            return
                sizeof(float) * 4 +      // crrs; 
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
        // װ�����
        string fullname = "Material/IndirectInstancing";
        _material = Resources.Load(fullname, typeof(Material)) as Material;
        if (_material == null)
        {
            Debug.LogError($"failed load material: {fullname}");
        }

        // װ�� mesh
        _meshes = instancingParam.loadMeshes(this.transform);
    }

    void createBuffers()
    {
        int instanceCount = instancingParam.numberPerRow * instancingParam.numberPerCol;

        DebugUI.DisplayMessage = $"InstanceCount: {instanceCount}";

        MeshProperties[] properties = new MeshProperties[instanceCount];
        float scale = 1.0f;
        for (int i = 0; i < instanceCount; i++)
        {
            // ��ʼ��λ�ü���ɫ
            properties[i].color = UnityEngine.Random.ColorHSV();

            float x = (i / instancingParam.numberPerRow) / (float)instancingParam.numberPerCol;
            float y = (i % instancingParam.numberPerRow) / (float)instancingParam.numberPerRow;
            properties[i].crrs = new Vector4(x, y, Mathf.Sqrt((x - 0.5f) * (x - 0.5f) + (y - 0.5f) * (y - 0.5f)), scale);
        }

        meshPropertiesBuffer = new ComputeBuffer(instanceCount, MeshProperties.Size());
        meshPropertiesBuffer.SetData(properties);

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
                // ��� Button ʱִ�д˴���
                _curMeshIndex = i;
                updateArgsBuffer();
            }

            rc.xMin += space + 200;
            rc.xMax += space + 200;
        }
    }
}