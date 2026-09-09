using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

namespace Exloris.Saving
{
    public static class SaveSystem
    {
        private const int IvSize = 16;
        private const int HashSize = 32;
        private static readonly byte[] FileHeader =
            { (byte)'E', (byte)'X', (byte)'2', (byte)'S' };

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

            byte[] iv = new byte[IvSize];
            byte[] cipherBytes;

            using (RandomNumberGenerator generator =
                   RandomNumberGenerator.Create())
            {
                generator.GetBytes(iv);
            }

            using (Aes aes = Aes.Create())
            {
                aes.Key = Key;
                aes.IV = iv;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;

                using (ICryptoTransform encryptor = aes.CreateEncryptor())
                {
                    cipherBytes = encryptor.TransformFinalBlock(
                        plainBytes,
                        0,
                        plainBytes.Length
                    );
                }
            }

            byte[] authenticatedBytes = new byte[IvSize + cipherBytes.Length];
            Buffer.BlockCopy(iv, 0, authenticatedBytes, 0, IvSize);
            Buffer.BlockCopy(
                cipherBytes,
                0,
                authenticatedBytes,
                IvSize,
                cipherBytes.Length
            );

            byte[] hash;
            using (HMACSHA256 hmac = new HMACSHA256(Key))
            {
                hash = hmac.ComputeHash(authenticatedBytes);
            }

            byte[] saveBytes =
                new byte[FileHeader.Length + HashSize + authenticatedBytes.Length];

            Buffer.BlockCopy(
                FileHeader,
                0,
                saveBytes,
                0,
                FileHeader.Length
            );

            Buffer.BlockCopy(
                hash,
                0,
                saveBytes,
                FileHeader.Length,
                HashSize
            );

            Buffer.BlockCopy(
                authenticatedBytes,
                0,
                saveBytes,
                FileHeader.Length + HashSize,
                authenticatedBytes.Length
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

                if (saveBytes.Length < FileHeader.Length + HashSize + IvSize)
                {
                    throw new InvalidDataException(
                        "The save file is too short."
                    );
                }

                for (int index = 0; index < FileHeader.Length; index++)
                {
                    if (saveBytes[index] != FileHeader[index])
                    {
                        throw new InvalidDataException(
                            "The save file uses an unsupported format."
                        );
                    }
                }

                byte[] expectedHash = new byte[HashSize];
                Buffer.BlockCopy(
                    saveBytes,
                    FileHeader.Length,
                    expectedHash,
                    0,
                    HashSize
                );

                int authenticatedLength =
                    saveBytes.Length - FileHeader.Length - HashSize;
                byte[] authenticatedBytes = new byte[authenticatedLength];
                Buffer.BlockCopy(
                    saveBytes,
                    FileHeader.Length + HashSize,
                    authenticatedBytes,
                    0,
                    authenticatedLength
                );

                byte[] actualHash;
                using (HMACSHA256 hmac = new HMACSHA256(Key))
                {
                    actualHash = hmac.ComputeHash(authenticatedBytes);
                }

                if (!CryptographicOperations.FixedTimeEquals(expectedHash, actualHash))
                {
                    throw new CryptographicException(
                        "The save file failed integrity validation."
                    );
                }

                byte[] iv = new byte[IvSize];
                byte[] cipherBytes = new byte[authenticatedLength - IvSize];
                Buffer.BlockCopy(authenticatedBytes, 0, iv, 0, IvSize);
                Buffer.BlockCopy(
                    authenticatedBytes,
                    IvSize,
                    cipherBytes,
                    0,
                    cipherBytes.Length
                );

                byte[] plainBytes;
                using (Aes aes = Aes.Create())
                {
                    aes.Key = Key;
                    aes.IV = iv;
                    aes.Mode = CipherMode.CBC;
                    aes.Padding = PaddingMode.PKCS7;

                    using (ICryptoTransform decryptor = aes.CreateDecryptor())
                    {
                        plainBytes = decryptor.TransformFinalBlock(
                            cipherBytes,
                            0,
                            cipherBytes.Length
                        );
                    }
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