using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

namespace Exloris.Saving
{
    public static class SaveSystem
    {
        private const int NonceSize = 12;
        private const int TagSize = 16;

        private static string SavePath =>
            Path.Combine(
                Application.persistentDataPath,
                "player.save"
            );

        private static readonly byte[] Key =
            Convert.FromBase64String(
                SaveSecrets.KeyBase64
            );

        public static void SavePlayer(player player)
        {
            PlayerData data = new PlayerData(player);

            // Convert PlayerData into JSON bytes.
            string json = JsonUtility.ToJson(data);
            byte[] plainBytes = Encoding.UTF8.GetBytes(json);

            byte[] nonce = new byte[NonceSize];
            byte[] cipherBytes = new byte[plainBytes.Length];
            byte[] tag = new byte[TagSize];

            // A new nonce must be generated for every save.
            using (RandomNumberGenerator generator =
                   RandomNumberGenerator.Create())
            {
                generator.GetBytes(nonce);
            }

            using (AesGcm aes = new AesGcm(Key))
            {
                aes.Encrypt(
                    nonce,
                    plainBytes,
                    cipherBytes,
                    tag
                );
            }

            // File layout:
            // [12-byte nonce][16-byte tag][encrypted JSON]
            byte[] saveBytes =
                new byte[NonceSize + TagSize + cipherBytes.Length];

            Buffer.BlockCopy(
                nonce,
                0,
                saveBytes,
                0,
                NonceSize
            );

            Buffer.BlockCopy(
                tag,
                0,
                saveBytes,
                NonceSize,
                TagSize
            );

            Buffer.BlockCopy(
                cipherBytes,
                0,
                saveBytes,
                NonceSize + TagSize,
                cipherBytes.Length
            );

            // Base64 allows the encrypted bytes to be saved as text.
            string encodedSave =
                Convert.ToBase64String(saveBytes);

            File.WriteAllText(SavePath, encodedSave);

            Debug.Log("Player saved to: " + SavePath);
        }

        public static PlayerData LoadPlayer()
        {
            if (!File.Exists(SavePath))
            {
                Debug.LogWarning(
                    "Save file not found: " + SavePath
                );

                return null;
            }

            try
            {
                string encodedSave =
                    File.ReadAllText(SavePath);

                byte[] saveBytes =
                    Convert.FromBase64String(encodedSave);

                if (saveBytes.Length < NonceSize + TagSize)
                {
                    throw new InvalidDataException(
                        "The save file is too short."
                    );
                }

                byte[] nonce = new byte[NonceSize];
                byte[] tag = new byte[TagSize];

                int cipherLength =
                    saveBytes.Length - NonceSize - TagSize;

                byte[] cipherBytes =
                    new byte[cipherLength];

                byte[] plainBytes =
                    new byte[cipherLength];

                Buffer.BlockCopy(
                    saveBytes,
                    0,
                    nonce,
                    0,
                    NonceSize
                );

                Buffer.BlockCopy(
                    saveBytes,
                    NonceSize,
                    tag,
                    0,
                    TagSize
                );

                Buffer.BlockCopy(
                    saveBytes,
                    NonceSize + TagSize,
                    cipherBytes,
                    0,
                    cipherLength
                );

                using (AesGcm aes = new AesGcm(Key))
                {
                    aes.Decrypt(
                        nonce,
                        cipherBytes,
                        tag,
                        plainBytes
                    );
                }

                string json =
                    Encoding.UTF8.GetString(plainBytes);

                PlayerData data =
                    JsonUtility.FromJson<PlayerData>(json);

                if (data == null)
                {
                    Debug.LogError(
                        "Could not deserialize player data."
                    );

                    return null;
                }

                return data;
            }
            catch (Exception exception)
            {
                Debug.LogError(
                    "Failed to load player data: "
                    + exception.Message
                );

                return null;
            }
        }

        public static void DeletePlayerSave()
        {
            if (File.Exists(SavePath))
            {
                File.Delete(SavePath);
                Debug.Log("Player save deleted.");
            }
        }
    }
}