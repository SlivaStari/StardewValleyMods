using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Magic.Framework.Apis
{
    public interface ExperienceBarsApi
    {
        void DrawExperienceBar(Texture2D icon, int level, float percentFull, Color color);
        void SetDrawLuck(bool luck);
    }
}
