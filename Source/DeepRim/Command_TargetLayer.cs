using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace DeepRim
{
	// Token: 0x0200000A RID: 10
	public class Command_TargetLayer : Command_Action
	{
		// Token: 0x06000039 RID: 57 RVA: 0x00003233 File Offset: 0x00001433
		public override void ProcessInput(Event ev)
		{
			Find.WindowStack.Add(this.MakeMenu());
		}

		// Token: 0x0600003A RID: 58 RVA: 0x00003248 File Offset: 0x00001448
		private FloatMenu MakeMenu()
		{
			List<FloatMenuOption> list = new List<FloatMenuOption>();
			bool flag = this.shaft.curMode != 1;
			if (flag)
			{
				list.Add(new FloatMenuOption("New Layer", delegate()
				{
					this.shaft.drillNew = true;
					this.shaft.PauseDrilling();
				}, MenuOptionPriority.Default, null, null, 0f, null, null));
				using (Dictionary<int, UndergroundMapParent>.Enumerator enumerator = this.manager.layersState.GetEnumerator())
				{
					while (enumerator.MoveNext())
					{
						KeyValuePair<int, UndergroundMapParent> pair = enumerator.Current;
						bool flag2 = pair.Value != null;
						if (flag2)
						{
							list.Add(new FloatMenuOption("Layer at Depth:" + pair.Key + "0m", delegate()
							{
								this.shaft.drillNew = false;
								this.shaft.targetedLevel = pair.Key;
								this.shaft.PauseDrilling();
							}, MenuOptionPriority.Default, null, null, 0f, null, null));
						}
					}
				}
			}
			else
			{
				list.Add(new FloatMenuOption("Can't change target while drilling", null, MenuOptionPriority.Default, null, null, 0f, null, null));
			}
			return new FloatMenu(list);
		}

		// Token: 0x0400001F RID: 31
		public UndergroundManager manager;

		// Token: 0x04000020 RID: 32
		public Building_MiningShaft shaft;
	}
}
