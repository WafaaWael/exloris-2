using UnityEngine;

[System.Serializable]
public class PlayerData 
{
    public int level;
    public int step;
    public PlayerGender gender;

    public PlayerData()
    {
        level = 1;
        gender = PlayerGender.NotSelected;
    }

    public PlayerData(player player)
    {
        this.level = player.Level;
        this.step = player.Step;
        this.gender = player.Gender;
    }
}
