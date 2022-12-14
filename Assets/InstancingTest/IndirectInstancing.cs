using System;
using UnityEngine;
using UnityEngine.Rendering;


public class IndirectInstancing : MonoBehaviour
{
    // 使用GPU初始化参数，并控制动画
    public InstancingParam param;

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

        initmMaterial();

        argsBuffer = new ComputeBuffer(5, sizeof(uint), ComputeBufferType.IndirectArguments);
        createBuffers();
    }

    void initmMaterial()
    {
        // 装入材质
        string fullname = "Material/IndirectInstancing";
        _material = Resources.Load(fullname, typeof(Material)) as Material;
        if (_material == null)
        {
            Debug.LogError($"failed load material: {fullname}");
        }
    }

    void createBuffers()
    {
        int instanceCount = param.numberPerRow * param.numberPerCol;
        DebugUI.DisplayMessage = $"InstanceCount: {instanceCount}";

        Shader.SetGlobalFloat("_Col", param.numberPerCol);
        Shader.SetGlobalFloat("_Row", param.numberPerRow);
        Shader.SetGlobalVector("_Region", new Vector4(param.StartX, param.EndX, param.StartZ, param.EndZ));

        MeshProperties[] properties = new MeshProperties[instanceCount];
        float scale = 1.0f;
        for (int i = 0; i < instanceCount; i++)
        {
            // 初始化位置及颜色
            properties[i].color = UnityEngine.Random.ColorHSV();

            float x = (i / param.numberPerRow) / (float)param.numberPerCol;
            float y = (i % param.numberPerRow) / (float)param.numberPerRow;
            properties[i].crrs = new Vector4(x, y, Mathf.Sqrt((x - 0.5f) * (x - 0.5f) + (y - 0.5f) * (y - 0.5f)), scale);
        }

        meshPropertiesBuffer = new ComputeBuffer(instanceCount, MeshProperties.Size());
        meshPropertiesBuffer.SetData(properties);

        _material.SetBuffer("_Properties", meshPropertiesBuffer);

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
        Graphics.DrawMeshInstancedIndirect(param.meshes[_curMeshIndex], 0, _material, bounds, argsBuffer);
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
        if (_btnStyle == null)
        {
            _btnStyle = new GUIStyle(GUI.skin.box);
            _btnStyle.fontSize = param.fontSize;
            _btnStyle.alignment = TextAnchor.MiddleCenter;
        }
        int space = (Screen.width - param.buttonWidth * param.meshes.Length) / (param.meshes.Length + 1);
        Rect rc = new Rect(space, Screen.height - param.buttonHeight * 1.5f, param.buttonWidth, param.buttonHeight);
        for (int i = 0; i < param.meshes.Length; i++)
        {
            _btnStyle.normal.textColor = i == _curMeshIndex ? Color.green : Color.gray;

            if (GUI.Button(rc, param.meshes[i].name, _btnStyle))
            {
                // 点击 Button 时执行此代码
                _curMeshIndex = i;
                updateArgsBuffer();
            }

            rc.xMin += space + param.buttonWidth;
            rc.xMax += space + param.buttonWidth;
        }
    }
}