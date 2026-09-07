using UnityEngine;

public class PlayerSelector : MonoBehaviour
{
    public void SelectBoy()
    {
        Exloris.Saving.SaveManager.Instance.SetGender(PlayerGender.Male); 
    }  
    public void SelectGirl()
    {
        Exloris.Saving.SaveManager.Instance.SetGender(PlayerGender.Female); 
    } 
}

