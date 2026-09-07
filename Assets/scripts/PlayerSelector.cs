using UnityEngine;

public class PlayerUISelector : MonoBehaviour
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

