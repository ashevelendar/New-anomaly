using System;
using Verse;

namespace SightstealerColony
{
    public class HediffCompProperties_LightSensitivity : HediffCompProperties
    {
        public HediffCompProperties_LightSensitivity() { compClass = typeof(HediffComp_LightSensitivity); }
    }
    public class HediffComp_LightSensitivity : HediffComp
    {
        private int ticksUntilUpdate;
        public override void CompPostPostAdd(Nullable<DamageInfo> dinfo) { base.CompPostPostAdd(dinfo); UpdateSeverity(); }
        public override void CompPostTickInterval(ref float severityAdjustment, int delta)
        {
            ticksUntilUpdate -= delta;
            if (ticksUntilUpdate > 0) return;
            ticksUntilUpdate = SightstealerUtility.CheckInterval(SightstealerUtility.Config.lightCheckIntervalTicks);
            UpdateSeverity();
        }
        private void UpdateSeverity()
        {
            float glow = SightstealerUtility.GlowAt(Pawn);
            if (glow < 0f) { parent.Severity = 0f; return; }
            SightstealerConfigExtension config = SightstealerUtility.Config;
            if (glow >= config.veryBrightGlowThreshold) parent.Severity = 1f;
            else if (glow >= config.brightGlowThreshold) parent.Severity = 0.5f;
            else parent.Severity = 0f;
        }
    }

    public class HediffCompProperties_DarkRegeneration : HediffCompProperties
    {
        public HediffCompProperties_DarkRegeneration() { compClass = typeof(HediffComp_DarkRegeneration); }
    }
    public class HediffComp_DarkRegeneration : HediffComp
    {
        private const float TicksPerDay = 60000f;
        private int ticksUntilUpdate;
        public override void CompPostPostAdd(Nullable<DamageInfo> dinfo) { base.CompPostPostAdd(dinfo); UpdateAndHeal(0); }
        public override void CompPostTickInterval(ref float severityAdjustment, int delta)
        {
            ticksUntilUpdate -= delta;
            if (ticksUntilUpdate > 0) return;
            int interval = SightstealerUtility.CheckInterval(SightstealerUtility.Config.lightCheckIntervalTicks);
            ticksUntilUpdate = interval;
            UpdateAndHeal(interval);
        }
        private void UpdateAndHeal(int interval)
        {
            float darkness = SightstealerUtility.DarknessAt(Pawn);
            if (darkness < 0f) { parent.Severity = 0f; return; }
            SightstealerConfigExtension config = SightstealerUtility.Config;
            if (darkness >= 1f - config.deepDarkGlowThreshold) parent.Severity = 1f;
            else if (darkness >= 1f - config.darkGlowThreshold) parent.Severity = 0.5f;
            else { parent.Severity = 0f; return; }
            if (interval <= 0 || Pawn == null || Pawn.health == null || Pawn.Dead) return;
            float healingPerDay = darkness >= 1f - config.deepDarkGlowThreshold ? config.deepDarkHealingPerDay : config.darkHealingPerDay;
            float amount = healingPerDay * interval / TicksPerDay;
            if (amount <= 0f) return;
            foreach (Hediff hediff in Pawn.health.hediffSet.GetHediffsTendable())
            {
                Hediff_Injury injury = hediff as Hediff_Injury;
                if (injury != null) injury.Heal(amount);
            }
        }
    }

    public class HediffCompProperties_ConditionalInvisibility : HediffCompProperties
    {
        public HediffCompProperties_ConditionalInvisibility() { compClass = typeof(HediffComp_ConditionalInvisibility); }
    }
    public class HediffComp_ConditionalInvisibility : HediffComp
    {
        private int ticksUntilUpdate;
        private int revealTicksRemaining;
        public override void CompExposeData() { base.CompExposeData(); Scribe_Values.Look(ref revealTicksRemaining, "revealTicksRemaining", 0); }
        public override void CompPostPostAdd(Nullable<DamageInfo> dinfo) { base.CompPostPostAdd(dinfo); UpdateVisibility(); }
        public override void CompPostPostRemoved()
        {
            HediffComp_Invisibility invisibility = parent.GetComp<HediffComp_Invisibility>();
            if (invisibility != null) invisibility.BecomeVisible(true);
            base.CompPostPostRemoved();
        }
        public override void CompPostTickInterval(ref float severityAdjustment, int delta)
        {
            if (revealTicksRemaining > 0)
            {
                revealTicksRemaining -= delta;
                if (revealTicksRemaining < 0) revealTicksRemaining = 0;
            }
            ticksUntilUpdate -= delta;
            if (ticksUntilUpdate > 0) return;
            ticksUntilUpdate = SightstealerUtility.CheckInterval(SightstealerUtility.Config.invisibilityCheckIntervalTicks);
            UpdateVisibility();
        }
        public override void Notify_PawnUsedVerb(Verb verb, LocalTargetInfo target) { base.Notify_PawnUsedVerb(verb, target); Reveal(); }
        public override void Notify_PawnPostApplyDamage(DamageInfo dinfo, float totalDamageDealt)
        {
            base.Notify_PawnPostApplyDamage(dinfo, totalDamageDealt);
            if (totalDamageDealt > 0f) Reveal();
        }
        private void Reveal()
        {
            revealTicksRemaining = Math.Max(revealTicksRemaining, SightstealerUtility.Config.revealDurationTicks);
            HediffComp_Invisibility invisibility = parent.GetComp<HediffComp_Invisibility>();
            if (invisibility != null) invisibility.BecomeVisible(true);
        }
        private void UpdateVisibility()
        {
            HediffComp_Invisibility invisibility = parent.GetComp<HediffComp_Invisibility>();
            if (invisibility == null) return;
            bool canHide = revealTicksRemaining <= 0 && SightstealerUtility.IsDeepDark(Pawn);
            if (canHide) invisibility.BecomeInvisible(false);
            else invisibility.BecomeVisible(false);
        }
    }
}
