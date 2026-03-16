using UnityEngine;

namespace Bomber.Gameplay
{
    public interface IDamageable
    {
        void TakeHit(GameObject source);
    }
}
