using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using Verse.AI.Group;

namespace SightstealerColony
{
    public class PsychicRitualDef_VeiledOffering : PsychicRitualDef
    {
        public override AcceptanceReport AllowsFloatMenu(Pawn pawn)
        {
            if (!SightstealerUtility.IsSightstealer(pawn)) return new AcceptanceReport("A Sightstealer must lead this ritual.");
            return base.AllowsFloatMenu(pawn);
        }
        public override AcceptanceReport AllowsDrafting(Pawn pawn)
        {
            if (!SightstealerUtility.IsSightstealer(pawn)) return new AcceptanceReport("A Sightstealer must lead this ritual.");
            return base.AllowsDrafting(pawn);
        }
        public override List<PsychicRitualToil> CreateToils(PsychicRitual ritual, PsychicRitualGraph graph)
        {
            List<PsychicRitualToil> toils = base.CreateToils(ritual, graph);
            toils.Add(new PsychicRitualToil_VeiledOffering());
            return toils;
        }
    }

    public class PsychicRitualToil_VeiledOffering : PsychicRitualToil
    {
        public override bool Tick(PsychicRitual ritual, PsychicRitualGraph graph) => true;
        public override void End(PsychicRitual ritual, PsychicRitualGraph graph, bool success)
        {
            if (success) VeiledOfferingUtility.Resolve(ritual);
        }
    }

    public static class VeiledOfferingUtility
    {
        private const string TargetRoleDefName = "SS_OfferingTarget";
        public static void Resolve(PsychicRitual ritual)
        {
            if (ritual == null || ritual.Map == null || ritual.assignments == null) return;
            Pawn victim = ritual.assignments.FirstAssignedPawn(SightstealerUtility.GetDef<PsychicRitualRoleDef>(TargetRoleDefName));
            List<Pawn> participants = new List<Pawn>();
            foreach (Pawn participant in ritual.assignments.AllAssignedPawns)
                if (participant != null && participant.Spawned) participants.Add(participant);
            Pawn invoker = participants.FirstOrDefault(SightstealerUtility.IsSightstealer);
            if (victim == null || invoker == null || !IsValidVictim(victim) || !HasRitualSpot(victim))
            {
                Messages.Message("The Veiled Offering cannot find a valid living offering and ritual spot.", MessageTypeDefOf.RejectInput, false);
                return;
            }
            float glow = SightstealerUtility.GlowAt(invoker);
            if (glow < 0f || glow >= SightstealerUtility.Config.darkGlowThreshold)
            {
                Messages.Message("The Veiled Offering fails in the light. The offering remains untouched.", MessageTypeDefOf.RejectInput, false);
                ApplyFailureEffects(participants, false);
                return;
            }
            float quality = SightstealerUtility.Clamp01(ritual.PowerPercent + (SightstealerUtility.IsDeepDark(invoker) ? 0.2f : 0.08f));
            if (quality < 0.65f && Rand.Chance(0.05f))
            {
                victim.Kill(null); ApplyFailureEffects(participants, true);
                Messages.Message("The Veiled Offering consumes the prisoner, but nothing emerges.", MessageTypeDefOf.NegativeEvent, false);
                return;
            }
            if (quality < 0.35f)
            {
                victim.Kill(null); ApplyFailureEffects(participants, true);
                Messages.Message("The Veiled Offering collapses into psychic ruin.", MessageTypeDefOf.NegativeEvent, false);
                return;
            }
            Pawn replacement = GenerateReplacement(invoker);
            if (replacement == null) return;
            CopyInheritance(victim, replacement);
            if (quality >= 0.9f)
            {
                SightstealerUtility.AddHediff(replacement, "SS_RitualExaltation");
                Messages.Message("The Veiled Offering succeeds: an exceptional Sightstealer appears.", replacement, MessageTypeDefOf.PositiveEvent);
            }
            else if (quality >= 0.65f)
                Messages.Message("The Veiled Offering succeeds: a new Sightstealer appears.", replacement, MessageTypeDefOf.PositiveEvent);
            else
            {
                SightstealerUtility.AddHediff(replacement, "SS_RitualWound");
                Messages.Message("The Veiled Offering produces a weakened Sightstealer.", replacement, MessageTypeDefOf.CautionInput);
            }
            IntVec3 spawnCell = FindSpawnCell(invoker);
            victim.Kill(null);
            GenSpawn.Spawn(replacement, spawnCell, ritual.Map);
        }
        private static bool IsValidVictim(Pawn pawn) => pawn != null && pawn.Spawned && !pawn.Dead && (pawn.IsPrisonerOfColony || pawn.IsSlaveOfColony);
        private static bool HasRitualSpot(Pawn victim)
        {
            ThingDef spotDef = SightstealerUtility.GetDef<ThingDef>("PsychicRitualSpot");
            if (spotDef == null) return false;
            foreach (IntVec3 cell in GenRadial.RadialCellsAround(victim.Position, 4.9f, true))
            {
                if (cell.x < 0 || cell.z < 0 || cell.x >= victim.Map.Size.x || cell.z >= victim.Map.Size.z) continue;
                foreach (Thing thing in victim.Map.thingGrid.ThingsListAtFast(cell)) if (thing.def == spotDef) return true;
            }
            return false;
        }
        private static Pawn GenerateReplacement(Pawn invoker)
        {
            PawnKindDef kind = SightstealerUtility.GetDef<PawnKindDef>("SS_Colonist");
            XenotypeDef xenotype = SightstealerUtility.GetDef<XenotypeDef>("SS_Sightstealer");
            if (kind == null || xenotype == null) return null;
            PawnGenerationRequest request = default(PawnGenerationRequest);
            request.Context = PawnGenerationContext.NonPlayer;
            request.KindDef = kind;
            request.Faction = invoker.Faction;
            request.Tile = invoker.Tile;
            request.ForceGenerateNewPawn = true;
            request.FixedBiologicalAge = Rand.Range(1000f, 2000f);
            request.FixedChronologicalAge = request.FixedBiologicalAge;
            request.ForcedXenotype = xenotype;
            request.CanGeneratePawnRelations = false;
            request.ColonistRelationChanceFactor = 0f;
            request.RelationWithExtraPawnChanceFactor = 0f;
            return PawnGenerator.GeneratePawn(request);
        }
        private static void CopyInheritance(Pawn source, Pawn destination)
        {
            if (source.story != null && source.story.traits != null && destination.story != null && destination.story.traits != null)
            {
                foreach (Trait trait in destination.story.traits.allTraits.ToList()) destination.story.traits.RemoveTrait(trait);
                foreach (Trait trait in source.story.traits.allTraits)
                    if (Rand.Chance(Rand.Range(0.2f, 0.4f))) destination.story.traits.GainTrait(new Trait(trait.def, trait.Degree));
            }
            if (source.skills == null || destination.skills == null) return;
            foreach (SkillRecord sourceSkill in source.skills.skills)
            {
                SkillRecord destinationSkill = destination.skills.GetSkill(sourceSkill.def);
                if (Rand.Chance(Rand.Range(0.2f, 0.4f)))
                {
                    destinationSkill.Level = sourceSkill.Level;
                    destinationSkill.xpSinceLastLevel = sourceSkill.xpSinceLastLevel;
                }
                if (sourceSkill.passion != Passion.None && Rand.Chance(Rand.Range(0.3f, 0.5f))) destinationSkill.passion = sourceSkill.passion;
            }
        }
        private static void ApplyFailureEffects(List<Pawn> participants, bool severe)
        {
            foreach (Pawn participant in participants)
            {
                if (participant == null || participant.Dead || participant.health == null) continue;
                SightstealerUtility.AddHediff(participant, "PsychicComa");
                SightstealerUtility.AddHediff(participant, "SS_RitualBlindness");
                SightstealerUtility.AddHediff(participant, "SS_RitualWound");
                if (severe && participant.mindState != null) participant.mindState.mentalStateHandler.TryStartMentalState(MentalStateDefOf.PanicFlee);
            }
        }
        private static IntVec3 FindSpawnCell(Pawn invoker)
        {
            foreach (IntVec3 cell in GenRadial.RadialCellsAround(invoker.Position, 2f, true))
            {
                if (cell.x < 0 || cell.z < 0 || cell.x >= invoker.Map.Size.x || cell.z >= invoker.Map.Size.z || !cell.Walkable(invoker.Map)) continue;
                if (!cell.GetThingList(invoker.Map).Any(t => t is Pawn)) return cell;
            }
            return invoker.Position;
        }
    }

