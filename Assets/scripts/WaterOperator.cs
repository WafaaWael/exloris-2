using UnityEngine;
using System.Collections.Generic;

public class WaterOperator : MonoBehaviour
{
    [SerializeField] private WaterAnimator _waterAnimator;
    [SerializeField] private Dictionary<string, float> _waterLevels = new Dictionary<string, float>
    {
        { "Level1", 6f },
        { "Level2", 8f },
        { "Level3", 10f }
    };

}
