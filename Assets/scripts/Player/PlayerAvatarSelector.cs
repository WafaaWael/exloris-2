using UnityEngine;

public class PlayerAvatarSelector : MonoBehaviour
{
    [SerializeField] private GameObject boyAvatar;
    [SerializeField] private GameObject girlAvatar;

    private void Start()
    {
      //  ApplyGender(Exloris.Saving.SaveManager.Instance.CurrentData?.gender ?? PlayerGender.NotSelected);
    }

    public void ApplyGender(PlayerGender gender)
    {
        switch (gender)
        {
            case PlayerGender.Male:
                boyAvatar.SetActive(true);
                girlAvatar.SetActive(false);
                break;


            case PlayerGender.Female:
                boyAvatar.SetActive(false);
                girlAvatar.SetActive(true);
                break;

            default:
                Debug.LogWarning(
                    "No gender was selected. Using the boy avatar.");

                boyAvatar.SetActive(true);
                girlAvatar.SetActive(false);
                break;
        }
    }
}
