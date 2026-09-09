using UnityEngine;
using Exloris.Saving;
using UnityEngine.Events;
namespace Exloris.Saving
{
    public sealed class SaveManager : MonoBehaviour
    {
        [SerializeField] private PlayerAvatarSelector avatarSelector;
        [SerializeField] private UnityEvent OnLoadedData;
        public static SaveManager Instance { get; private set; }

        private global::player currentPlayer;

        public PlayerData CurrentData { get; private set; }
        public PlayerData CurrentPlayerData { get; private set; }

        public bool HasLoadedData => CurrentData != null;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            LoadData();
            
        }

        /// <summary>
        /// Registers the player belonging to the current level.
        /// </summary>
        public void RegisterPlayer(global::player player)
        {
            currentPlayer = player;

            if (CurrentData != null)
            {
                ApplyDataToPlayer(currentPlayer, CurrentData);
            }

            if (CurrentPlayerData != null)
            {
                ApplyDataToPlayer(currentPlayer, CurrentPlayerData);
            }
        }
        public void SaveCurrentPlayer()
        {
            if (currentPlayer == null)
            {
                Debug.LogWarning(
                    "Cannot save because no player is registered."
                );

                return;
            }

            SaveSystem.SavePlayer(currentPlayer);
            CurrentData = SaveSystem.LoadPlayer();

            Debug.Log("Current player was saved.");
        }

        public PlayerData LoadData()
        {
            CurrentData = SaveSystem.LoadPlayer();

            if (CurrentData != null)
            {
                CurrentPlayerData = CurrentData;
                Debug.Log(
                    "Existing player save loaded. "
                    + "Level: " + CurrentData.level
                    + ", Step: " + CurrentData.step
                    + ", Gender: " + CurrentData.gender
                );
                OnLoadedData?.Invoke();
                //addthe logic of a saved player to the game here, such as loading the level and setting the player's position
            }
            else
            {
                Debug.Log("No existing player save found.");
            }

            return CurrentData;
        }

        public void LoadCurrentPlayer()
        {
            CurrentData = SaveSystem.LoadPlayer();

            if (CurrentData == null)
                return;

            if (currentPlayer == null)
            {
                Debug.LogWarning(
                    "Data was loaded, but no player is registered."
                );

                return;
            }

            ApplyDataToPlayer(currentPlayer, CurrentData);
        }

        public void DeleteSave()
        {
            SaveSystem.DeletePlayerSave();
            CurrentData = null;
        }

        private static void ApplyDataToPlayer(
            global::player player,
            PlayerData data
        )
        {
            // Change these assignments to match your actual fields.
            player.Level = data.level;
            player.Step = data.step;
            player.SetGender(data.gender);

        }

        private void OnApplicationPause(bool paused)
        {
            if (paused && currentPlayer != null)
            {
                SaveCurrentPlayer();
            }
        }

        private void OnApplicationQuit()
        {
            if (currentPlayer != null)
            {
                SaveCurrentPlayer();
            }
        }
        public void SetGender(PlayerGender selectedGender)
        {
            if (CurrentData == null)
            {
                CurrentData = new PlayerData
                {
                    level = 1,
                    step = 0
                };
            }

            CurrentData.gender = selectedGender;
            CurrentPlayerData = CurrentData;
            if (currentPlayer != null)
            {
                currentPlayer.SetGender(selectedGender);
                SaveSystem.SavePlayer(currentPlayer);
            }

            if (avatarSelector != null)
            {
                avatarSelector.ApplyGender(selectedGender);
            }
        }
    
    public void CreateNewPlayer(PlayerGender gender)
        {
            if (currentPlayer == null)
            {
                Debug.LogWarning(
                    "Cannot create a player because no player is registered."
                );
                return;
            }

            CurrentPlayerData = new PlayerData(currentPlayer);
            CurrentPlayerData.gender = gender;
            currentPlayer.SetGender(gender);
            SaveSystem.SavePlayer(currentPlayer);
            Debug.Log("New player created and saved with gender: " + gender);
        }
    public void CreateNewPlayer()
    {
            if (currentPlayer == null)
            {
                Debug.LogWarning(
                    "Cannot create a player because no player is registered."
                );
                return;
            }

            CurrentPlayerData = new PlayerData(currentPlayer);
            SaveSystem.SavePlayer(currentPlayer);
            Debug.Log("New player created with default values.");
    }
        public PlayerData GetCurrentPlayerData()
        {
            if (CurrentPlayerData == null)
            {
                CurrentPlayerData = SaveSystem.LoadPlayer();
            }

            return CurrentPlayerData;
        }
    }
    
}