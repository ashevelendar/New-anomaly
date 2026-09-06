using Verse;

namespace SightstealerColony
{
    public class SightstealerConfigExtension : DefModExtension
    {
        public float brightGlowThreshold = 0.5f;
        public float veryBrightGlowThreshold = 0.8f;
        public float darkGlowThreshold = 0.3f;
        public float deepDarkGlowThreshold = 0.15f;
        public int lightCheckIntervalTicks = 30;
        public int invisibilityCheckIntervalTicks = 30;
        public float darkHealingPerDay = 4f;
        public float deepDarkHealingPerDay = 12f;
        public int revealDurationTicks = 180;
    }
}
