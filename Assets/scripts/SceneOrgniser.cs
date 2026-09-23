using UnityEngine;
using UnityEngine.Events;

public class SceneOrgniser : MonoBehaviour
{
    [SerializeField] private ManagersDetector _managersDetector;
    [SerializeField] private UnityEvent _startSceneEvent;
   private void Awake()
    {
        if (_managersDetector == null)
        {
            Debug.LogError("ManagersDetector reference is missing in SceneOrgniser.");
            return;
        }

        _startSceneEvent?.Invoke();
    }
}
