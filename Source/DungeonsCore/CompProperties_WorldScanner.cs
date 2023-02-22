using RimWorld;
using Verse;

namespace DungeonsCore;

public class CompProperties_WorldScanner : CompProperties
{
    public float scanFindGuaranteedDays;
    public float scanFindMtbDays;
    public JobDef scanJob;
    public StatDef scanSpeedStat;

    public CompProperties_WorldScanner()
    {
        compClass = typeof(CompWorldScanner);
    }
}