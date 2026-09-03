using RimWorld;
using Verse;

namespace SightstealerColony
{
    public class ScenPart_ForceRiftBiome : ScenPart
    {
        public override string Summary(Scenario scenario) => "Starting tile is always the Rift biome.";

        public override void PreMapGenerate()
        {
            base.PreMapGenerate();
            GameInitData gameInitData = Find.GameInitData;
            if (gameInitData == null) return;
            BiomeDef rift = SightstealerUtility.GetDef<BiomeDef>("SS_Rift");
            if (rift != null && gameInitData.startingTile.Tile != null) gameInitData.startingTile.Tile.PrimaryBiome = rift;
        }

        public override void PostMapGenerate(Map map)
        {
            base.PostMapGenerate(map);
            BiomeDef rift = SightstealerUtility.GetDef<BiomeDef>("SS_Rift");
            if (map == null || rift == null) return;
            map.TileInfo.PrimaryBiome = rift;
            GameConditionDef darknessDef = SightstealerUtility.GetDef<GameConditionDef>("UnnaturalDarkness");
            if (darknessDef != null && !map.GameConditionManager.ConditionIsActive(darknessDef))
                map.GameConditionManager.RegisterCondition(GameConditionMaker.MakeConditionPermanent(darknessDef));
            WeatherDef mist = SightstealerUtility.GetDef<WeatherDef>("SS_RiftMist");
            if (mist != null) map.weatherManager.curWeather = mist;
        }
    }
}
