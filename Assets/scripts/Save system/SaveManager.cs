using UnityEngine;

namespace Exloris.Saving
{
    public sealed class SaveManager : MonoBehaviour
    {
        public static SaveManager Instance { get; private set; }

        private global::player currentPlayer;

        public PlayerData CurrentData { get; private set; }

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
    }
}