using HarmonyLib;
using RimWorld;
using Verse;

namespace SightstealerColony
{
    [HarmonyPatch(typeof(PawnGenerator), nameof(PawnGenerator.GeneratePawn), new[] { typeof(PawnGenerationRequest) })]
    public static class SightstealerPawnGenerationPatch
    {
        public static void Prefix(ref PawnGenerationRequest request)
        {
            XenotypeDef sightstealerXenotype = DefDatabase<XenotypeDef>.GetNamedSilentFail("SS_Sightstealer");
            PawnKindDef sightstealerKind = DefDatabase<PawnKindDef>.GetNamedSilentFail("SS_Colonist");
            if (sightstealerXenotype == null || sightstealerKind == null)
            {
                return;
            }

            if (request.ForcedXenotype == sightstealerXenotype)
            {
                request.KindDef = sightstealerKind;
                return;
            }

            if (request.AllowedXenotypes != null && request.AllowedXenotypes.Contains(sightstealerXenotype))
            {
                request.KindDef = sightstealerKind;
            }
        }
    }
}
