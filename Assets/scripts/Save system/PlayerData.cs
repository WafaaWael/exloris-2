using UnityEngine;

[System.Serializable]
public class PlayerData 
{
    public int level;
    public int step;

    public PlayerData(player player)
    {
        this.level = player.Level;
        this.step = player.Step;
    }
}
