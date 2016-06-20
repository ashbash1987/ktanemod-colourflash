using System;
using System.Collections;
using System.Linq;
using UnityEngine;

/// <summary>
/// </summary>
public class StatusModule : MonoBehaviour
{
    #region Related Components
    public KMBombInfo BombInfo;
    public KMBombModule BombModule;
    public TextMesh Indicator;
    #endregion

    #region Unity Lifecycle Methods
    void Start()
    {
        StartCoroutine(DisplayIndicators());
    }
    #endregion

    #region Status Message Methods
    void ChangeMessage(string message)
    {
        Indicator.text = message;
    }

    IEnumerator DisplayIndicators()
    {
        string indicators = string.Join(",", BombInfo.GetIndicators().ToArray());
        ChangeMessage(string.Format("Indicators:{0}{1}", Environment.NewLine, indicators));
        yield return new WaitForSeconds(5.0f);
        yield return DisplayBatteries();
    }

    IEnumerator DisplayBatteries()
    {
        ChangeMessage(string.Format("Batteries:{0}{1} batteries{0}{2} holders", Environment.NewLine, BombInfo.GetBatteryCount(), BombInfo.GetBatteryHolderCount()));
        yield return new WaitForSeconds(5.0f);
        yield return DisplayPorts();
    }

    IEnumerator DisplayPorts()
    {
        string ports = string.Join(Environment.NewLine, BombInfo.GetPortPlates().Select((x) => string.Join(",", x)).ToArray());
        ChangeMessage(string.Format("Ports:{0}{1}", Environment.NewLine, ports));
        yield return new WaitForSeconds(5.0f);
        yield return DisplaySerial();
    }

    IEnumerator DisplaySerial()
    {
        ChangeMessage(string.Format("Serial:{0}{1}", Environment.NewLine, BombInfo.GetSerialNumber()));
        yield return new WaitForSeconds(5.0f);
        yield return DisplayIndicators();
    }
    #endregion
}
