using RimWorld;
using Verse;

namespace SightstealerColony
{
    public class Gene_SightstealerPhysiology : Gene
    {
        public override void PostAdd()
        {
            base.PostAdd();
            SightstealerUtility.AddHediff(pawn, "SS_LightStrain");
            SightstealerUtility.AddHediff(pawn, "SS_DarkRegeneration");
        }

        public override void PostRemove()
        {
            SightstealerUtility.RemoveHediff(pawn, "SS_LightStrain");
            SightstealerUtility.RemoveHediff(pawn, "SS_DarkRegeneration");
            base.PostRemove();
        }
    }

    public class Gene_UmbralForm : Gene
    {
        public override void PostAdd()
        {
            base.PostAdd();
            SightstealerUtility.AddHediff(pawn, "SS_UmbralInvisibility");
        }

        public override void PostRemove()
        {
            SightstealerUtility.RemoveHediff(pawn, "SS_UmbralInvisibility");
            base.PostRemove();
        }
    }
}
