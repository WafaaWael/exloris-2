using UnityEngine;
using BrunoMikoski.AnimationSequencer;
using System.Collections.Generic;

public class Tail : MonoBehaviour
{
        [SerializeField] private List<AnimationSequencer>  _buttonAnimation;

        private void Start()
        {
            foreach (var button in _buttonAnimation)
            {
                button.Play();
            }
        }
}
