using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace AgsEventAdder
{
	public class CharacterTableLine
	{
		public string Name { get; set; }
		public int Id { get; set; }

		public ObservableCollection<EventFacts> Facts { get; set; }
		public CharacterTableLine() { }
	}

	public class CharacterTable
	{
		public ObservableCollection<CharacterTableLine> Lines { get; set; }

		public CharacterTable(XDocument tree, EventDescs descs, HashSet<string> global_funcs)
		{
			Lines = [];
			var xfolder = tree.Root.ElementOrThrow("Game")
				.ElementOrThrow("Characters")
				.ElementOrThrow("CharacterFolder")
				.CheckAttributeOrThrow("Name", "Main");
			ProcessCharacterFolder(xfolder, descs.CharacterEvents, global_funcs);
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

				CharacterTableLine ctl = new() { Name = name, Id = id, Facts = [] };
				Lines.Add(ctl);
				var xinteractions = xcharacter.ElementOrThrow("Interactions");
				foreach (XElement xevent in xinteractions.Elements())
				{
					var index = xevent.IntAttributeValueOrThrow("Index");

					while (ctl.Facts.Count <= index)
						ctl.Facts.Add(new EventFacts());
					ctl.Facts[index].CurrentInRoster = xevent.Value;
					ctl.Facts[index].CurrentIsInCode = global_funcs.Contains(xevent.Value);
					ctl.Facts[index].DefaultName = (name == "") ? "" : $"{name}_{descs[index].Ending}";
					ctl.Facts[index].DefaultIsInCode = global_funcs.Contains(ctl.Facts[index].DefaultName);
				}
			}
		}
	}
}
