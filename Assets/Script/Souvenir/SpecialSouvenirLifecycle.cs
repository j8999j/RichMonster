using System;
using System.Collections.Generic;
using UnityEngine;

namespace Souvenir
{
    public sealed class SpecialSouvenirLifecycle
    {
        private readonly Func<IEnumerable<SpecialSouvenir>> _getAllSpecialSouvenirs;

        public SpecialSouvenirLifecycle(Func<IEnumerable<SpecialSouvenir>> getAllSpecialSouvenirs)
        {
            _getAllSpecialSouvenirs = getAllSpecialSouvenirs;
        }

        public void InitializeAll()
        {
            foreach (var souvenir in _getAllSpecialSouvenirs())
            {
                souvenir.InitializeLifecycle();
            }

            Debug.Log("[SpecialSouvenirLifecycle] Initialized all special souvenirs.");
        }

        public void ReleaseAll()
        {
            foreach (var souvenir in _getAllSpecialSouvenirs())
            {
                souvenir.ReleaseLifecycle();
            }

            Debug.Log("[SpecialSouvenirLifecycle] Released all special souvenirs.");
        }
    }
}
