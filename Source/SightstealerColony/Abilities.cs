using System;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;

namespace SightstealerColony
{
    public class CompProperties_InhumanScreech : CompProperties_AbilityEffect
    {
        public float radius = 10f;
        public float psychicComaChance = 0.1f;
        public CompProperties_InhumanScreech() { compClass = typeof(CompAbilityEffect_InhumanScreech); }
    }

    public class CompAbilityEffect_InhumanScreech : CompAbilityEffect
    {
        public new CompProperties_InhumanScreech Props => (CompProperties_InhumanScreech)props;
        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
        {
            base.Apply(target, dest);
            Pawn caster = parent.pawn;
            if (caster == null || caster.Map == null) return;
            float radiusSquared = Props.radius * Props.radius;
            foreach (Pawn victim in caster.Map.mapPawns.AllPawnsSpawned.ToList())
            {
                if (victim == caster || victim.Dead || !victim.Spawned || SightstealerUtility.IsSightstealer(victim)) continue;
                if (caster.Position.DistanceToSquared(victim.Position) > radiusSquared) continue;
                if (Rand.Chance(Props.psychicComaChance)) SightstealerUtility.AddHediff(victim, "PsychicComa");
                else if (victim.mindState != null) victim.mindState.mentalStateHandler.TryStartMentalState(MentalStateDefOf.PanicFlee);
            }
        }
    }

    public class CompProperties_Sacrifice : CompProperties_AbilityEffect
    {
        public float ritualSpotRadius = 4.9f;
        public CompProperties_Sacrifice() { compClass = typeof(CompAbilityEffect_Sacrifice); }
    }

    public class CompAbilityEffect_Sacrifice : CompAbilityEffect
    {
        public new CompProperties_Sacrifice Props => (CompProperties_Sacrifice)props;
        public override bool CanApplyOn(LocalTargetInfo target, LocalTargetInfo dest) => IsValidVictim(target) && HasRitualSpot(target.Pawn);
        public override bool Valid(LocalTargetInfo target, bool throwMessages = false) => IsValidVictim(target) && HasRitualSpot(target.Pawn);
        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
        {
            base.Apply(target, dest);
            Messages.Message("The Veiled Offering must be performed as a psychic ritual at a ritual spot.", MessageTypeDefOf.RejectInput, false);
        }

        private bool IsValidVictim(LocalTargetInfo target)
        {
            Pawn victim = target.Pawn;
            return victim != null && victim.Spawned && !victim.Dead && (victim.IsPrisonerOfColony || victim.IsSlaveOfColony);
        }

        private bool HasRitualSpot(Pawn victim)
        {
            ThingDef spotDef = SightstealerUtility.GetDef<ThingDef>("PsychicRitualSpot");
            if (spotDef == null || victim == null || victim.Map == null) return false;
            foreach (IntVec3 cell in GenRadial.RadialCellsAround(victim.Position, Props.ritualSpotRadius, true))
            {
                if (cell.x < 0 || cell.z < 0 || cell.x >= victim.Map.Size.x || cell.z >= victim.Map.Size.z) continue;
                foreach (Thing thing in victim.Map.thingGrid.ThingsListAtFast(cell))
                    if (thing.def == spotDef) return true;
            }
            return false;
        }
    }

    [StaticConstructorOnStartup]
    public static class SightstealerHarmony
    {
        static SightstealerHarmony()
        {
            Harmony harmony = new Harmony("spider.sightstealercolony");
            try
            {
                TryPatchFoodMethod(harmony, typeof(Thing), typeof(SightstealerFoodThingPatch));
                TryPatchFoodMethod(harmony, typeof(ThingDef), typeof(SightstealerFoodDefPatch));
                TryPatch(harmony, typeof(Thing), "Ingested", new[] { typeof(Pawn), typeof(float) }, typeof(SightstealerForcedFoodPatch));
            }
            catch (Exception ex)
            {
                Log.Error("[Sightstealer Colony] Harmony initialization failed; affected patches were skipped: " + ex);
            }
        }

        private static void TryPatchFoodMethod(Harmony harmony, Type foodType, Type patchType)
        {
            TryPatch(harmony, typeof(FoodUtility), "WillEat", new[] { typeof(Pawn), foodType, typeof(Pawn), typeof(bool), typeof(bool) }, patchType);
        }

        private static void TryPatch(Harmony harmony, Type targetType, string methodName, Type[] argumentTypes, Type patchType)
        {
            MethodInfo target = AccessTools.Method(targetType, methodName, argumentTypes);
            if (target == null)
            {
                Log.Error("[Sightstealer Colony] Skipping Harmony patch: missing target " + targetType.FullName + "." + methodName + ".");
                return;
            }
            MethodInfo postfix = AccessTools.Method(patchType, "Postfix");
            if (postfix == null)
            {
                Log.Error("[Sightstealer Colony] Skipping Harmony patch: missing postfix " + patchType.FullName + ".Postfix.");
                return;
            }
            harmony.Patch(target, null, new HarmonyMethod(postfix));
        }
    }

    public static class SightstealerFoodThingPatch
    {
        public static void Postfix(ref bool __result, Pawn p, Thing food)
        {
            if (SightstealerUtility.IsSightstealer(p)) __result = IsAllowedFood(food);
        }
        public static bool IsAllowedFood(Thing food) => food is Corpse || (food != null && food.def != null && food.def.defName == "Meat_Twisted");
    }

    public static class SightstealerFoodDefPatch
    {
        public static void Postfix(ref bool __result, Pawn p, ThingDef food)
        {
            if (SightstealerUtility.IsSightstealer(p)) __result = food != null && food.defName == "Meat_Twisted";
        }
    }

    public static class SightstealerForcedFoodPatch
    {
        public static void Postfix(Thing __instance, Pawn ingester)
        {
            if (!SightstealerUtility.IsSightstealer(ingester)) return;
            if (__instance is Corpse)
            {
                ThoughtDef thought = SightstealerUtility.GetDef<ThoughtDef>("SS_AteCorpse");
                if (thought != null && ingester.needs != null && ingester.needs.mood != null) ingester.needs.mood.thoughts.memories.TryGainMemory(thought);
                ThoughtDef bileThought = SightstealerUtility.GetDef<ThoughtDef>("SS_CorpseBile");
                if (bileThought != null && ingester.needs != null && ingester.needs.mood != null) ingester.needs.mood.thoughts.memories.TryGainMemory(bileThought);
                return;
            }
            if ((__instance.def == null || __instance.def.defName != "Meat_Twisted") && ingester.jobs != null)
                ingester.jobs.StartJob(JobMaker.MakeJob(JobDefOf.Vomit), JobCondition.InterruptForced, null, true);
        }
    }
}
