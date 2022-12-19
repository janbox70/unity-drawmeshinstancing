using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using UnityEngine;

public class InstancingHub : MonoBehaviour
{
    public InstancingParam param;
    private string[] _modeNames = { "DirectInstancing", "IndirectInstancing", "IndirectInstancingWithCompute" };
    public int curMode = 0;

    private GUIStyle _btnStyle = null;
    private GUIStyle _msgStyle = null;

    // cpu
    public bool EnableCPUUsage = false;
    private int processorCount;
    private float updateInterval = 1f;

    static public float CpuUsage = 0f;

    private Thread _cpuThread;
    private float _lasCpuUsage;


    // fps 
    static public float _fps = 1f;
    private float _lastUpdateTime = 0f;
    private int _framesSinceLastUpdate = 0;

    // Start is called before the first frame update

    void Start()
    {
        _msgStyle = new GUIStyle();
        _msgStyle.fontSize = 28;
        _msgStyle.alignment = TextAnchor.LowerLeft;
        _msgStyle.normal.textColor = Color.white;

        processorCount = SystemInfo.processorCount;
        _lastUpdateTime = Time.realtimeSinceStartup;

        //Application.runInBackground = true;
        //QualitySettings.vSyncCount = 0;
        //Application.targetFrameRate = -1;

        // setup the thread
        if (EnableCPUUsage)
        {
            _cpuThread = new Thread(UpdateCPUUsage)
            {
                IsBackground = true,
                // we don't want that our measurement thread
                // steals performance
                Priority = System.Threading.ThreadPriority.BelowNormal
            };

            // start the cpu usage thread
            _cpuThread.Start();
        }


        modeChanged();
    }
    private void modeChanged() 
    { 
        for (int i = 0; i < _modeNames.Length; i++)
        {
            GameObject obj = this.transform.Find(_modeNames[i]).gameObject;
            obj.SetActive(i == curMode);
        }
    }

    private void OnGUI()
    {
        if (_btnStyle == null)
        {
            _btnStyle = new GUIStyle(GUI.skin.box);
            _btnStyle.fontSize = 28;
            _btnStyle.alignment = TextAnchor.MiddleCenter;
        }
        GUILayout.BeginHorizontal();
        for (int i = 0; i < _modeNames.Length; i++)
        {
            _btnStyle.normal.textColor = i == curMode ? Color.green : Color.gray;

            if (GUILayout.Button(_modeNames[i], _btnStyle))
            {
                // 点击 Button 时执行此代码
                curMode = i;
                modeChanged();
            }
        }
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        for (int i = 0; i < param.meshes.Length; i++)
        {
            _btnStyle.normal.textColor = i == param.curMesh ? Color.green : Color.gray;

            if (GUILayout.Button(param.meshes[i].name, _btnStyle))
            {
                // 点击 Button 时执行此代码
                param.curMesh = i;
            }
        }
        GUILayout.EndHorizontal();

        int number = param.numberPerCol * param.numberPerRow;

        if (EnableCPUUsage)
        {
            GUILayout.Label($"InstanceCount: {number} FPS: {_fps:F2} CPU: {CpuUsage:F1}%  (Core: {processorCount})", _msgStyle);
        }
        else
        {
            GUILayout.Label($"InstanceCount: {number} FPS: {_fps:F2}", _msgStyle);
        }
    }

    private void OnDestroy()
    {
        // Just to be sure kill the thread if this object is destroyed
        _cpuThread?.Abort();
    }

    private void Update()
    {
        _framesSinceLastUpdate++;

        float curTime = Time.realtimeSinceStartup;
        if (curTime - _lastUpdateTime > updateInterval)
        {
            _fps = _framesSinceLastUpdate / (curTime - _lastUpdateTime);
            _framesSinceLastUpdate = 0;
            _lastUpdateTime = curTime;
        }

        if (EnableCPUUsage)
        {
            // for more efficiency skip if nothing has changed
            if (Mathf.Approximately(_lasCpuUsage, CpuUsage)) return;

            // the first two values will always be "wrong"
            // until _lastCpuTime is initialized correctly
            // so simply ignore values that are out of the possible range
            if (CpuUsage < 0 || CpuUsage > 100) return;

            // Update the value of _lasCpuUsage
            _lasCpuUsage = CpuUsage;
        }
    }

    /// <summary>
    /// Runs in Thread
    /// </summary>
    private void UpdateCPUUsage()
    {
        var lastCpuTime = new TimeSpan(0);

        // This is ok since this is executed in a background thread
        while (true)
        {
            var cpuTime = new TimeSpan(0);

            // Get a list of all running processes in this PC
            var AllProcesses = Process.GetProcesses();

            // Sum up the total processor time of all running processes
            cpuTime = AllProcesses.Aggregate(cpuTime, (current, process) => current + process.TotalProcessorTime);

            // get the difference between the total sum of processor times
            // and the last time we called this
            var newCPUTime = cpuTime - lastCpuTime;

            // update the value of _lastCpuTime
            lastCpuTime = cpuTime;

            // The value we look for is the difference, so the processor time all processes together used
            // since the last time we called this divided by the time we waited
            // Then since the performance was optionally spread equally over all physical CPUs
            // we also divide by the physical CPU count
            CpuUsage = 100f * (float)newCPUTime.TotalSeconds / updateInterval / processorCount;

            // Wait for UpdateInterval
            Thread.Sleep(Mathf.RoundToInt(updateInterval * 1000));
        }
    }
}
