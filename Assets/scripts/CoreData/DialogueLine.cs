using System;
using UnityEngine;

namespace BoatGame.CoreData
{
    [Serializable]
    public struct DialogueLine
    {
        [TextArea(2, 4)] public string Text;
        public AudioClip Voice;
    }
}
