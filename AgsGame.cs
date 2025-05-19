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
using System.Runtime.CompilerServices;

namespace AgsEventAdder
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

		public Overview Overview { get; private set; }

		private XDocument Tree { get; set; }

		/// <summary>
		/// Initialize the object, when the tree is already loaded
		/// </summary>
		private void Init()
		{
			InitDesc();
			InitOverview();
		}
		
		private void InitDesc()
		{
			List<String> desc_list = [];
			var settings_el = Tree.Root.ElementOrThrow("Game").ElementOrThrow("Settings");

			String game_name = settings_el.Element("GameName").Value;
			if (!String.IsNullOrEmpty(game_name))
				desc_list.Add(game_name);

			String game_desc = settings_el.Element("Description").Value;
			if (!String.IsNullOrEmpty(game_desc))
				desc_list.Add(game_desc);

			String game_developer = settings_el.Element("DeveloperName").Value;
			if (!String.IsNullOrEmpty(game_developer))
				desc_list.Add(game_developer);

			Desc = String.Join(" · ", [.. desc_list]);
			if (String.IsNullOrEmpty(Desc))
				Desc = "AGS Game";
		}

		private void InitOverview() => Overview = new(Tree);


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
					String message = $"Locked by AGS Event Manager on {DateTime.Now:u}\n";
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
				Path = agsfilepath,
				LockPath = lockfilepath,
			};
			game.Init();
			error_msg = null;
		}
	}
}
