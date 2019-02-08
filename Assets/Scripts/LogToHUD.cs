using UnityEngine;
using UnityEngine.UI;

// https://forums.hololens.com/discussion/708/how-can-i-see-the-unity-debug-log-output-from-a-running-app-on-the-device
public class LogToHUD : MonoBehaviour
{
    public Text text;
    public int maxLines;

    void Start()
    {
        text.text = "";
    }

    void OnEnable()
    {
        Application.logMessageReceived += LogMessage;
        Debug.Log("Console on.");
    }

    void OnDisable()
    {
        Application.logMessageReceived -= LogMessage;
    }

    public void LogMessage(string message, string stackTrace, LogType type)
    {
        if (maxLines > 0)
        {
            if (text.text.Split('\n').Length > maxLines)
            {
                text.text = text.text.Split(new[] { '\n' }, 2)[1];
            }
        }
        text.text += "\n" + message;
    }
}