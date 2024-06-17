using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace AgsEventAdder
{
	public class Rooms
	{
		public List<Room> List { get; private set; }

		public Rooms(in XDocument tree) 
		{
			XElement folder =
				tree.Root.Element("Game").Element("Rooms").Element("UnloadedRoomFolder");

			var rooms_list = new List<Room>
			{
				new ()
				{
					Nesting = "",
					Id = Room.kNone,
					Description = "‹ No room ›",
				}
			};

			var subfolders = folder.Element("SubFolders").Elements();
			foreach (var f in subfolders)
				InitFolder(
					prefix: "",
					lead_in: "",
					folder: f,
					rlist: ref rooms_list);

			var room_elements = folder.Element("UnloadedRooms").Elements();
			foreach (var rm in room_elements)
				InitRoom(
					prefix: "",
					lead_in: "",
					room: rm,
					rlist: ref rooms_list);
			List = rooms_list;
		}

		private void InitFolder(in String prefix, in String lead_in, in XElement folder, ref List<Room> rlist)
		{
			Room rr = new()
			{
				Id = Room.kNone,
				Nesting = prefix + lead_in,
				Description = (folder?.Attribute("Name")?.Value) ?? "",
			};
			rlist.Add(rr);

			var subfolder_elements = folder.Element("SubFolders").Elements();
			int subfolders_count = subfolder_elements.Count();

			var room_elements = folder.Element("UnloadedRooms").Elements();
			var rooms_count = room_elements.Count();
			int last_idx = subfolders_count + rooms_count - 1;

			int idx = 0;
			String new_prefix = (lead_in == "") ? "" : NestingStrings.TopDown;
			foreach (var sf_el in subfolder_elements)
			{
				String new_lead_in = (idx == last_idx) ?
					NestingStrings.TopRight : NestingStrings.TopRightDown;
				InitFolder(
					prefix: new_prefix,
					lead_in: new_lead_in,
					folder: sf_el,
					rlist: ref rlist);
				idx++;
			}

			foreach (var room_el in room_elements)
			{
				String new_lead_in = (idx == last_idx) ?
					NestingStrings.TopRight : NestingStrings.TopRightDown;
				InitRoom(
					prefix: new_prefix,
					lead_in: new_lead_in,
					room: room_el,
					rlist: ref rlist);
				idx++;
			}
		}

		private void InitRoom(in String prefix, in String lead_in, in XElement room, ref List<Room> rlist)
		{
			Room rr = new()
			{
				Id = Int32.Parse(room.Element("Number").Value),
				Nesting = prefix + lead_in,
				Description = room.Element("Description").Value
			};
			rlist.Add(rr);
		}
	}
}
