using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace DungeonsCore;

public class CompWorldScanner : ThingComp
{
    public static HashSet<CompWorldScanner> worldScanners = new HashSet<CompWorldScanner>();
    protected float daysWorkingSinceLastFinding;

    protected float lastScanTick = -1f;

    protected float lastUserSpeed = 1f;

    public bool scanningToggled;
    public CompProperties_WorldScanner Props => props as CompProperties_WorldScanner;

    public override void PostSpawnSetup(bool respawningAfterLoad)
    {
        base.PostSpawnSetup(respawningAfterLoad);
        worldScanners.Add(this);
    }

    public override string CompInspectStringExtra()
    {
        var text = "";
        if (lastScanTick > Find.TickManager.TicksGame - 30)
        {
            text += "UserScanAbility".Translate() + ": " + lastUserSpeed.ToStringPercent() + "\n" +
                    "ScanAverageInterval".Translate() + ": "
                    + "PeriodDays".Translate((Props.scanFindMtbDays / lastUserSpeed).ToString("F1")) + "\n";
        }

        return text + "ScanningProgressToGuaranteedFind".Translate() + ": " +
               (daysWorkingSinceLastFinding / Props.scanFindGuaranteedDays).ToStringPercent();
    }

    public override void PostDestroy(DestroyMode mode, Map previousMap)
    {
        base.PostDestroy(mode, previousMap);
        worldScanners.Remove(this);
    }

    public override void PostDeSpawn(Map map)
    {
        base.PostDeSpawn(map);
        worldScanners.Remove(this);
    }

    public override IEnumerable<Gizmo> CompGetGizmosExtra()
    {
        if (parent.Faction != Faction.OfPlayer)
        {
            yield break;
        }

        yield return new Command_Toggle
        {
            defaultLabel = "EF.ToggleScanning".Translate(scanningToggled ? "On".Translate() : "Off".Translate()),
            defaultDesc = "EF.ToggleScanningDesc".Translate(),
            icon = parent.def.uiIcon,
            isActive = () => scanningToggled,
            toggleAction = () => { scanningToggled = !scanningToggled; }
        };
        if (Prefs.DevMode)
        {
            yield return new Command_Action
            {
                defaultLabel = "Dev: Force find",
                action = DoFind
            };
        }
    }

    public void Used(Pawn worker)
    {
        lastScanTick = Find.TickManager.TicksGame;
        lastUserSpeed = 1f;
        if (Props.scanSpeedStat != null)
        {
            lastUserSpeed = worker.GetStatValue(Props.scanSpeedStat);
        }

        daysWorkingSinceLastFinding += lastUserSpeed / GenDate.TicksPerDay;
        if (!TickDoesFind(lastUserSpeed))
        {
            return;
        }

        DoFind();
        daysWorkingSinceLastFinding = 0f;
    }

    public bool TickDoesFind(float scanSpeed)
    {
        return Find.TickManager.TicksGame % 59 == 0 &&
               (Rand.MTBEventOccurs(Props.scanFindMtbDays / scanSpeed, GenDate.TicksPerDay, 59f) ||
                Props.scanFindGuaranteedDays > 0f &&
                daysWorkingSinceLastFinding >= Props.scanFindGuaranteedDays);
    }

    public void DoFind()
    {
        var quests =
            DefDatabase<QuestScriptDef>.AllDefs.Where(x => x.defName.Contains("EncounterFramework_QuestPool_"));
        if (!quests.Any())
        {
            return;
        }

        var chosenQuest = quests.RandomElement();
        var quest = QuestUtility.GenerateQuestAndMakeAvailable(chosenQuest, 0);
        if (!quest.hidden)
        {
            QuestUtility.SendLetterQuestAvailable(quest);
        }
    }

    public override void PostExposeData()
    {
        base.PostExposeData();
        Scribe_Values.Look(ref daysWorkingSinceLastFinding, "daysWorkingSinceLastFinding");
        Scribe_Values.Look(ref lastUserSpeed, "lastUserSpeed");
        Scribe_Values.Look(ref lastScanTick, "lastScanTick");
        Scribe_Values.Look(ref scanningToggled, "scanningToggled");
    }
}