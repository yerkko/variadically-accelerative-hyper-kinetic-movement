using UnityEngine;
namespace DefaultNamespace
{
    public class SquashBall: Ball
    {

        public bool Squashed;
        protected override void OnCollisionEnter2D(Collision2D col)
        {
            base.OnCollisionEnter2D(col);
            if (MapManager.Instance.Mode == MapManager.GameMode.Squash && col.otherCollider.gameObject.TryGetComponent<SquashWall>(out var squashWall))
            {
                Squashed = true;
            }
        }
    }
}