    public class CompProperties_SSTeleport : CompProperties_AbilityTeleport
    {
        public CompProperties_SSTeleport() { compClass = typeof(CompAbilityEffect_SSTeleport); destination = AbilityEffectDestination.Selected; requiresLineOfSight = true; range = 24.9f; }
    }
    public class CompAbilityEffect_SSTeleport : CompAbilityEffect_Teleport
    {
        public new CompProperties_SSTeleport Props => (CompProperties_SSTeleport)props;
        public override bool CanApplyOn(LocalTargetInfo target, LocalTargetInfo dest) => IsValidDestination(target, false);
        public override bool Valid(LocalTargetInfo target, bool throwMessages = false) => IsValidDestination(target, throwMessages);
        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
        {
            base.Apply(target, dest);
            Pawn pawn = parent.pawn;
            if (pawn != null) parent.StartCooldown(SightstealerUtility.IsDeepDark(pawn) ? 2500 : 4000);
        }
        private bool IsValidDestination(LocalTargetInfo target, bool throwMessages)
        {
            Pawn pawn = parent.pawn;
            if (pawn == null || pawn.Map == null || pawn.Downed || !target.IsValid || target.Cell.x < 0 || target.Cell.z < 0 || target.Cell.x >= pawn.Map.Size.x || target.Cell.z >= pawn.Map.Size.z) return false;
            if (pawn.carryTracker != null && pawn.carryTracker.CarriedThing != null)
            {
                Thing carried = pawn.carryTracker.CarriedThing;
                if (carried.def != null && (carried.def.size.x > 1 || carried.def.size.z > 1)) return false;
            }
            if (!target.Cell.Walkable(pawn.Map) || target.Cell.GetThingList(pawn.Map).Any(t => t is Pawn)) return false;
            float glow = SightstealerUtility.GlowAt(pawn);
            bool deepDark = SightstealerUtility.IsDeepDark(pawn);
            float maxRange = deepDark ? 24.9f : (SightstealerUtility.IsDark(pawn) ? 20.9f : 12.9f);
            if (glow < 0f || pawn.Position.DistanceTo(target.Cell) > maxRange) return false;
            if (glow >= SightstealerUtility.Config.veryBrightGlowThreshold) return false;
            return deepDark || GenSight.LineOfSight(pawn.Position, target.Cell, pawn.Map);
        }
    }
}
