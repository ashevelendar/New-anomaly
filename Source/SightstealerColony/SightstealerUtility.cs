using RimWorld;
using Verse;

namespace SightstealerColony
{
    public static class SightstealerUtility
    {
        private const string XenotypeDefName = "SS_Sightstealer";
        private static readonly SightstealerConfigExtension DefaultConfig = new SightstealerConfigExtension();

        public static SightstealerConfigExtension Config
        {
            get
            {
                XenotypeDef xenotype = GetDef<XenotypeDef>(XenotypeDefName);
                if (xenotype != null)
                {
                    SightstealerConfigExtension config = xenotype.GetModExtension<SightstealerConfigExtension>();
                    if (config != null) return config;
                }
                return DefaultConfig;
            }
        }

        public static bool HasMap(Pawn pawn) => pawn != null && pawn.Spawned && pawn.Map != null;

        public static bool IsSightstealer(Pawn pawn)
        {
            if (pawn == null) return false;
            GeneDef physiology = GetDef<GeneDef>("SS_SightstealerPhysiology");
            return physiology != null && pawn.genes != null && pawn.genes.HasActiveGene(physiology);
        }

        public static float GlowAt(Pawn pawn)
        {
            if (!HasMap(pawn)) return -1f;
            return Clamp01(pawn.Map.glowGrid.GroundGlowAt(pawn.Position, true, false));
        }

        public static float DarknessAt(Pawn pawn)
        {
            float glow = GlowAt(pawn);
            return glow < 0f ? -1f : 1f - glow;
        }

        public static bool IsDark(Pawn pawn)
        {
            float glow = GlowAt(pawn);
            return glow >= 0f && glow < Config.darkGlowThreshold;
        }

        public static bool IsDeepDark(Pawn pawn)
        {
            float glow = GlowAt(pawn);
            return glow >= 0f && glow < Config.deepDarkGlowThreshold;
        }

        public static int CheckInterval(int configuredInterval) => configuredInterval < 1 ? 1 : configuredInterval;

        public static float Clamp01(float value)
        {
            if (value < 0f) return 0f;
            return value > 1f ? 1f : value;
        }

        public static T GetDef<T>(string defName) where T : Def
        {
            T def = DefDatabase<T>.GetNamedSilentFail(defName);
            if (def == null)
                Log.ErrorOnce("[Sightstealer Colony] Missing " + typeof(T).Name + " def: " + defName, defName.GetHashCode() ^ typeof(T).GetHashCode());
            return def;
        }

        public static void AddHediff(Pawn pawn, string defName)
        {
            if (pawn == null || pawn.health == null) return;
            HediffDef def = GetDef<HediffDef>(defName);
            if (def == null || pawn.health.hediffSet.GetFirstHediffOfDef(def, false) != null) return;
            pawn.health.AddHediff(def);
        }

        public static void RemoveHediff(Pawn pawn, string defName)
        {
            if (pawn == null || pawn.health == null) return;
            HediffDef def = GetDef<HediffDef>(defName);
            if (def == null) return;
            Hediff hediff = pawn.health.hediffSet.GetFirstHediffOfDef(def, false);
            if (hediff != null) pawn.health.RemoveHediff(hediff);
        }
    }
}
