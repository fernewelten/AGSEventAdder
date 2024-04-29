using System;
using System.Collections.Generic;
using IO = System.IO;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.Security.AccessControl;
using System.Security;
using System.Windows.Controls;

namespace AGSEventAdder
{
	public class AgsGame 
	{
		/// <summary>
		/// Description of the game
		/// </summary>
		public String Desc { get; private set; }

		/// <summary>
		/// Path to the .agf file
		/// </summary>
		public String Path { get; private set; }
		
		private String LockPath { get; set; }

		/// <summary>
		/// All the rooms in the game
		/// </summary>
		public List<RoomRow> Rooms { get; private set; }

		/// <summary>
		/// The room to work on
		/// </summary>
		public int Room { get; set; }

		public const int kNoRoom = -1;


		private XDocument Tree { get; set; }

		/// <summary>
		/// Initialize the object, when the tree is already loaded
		/// </summary>
		private void Init()
		{
			InitDesc();
			InitRooms();
		}
		
		private void InitDesc()
		{
			List<String> desc_list = [];
			var settings_el = Tree.Root.Element("Game").Element("Settings");
			String game_name =
				(from el in settings_el.Elements("GameName")
				 select el.Value).SingleOrDefault();
			if (!String.IsNullOrEmpty(game_name))
				desc_list.Add(game_name);

			String game_desc =
				(from el in settings_el.Elements("Description")
				 select el.Value).SingleOrDefault();
			if (!String.IsNullOrEmpty(game_desc))
				desc_list.Add(game_desc);

			String game_developer =
				(from el in settings_el.Elements("DeveloperName")
				 select el.Value).SingleOrDefault();
			if (!String.IsNullOrEmpty(game_developer))
				desc_list.Add(game_developer);

			Desc = String.Join(" · ", desc_list.ToArray());
			if (String.IsNullOrEmpty(Desc))
				Desc = "AGS Game";
		}

		private void InitRooms()
		{
			XElement rooms_folder = 
				Tree.Root.Element("Game").Element("Rooms").Element("UnloadedRoomFolder");

			var rooms_list = new List<RoomRow>
			{
				new ()
				{
					Nesting = "",
					Room = kNoRoom,
					Description = "‹ No room ›",
				}
			};

			var subfolders = rooms_folder.Element("SubFolders").Elements();
			foreach (var f in subfolders)
				InitRooms_Folder(
					prefix: "",
					lead_in: "",
					folder: f,
					rlist: ref rooms_list);

			var rooms = rooms_folder.Element("UnloadedRooms").Elements();
			foreach (var rm in rooms)
				InitRooms_Room(
					prefix: "",
					lead_in: "",
					room: rm,
					rlist: ref rooms_list);
			Rooms = rooms_list;
		}

		private void InitRooms_Folder(in String prefix, in String lead_in, in XElement folder, ref List<RoomRow> rlist)
		{
			RoomRow rr = new()
			{
				Room = kNoRoom,
				Nesting = prefix + lead_in,
				Description = (folder?.Attribute("Name")?.Value) ?? "",
			};
			rlist.Add(rr);

			var subfolders = folder.Element("SubFolders").Elements();
			int subfolders_count = subfolders.Count();

			var rooms = folder.Element("UnloadedRooms").Elements();
			var rooms_count = rooms.Count();
			int last_idx = subfolders_count + rooms_count - 1;

			int idx = 0;
			String new_prefix = (lead_in == "") ? "" : NestingStrings.TopDown;
			foreach (var f in subfolders)
			{
				String new_lead_in = (idx == last_idx) ? 
					NestingStrings.TopRight : NestingStrings.TopRightDown;
				InitRooms_Folder(
					prefix: new_prefix,
					lead_in: new_lead_in,
					folder: f,
					rlist: ref rlist);
				idx++;
			}

			foreach (var rm in rooms)
			{
				String new_lead_in = (idx == last_idx) ?
					NestingStrings.TopRight : NestingStrings.TopRightDown;
				InitRooms_Room(
					prefix: new_prefix,
					lead_in: new_lead_in,
					room: rm,
					rlist: ref rlist);
				idx++;
			}
		}

		private void InitRooms_Room(in String prefix, in String lead_in, in XElement room, ref List<RoomRow> rlist)
		{
			RoomRow rr = new()
			{
				Room = Int32.Parse(room.Element("Number").Value),
				Nesting = prefix + lead_in,
				Description = room.Element("Description").Value
			};
			rlist.Add(rr);
		}


		private AgsGame() { }

		public void Unlock()
		{
			if (String.IsNullOrEmpty(LockPath))
				return;

			// This is only a best-efforts try to delete the lock file
			// If it doesn't work out, can't help it. 
			try
			{
				File.Delete(LockPath);
				LockPath = null;
			}
			catch
			{ }
		}

		~AgsGame() 
		{
			Unlock();
		}

		public static void Factory(String agsfilepath, out AgsGame game, out String error_msg)
		{
			game = null;
			error_msg = null;

			// Get directory from path
			FileInfo fi = new(agsfilepath);
			String game_dir = fi.Directory.FullName;
			String lockfilepath = game_dir +
									IO.Path.DirectorySeparatorChar +
									"_OpenInEditor.lock";
			
			XDocument xtree = new ();

			try
			{
				using FileStream fs = File.Open(
					agsfilepath,
					FileMode.Open,
					FileAccess.Read,
					FileShare.None);

				// Must write the lock file before reading the game file
				try
				{
					using FileStream lock_fs = File.Open(
						lockfilepath,
						FileMode.CreateNew,
						FileAccess.Write,
						FileShare.None);
					String message = "Locked by AGS Event Manager\n";
					byte[] buf = new UTF8Encoding(true).GetBytes(message);
					lock_fs.Write(buf, 0, buf.Length);
				}
				catch (IOException)
				{
					error_msg = "Cannot lock game (must not be open, e.g., in AGS Editor)";
					return;

				}
				catch (Exception ex)
				{
					error_msg = "Cannot lock game: " + ex.Message;
					return;
				}

				try
				{
					xtree = XDocument.Load(
						fs,
						LoadOptions.PreserveWhitespace | LoadOptions.SetLineInfo);
				}
				catch(Exception ex) when (ex is InvalidOperationException ||
					                      ex is XmlSyntaxException)
				{
					error_msg = "Error in game file: " + ex.Message;
					return;	
				}
				catch(Exception ex)
				{
					error_msg = "Error reading game file: " + ex.Message;
					return;
				}
			}
			catch (Exception ex)
			{
				error_msg = "Cannot open game file: " + ex.Message;
				return;

			}

			game = new AgsGame
			{
				Tree = xtree,
				Room = kNoRoom,
				Path = agsfilepath,
				LockPath = lockfilepath,
			};
			game.Init();
			error_msg = null;
		}
	}

	public class RoomRow
	{
		public String Nesting { get; set; }
		public int Room { get; set; }
		public String Description { get; set; }
	}
}
