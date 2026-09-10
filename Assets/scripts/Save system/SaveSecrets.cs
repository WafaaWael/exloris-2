using UnityEngine;

namespace Exloris.Saving
{
    internal static class SaveSecrets
    {
    // this is a base64 encoded 32 byte key, you can generate your own using the following command in a terminal:
    // openssl rand -base64 32
        internal const string KeyBase64 =
            "add your base64 encoded key here";
    }
}
