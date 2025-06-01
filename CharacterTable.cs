using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Shapes;
using System.Xml.Linq;

namespace AgsEventAdder
{
	/// <summary>
	/// Event facts for Characters
	/// </summary>
	/// <param name="parent">The object that contains the table for this line</param>
	public class CharacterTableLine(TableOverviewItem parent) : TableLine(parent)
	{
		public string Name { get; set; }
		public int Id { get; set; }
	}

	/// <summary>
	/// Overview items that contain a table of a list of events per entity
	/// </summary>
	public abstract class TableOverviewItem: OverviewItem
	{
		public TableOverviewItem(EventCarrier carrier, string name, string icon)
			: base(carrier, name, icon)
		{ }

		public abstract void UpdateDiscrepancyCount();

		public abstract void UpdateChangesPending();
	}

	public class CharacterTable : TableOverviewItem
	{
		public ObservableCollection<CharacterTableLine> Lines { get; set; }

		public CharacterTable()
			: base(carrier: EventCarrier.Characters, name: "Character events", icon: "🧑") => Lines = [];

		public override void UpdateDiscrepancyCount() => 
			DiscrepancyCount = Lines.Sum(line => line.DiscrepancyCount);

		public override void UpdateChangesPending() => ChangesPending = Lines.Sum(line => line.ChangesPending);

		public CharacterTable Init(XDocument tree, EventDescs descs, HashSet<string> global_funcs)
		{
			var xfolder = tree.Root.ElementOrThrow("Game")
				.ElementOrThrow("Characters")
				.ElementOrThrow("CharacterFolder")
				.CheckAttributeOrThrow("Name", "Main");
			ProcessCharacterFolder(xfolder, descs.CharacterEvents, global_funcs);
			return this;
		}

		private void ProcessCharacterFolder(
			XElement xfolder, List<EventDesc> descs, HashSet<string> global_funcs)
		{
			var xsubfolders = xfolder.ElementOrThrow("SubFolders");
			foreach (XElement xsubfolder in xsubfolders.Elements())
			{
				if (xsubfolder.Name != "CharacterFolder")
					throw new AgsXmlParsingException(
						$"'Found unexpected sub-element <{xsubfolder.Name}>' within <Subfolders>",
						xsubfolder);
				ProcessCharacterFolder(xsubfolder, descs, global_funcs);
			}

			var xcharacters = xfolder.ElementOrThrow("Characters");
			foreach (XElement xcharacter in xcharacters.Elements())
			{
				if (xcharacter.Name != "Character")
					throw new AgsXmlParsingException(
						$"'Found unexpected sub-element <{xcharacter.Name}>' within <Characters>",
						xcharacter);
				int id = xcharacter.IntElementOrThrow("ID");
				XElement script_name_el = xcharacter.ElementOrThrow("ScriptName");
				string name = script_name_el.Value;
				if (string.IsNullOrWhiteSpace(name))
					name = "";

				CharacterTableLine ctl = new(this) { Name = name, Id = id, Facts = [] };
				Lines.Add(ctl);
				var xinteractions = xcharacter.ElementOrThrow("Interactions");
				foreach (XElement xevent in xinteractions.Elements())
				{
					var index = xevent.IntAttributeValueOrThrow("Index");

					while (ctl.Facts.Count <= index)
					{
						var ev = new EventFacts(parent: ctl, global_funcs: global_funcs);
						ctl.Facts.Add(ev);
					}
					ctl.Facts[index].CurrentInRoster = xevent.Value;
					ctl.Facts[index].DefaultName = (name == "") ? "" : $"{name}_{descs[index].Ending}";
				}
			}
		}
	}
}
