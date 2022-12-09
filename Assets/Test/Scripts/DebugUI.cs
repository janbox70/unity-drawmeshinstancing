using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using UnityEngine;

public class DebugUI : MonoBehaviour
{
    //[Header("Components")]

    //[SerializeField] private TMP_Text perfText;

    //[Header("Settings")]

    //[Tooltip("In which interval should the CPU usage be updated?")]
    //[SerializeField] private float updateInterval = 1;

    //[Tooltip("The amount of physical CPU cores")]
    //[SerializeField] private int processorCount;


    //[Header("Output")]
    //public float CpuUsage;


    private int processorCount;
    private float updateInterval = 1f;
    public float CpuUsage = 0f;

    private Thread _cpuThread;
    private float _lasCpuUsage;

    private float _fps = 1f;
    private float _lastUpdateTime = 0f;
    private int _framesSinceLastUpdate = 0;
    GUIStyle _style = null;

    private void Start()
    {
        _style = new GUIStyle();
        _style.fontSize = 32;
        _style.alignment = TextAnchor.MiddleCenter;
        _style.fixedWidth = Screen.width;
        _style.normal.textColor = Color.white;

        processorCount = SystemInfo.processorCount;
        if (processorCount == 0)
        {
            processorCount = 1;
        }

        Application.runInBackground = true;

        _lastUpdateTime = Time.realtimeSinceStartup;

        // setup the thread
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

    private void OnValidate()
    {
        // We want only the physical cores but usually
        // this returns the twice as many virtual core count
        //
        // if this returns a wrong value for you comment this method out
        // and set the value manually
        //processorCount = SystemInfo.processorCount;
        //if (processorCount == 0)
        //{
        //    processorCount = 1;
        //}
    }

    private void OnDestroy()
    {
        // Just to be sure kill the thread if this object is destroyed
        _cpuThread?.Abort();
    }

    private void Update()
    {
        _framesSinceLastUpdate++;
        if (Time.realtimeSinceStartup - _lastUpdateTime > 0.5)
        {
            _fps = _framesSinceLastUpdate / (Time.realtimeSinceStartup - _lastUpdateTime);
            _framesSinceLastUpdate = 0;
            _lastUpdateTime = Time.realtimeSinceStartup;
        }

        // for more efficiency skip if nothing has changed
        if (Mathf.Approximately(_lasCpuUsage, CpuUsage)) return;

        // the first two values will always be "wrong"
        // until _lastCpuTime is initialized correctly
        // so simply ignore values that are out of the possible range
        if (CpuUsage < 0 || CpuUsage > 100) return;

        // Update the value of _lasCpuUsage
        _lasCpuUsage = CpuUsage;
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
    private void OnGUI()
    {
        GUILayout.Label($"FPS: {_fps:F2}\nCPU: {CpuUsage:F1}%  (Core: {processorCount})", _style);
    }
}