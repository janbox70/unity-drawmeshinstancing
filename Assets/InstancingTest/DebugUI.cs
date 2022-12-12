using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using UnityEngine;

public class DebugUI : MonoBehaviour
{
    static public string DisplayMessage = "";
    GUIStyle _style = null;

    // cpu
    public bool EnableCPUUsage = false;
    private int processorCount;
    private float updateInterval = 1f;

    private float CpuUsage = 0f;

    private Thread _cpuThread;
    private float _lasCpuUsage;

    // fps 
    private float _fps = 1f;
    private float _lastUpdateTime = 0f;
    private int _framesSinceLastUpdate = 0;

    private void Start()
    {
        _style = new GUIStyle();
        _style.fontSize = 32;
        _style.alignment = TextAnchor.MiddleCenter;
        _style.fixedWidth = Screen.width;
        _style.normal.textColor = Color.white;

        processorCount = SystemInfo.processorCount;
        _lastUpdateTime = Time.realtimeSinceStartup;

        //Application.runInBackground = true;

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
    }

    private void OnDestroy()
    {
        // Just to be sure kill the thread if this object is destroyed
        _cpuThread?.Abort();
    }

    private void Update()
    {
        _framesSinceLastUpdate++;

        float curTime  = Time.realtimeSinceStartup;
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
    private void OnGUI()
    {
        if (EnableCPUUsage)
        {
            GUILayout.Label($"FPS: {_fps:F2} CPU: {CpuUsage:F1}%  (Core: {processorCount})\n{DisplayMessage}", _style);
        } 
        else
        {
            GUILayout.Label($"FPS: {_fps:F2}\n{DisplayMessage}", _style);
        }
    }
}