using UnityEngine;


public class player : MonoBehaviour
{
    [SerializeField] private int level;
    [SerializeField] private int step;

    public int Level
    {
        get => level;
        private set => level = value;
    }
    public int Step
    {
        get => step;
        private set => step = value;
    }
}