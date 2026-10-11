using UnityEngine;

public class ScoreSystem : MonoBehaviour
{
[SerializeField] private int score = 0;

    public int Score
    {
        get { return score; }
        set { score = value; }
    }

    public void AddScore(int amount)
    {
        score += amount;
    }
    public void SubtractScore(int amount)
    {
        score -= amount;
    }

    public void ResetScore()
    {
        score = 0;
    }
}
