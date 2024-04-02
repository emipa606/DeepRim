using Verse;
using RimWorld;
using DeepRim;
using UnityEngine;

public class Dialog_RenameLayer : Dialog_Rename
{
	private Building_SpawnedLift lift;

	public Dialog_RenameLayer(Building_SpawnedLift lift)
	{
		this.lift = lift;
	}

	public override void SetName(string name)
	{
        lift.parentDrill.UndergroundManager.layerNames[lift.depth] = name;
        if (name != ""){
		    Messages.Message("Deeprim.LayerNameAdded".Translate(name), MessageTypeDefOf.TaskCompletion, historical: false);
        }
        else {
            Messages.Message("Deeprim.LayerNameRemoved".Translate(name), MessageTypeDefOf.TaskCompletion, historical: false);
        }
	}
    public override void DoWindowContents(Rect inRect)
	{
		Text.Font = GameFont.Small;
		bool flag = false;
		if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Return)
		{
			flag = true;
			Event.current.Use();
		}
		GUI.SetNextControlName("RenameField");
		string text = Widgets.TextField(new Rect(0f, 15f, inRect.width, 35f), curName);
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
        if (Widgets.ButtonText(new Rect(15f, inRect.height - 35f - 35f - 5f, inRect.width - 15f - 15f, 35f), "RESET")){
            SetName("");
            Find.WindowStack.TryRemove(this);
        }
		if (!(Widgets.ButtonText(new Rect(15f, inRect.height - 35f - 5f, inRect.width - 15f - 15f, 35f), "OK") || flag))
		{
			return;
		}
		AcceptanceReport acceptanceReport = NameIsValid(curName);
		if (!acceptanceReport.Accepted)
		{
			if (acceptanceReport.Reason.NullOrEmpty())
			{
				Messages.Message("NameIsInvalid".Translate(), MessageTypeDefOf.RejectInput, historical: false);
			}
			else
			{
				Messages.Message(acceptanceReport.Reason, MessageTypeDefOf.RejectInput, historical: false);
			}
		}
		else
		{
			SetName(curName);
			Find.WindowStack.TryRemove(this);
		}
	}
}