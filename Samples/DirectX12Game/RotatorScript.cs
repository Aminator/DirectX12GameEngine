using System.Numerics;
using DirectX12GameEngine.Engine;

namespace DirectX12Game
{
    public class RotatorScript : SyncScript
    {
        public override void Update()
        {
            if (Entity is null) return;

            float deltaTime = (float)Game.Time.Elapsed.TotalSeconds;

            Entity.Transform.Rotation *= Quaternion.CreateFromAxisAngle(Vector3.UnitY, deltaTime * 0.2f);
        }
    }
}
