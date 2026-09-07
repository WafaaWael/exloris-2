using UnityEngine;


public class player : MonoBehaviour
{
    [SerializeField] private int level;
    [SerializeField] private int step;
    [SerializeField] private PlayerGender _gender;

public PlayerGender Gender => _gender;
    public int Level
    {
        get => level;
        set => level = value;
    }
    public int Step
    {
        get => step;
        set => step = value;
    }
    public void SetGender(PlayerGender selectedGender)
    {
        _gender = selectedGender;
    }
    private void Start()
    {
       Exloris.Saving.SaveManager.Instance.RegisterPlayer(this);
    }
}
public enum PlayerGender
{
    NotSelected = 0,
    Male = 1,
    Female = 2
}