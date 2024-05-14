using DeepRim;
using RimWorld;
using UnityEngine;
using Verse;

public class Dialog_RenameLayer : Window
{
    private readonly Building_SpawnedLift lift;
    protected string curName;

    private bool focusedRenameField;

    private int startAcceptingInputAtFrame;

    public Dialog_RenameLayer(Building_SpawnedLift lift)
    {
        forcePause = true;
        doCloseX = true;
        absorbInputAroundWindow = true;
        closeOnAccept = false;
        closeOnClickedOutside = true;
        this.lift = lift;
    }

    private bool AcceptsInput => startAcceptingInputAtFrame <= Time.frameCount;

    protected int MaxNameLength => 28;

    public override Vector2 InitialSize => new Vector2(280f, 175f);

    public void WasOpenedByHotkey()
    {
        startAcceptingInputAtFrame = Time.frameCount + 1;
    }

    protected AcceptanceReport NameIsValid(string name)
    {
        return name.Length != 0;
    }

    public void SetName(string name)
    {
        lift.parentDrill.UndergroundManager.layerNames[lift.depth] = name;
        Messages.Message(
            name != "" ? "Deeprim.LayerNameAdded".Translate(name) : "Deeprim.LayerNameRemoved".Translate(name),
            MessageTypeDefOf.TaskCompletion, false);
    }

    public override void DoWindowContents(Rect inRect)
    {
        Text.Font = GameFont.Small;
        var returnPressed = false;
        if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Return)
        {
            returnPressed = true;
            Event.current.Use();
        }

        GUI.SetNextControlName("RenameField");
        var text = Widgets.TextField(new Rect(0f, 15f, inRect.width, 35f), curName);
        if (AcceptsInput && text.Length < MaxNameLength)
        {
            curName = text;
        }
        else if (!AcceptsInput)
        {
            ((TextEditor)GUIUtility.GetStateObject(typeof(TextEditor), GUIUtility.keyboardControl)).SelectAll();
        }

        if (!focusedRenameField)
        {
            UI.FocusControl("RenameField", this);
            focusedRenameField = true;
        }

        if (Widgets.ButtonText(new Rect(15f, inRect.height - 35f - 35f - 5f, inRect.width - 15f - 15f, 35f), "RESET"))
        {
            SetName("");
            Find.WindowStack.TryRemove(this);
        }

        if (!(Widgets.ButtonText(new Rect(15f, inRect.height - 35f - 5f, inRect.width - 15f - 15f, 35f), "OK") ||
              returnPressed))
        {
            return;
        }

        var acceptanceReport = NameIsValid(curName);
        if (!acceptanceReport.Accepted)
        {
            if (acceptanceReport.Reason.NullOrEmpty())
            {
                Messages.Message("NameIsInvalid".Translate(), MessageTypeDefOf.RejectInput, false);
            }
            else
            {
                Messages.Message(acceptanceReport.Reason, MessageTypeDefOf.RejectInput, false);
            }
        }
        else
        {
            SetName(curName);
            Find.WindowStack.TryRemove(this);
        }
    }
}