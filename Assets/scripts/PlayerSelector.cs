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
    private void CreatePlayer(PlayerGender gender)
    {
        Exloris.Saving.SaveManager.Instance.CreateNewPlayer(gender);
        //SceneManager.LoadSceneAsync(gameplaySceneName);
    }
}

