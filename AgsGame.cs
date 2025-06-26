using System;
using System.Collections.Generic;
using System.ComponentModel;
using IO = System.IO;
using System.Security;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Xml.Linq;
using System.Xml.XPath;

namespace AgsEventAdder
{
	public class AgsGame : INotifyPropertyChanged
	{
		/// <summary>
		/// Name of the lock file for an AGS game
		/// </summary>
		private const string _lockPathFileName = "_OpenInEditor.lock";

		/// <summary>
		/// Filename of the the global script
		/// </summary>
		private const string _globalCodeFileName = "GlobalScript.asc";

		/// <summary>
		/// Filename of the global definitions
		/// </summary>
		private const string _globalDefinitionsFileName = "GlobalScript.ash";

		/// <summary>
		/// Description of the game
		/// </summary>
		public string Desc
		{
			get => _desc;
			private set
			{
				if (_desc == value)
					return;

				_desc = value;
				OnPropertyChanged(nameof(Desc));
			}
		}
		public string _desc; 

		/// <summary>
		/// Path to the .agf file
		/// </summary>
		public string Path
		{
			get => _path;
			private set
			{
				if (_path == value)
					return;

				_path = value;
				OnPropertyChanged(nameof(Path));
			}
		}
		public string _path;

		/// <summary>
		/// Set when a lock file has been written at this location
		/// </summary>
		private string _lockFilePath;

		
		public Overview Overview { get; private set; }

		public EventDescs EventDescs { get; private set; }

		public bool SaveIsPending { get; private set; } = false;

		public bool UsesNewRoomLocations { get; private set; }

		public string GlobalHeaderLocation { get; private set; }

		public string GlobalCodeLocation { get; private set; }

		public HashSet<string> HeaderFunctions { get; private set; } = [];
		
		public HashSet<string> GlobalFunctions { get; private set; } = [];

		public XDocument Tree { get; set; }

		/// <summary>
		/// Initialize the object, when the tree is already loaded
		/// </summary>
		private void Init()
		{
			InitDesc();
			InitEventDescs();
			InitCodeLocations();
			InitGlobalHeaders();
			InitGlobalFunctions();
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

		private void InitOverview() => Overview = new(this);

		private void InitEventDescs() => EventDescs = new(Tree);

		private void InitCodeLocations()
		{
			GlobalHeaderLocation = IO.Path.Combine(
				IO.Path.GetDirectoryName(Path),
				_globalDefinitionsFileName);
			GlobalCodeLocation = IO.Path.Combine(
				IO.Path.GetDirectoryName(Path),
				_globalCodeFileName);
			UsesNewRoomLocations = IO.Directory.Exists(
				IO.Path.Combine(IO.Path.GetDirectoryName(Path), "Rooms"));
		}

		private void InitGlobalHeaders()
		{
			var reader = new StreamReaderShim(new StreamReader(GlobalHeaderLocation));
			var preprocessor = new Preprocessor(reader);
			var scanner = new Scanner(preprocessor);
			scanner.CollectDeclaredFunctions(HeaderFunctions);
		}

		private void InitGlobalFunctions()
		{
			var reader = new StreamReaderShim(new StreamReader(GlobalCodeLocation));
			var preprocessor = new Preprocessor(reader);
			var scanner = new Scanner(preprocessor);
			scanner.CollectDeclaredFunctions(GlobalFunctions);
		}

		private string GetRoomCodeLocation(int room_number)
		{
			if (UsesNewRoomLocations)
				return IO.Path.Combine(
					IO.Path.GetDirectoryName(Path),
					"Rooms",
					room_number.ToString(),
					$"room{room_number}.asc");
			return IO.Path.Combine(
				IO.Path.GetDirectoryName(Path),
				$"room{room_number}.asc");
		}

		private AgsGame() { }

		public void Unlock()
		{
			if (string.IsNullOrEmpty(_lockFilePath))
				return;

			try
			{
				File.Delete(_lockFilePath);
				_lockFilePath = null;
			}
			catch
			{
				// Rats. Well we tried.
			}
		}

		~AgsGame() 
		{
			// Destructors aren't guaranteed to ever be called, so this is only
			// a last-ditch additional effort to get rid of the lock file
			Unlock();
		}

		public static void Factory(string path, out AgsGame game, out string error_msg)
		{
			game = null;
			error_msg = null;

			string lock_file_path = Path2LockPathName(path);

			XDocument xtree = new();

			
			// Must write the lock file before reading the game file
			try
			{
				using FileStream lock_fs = File.Open(
					lock_file_path,
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
				using FileStream fs = File.Open(
					path,
					FileMode.Open,
					FileAccess.Read,
					FileShare.None);

				xtree = XDocument.Load(
					fs,
					LoadOptions.PreserveWhitespace | LoadOptions.SetLineInfo);
			}
			catch (Exception ex) when (ex is InvalidOperationException or
											 XmlSyntaxException)
			{
				error_msg = "Error in game file: " + ex.Message;
				File.Delete(lock_file_path);
				return;
			}
			catch (Exception ex)
			{
				error_msg = "Error reading game file: " + ex.Message;
				File.Delete(lock_file_path);
				return;
			}

			try
			{
				game = new AgsGame
				{
					Tree = xtree,
					Path = path,
					_lockFilePath = lock_file_path,
				};
				game.Init();
				error_msg = null;
			}
			catch (Exception ex)
			{
				error_msg = "Error when initialising game: " + ex.Message;
				File.Delete(lock_file_path);
				return;
			}
		}

		public void UpdatePendingAndSave()
		{
			// Update pending changes in the tree, write stubs
			update_tree_write_stubs(Overview.Root);

			// Move away the AGS file so that it isn't overwritten
			string old_file = move_away_ags_file();

			// Save the tree to Path
			save_tree(old_file);

			void update_tree_write_stubs(OverviewFolder folder)
			{
				foreach (var item in folder.Items)
				{
					if (item is null)
						continue;

					if (item is OverviewFolder f)
					{
						update_tree_write_stubs(f);
						continue;
					}

					if (item is not TableOverviewItem toi)
						continue;

					toi.UpdateToTreeWhenPending();
					toi.UpdateCodeWhenPending();
				}
			}

			string move_away_ags_file()
			{
				string stem = System.IO.Path.Combine(
					IO.Path.GetDirectoryName(Path),
					IO.Path.GetFileNameWithoutExtension(Path));

				string relocated = Path;
				for (int i = 1; File.Exists(relocated); i++)
					relocated = $"{stem}~{i}.ags";
				try
				{ 
					File.Move(Path, relocated); 
				}
				catch (Exception ex)
				{
					MessageBox.Show(
						$"Couldn't move the original AGS file:\n{ex.Message}\n" +
						"Aborting the save.\n" +
						"You can retry saving your pending changes",
						"Error when moving the original AGS file",
						MessageBoxButton.OK,
						MessageBoxImage.Error);
					// TODO: Keep pending changes up so that another attempt at saving can be made
					SaveIsPending = true;
					return null;
				}
				SaveIsPending = false;
				return relocated;
			}

			void save_tree(string old_file_loc)
			{
				try
				{
					Tree.Save(Path);
				} 
				catch (Exception ex) 
				{
					MessageBox.Show(
						$"Couldn't save the AGS file:\n{ex.Message}\n" +
						"Aborting the save.\n" +
						$"The original file is here:\n{old_file_loc}\n" + 
						$"Rename the file to:\n{IO.Path.GetFileName(Path)}\n" +
						"Or you can retry saving your pending changes",
						"Error saving the AGS file",
						MessageBoxButton.OK,
						MessageBoxImage.Error);
					SaveIsPending = true;
					return;
				}
				SaveIsPending = false;
			}
		}

		private static string Path2LockPathName(string path)
		{
			if (string.IsNullOrEmpty(path))
				return null;
			return IO.Path.Combine(
				IO.Path.GetDirectoryName(path),
				_lockPathFileName);
		}

		/// <summary>
		/// Notify UI elements whenever a property has changed
		/// </summary>
		public event PropertyChangedEventHandler PropertyChanged;

		protected void OnPropertyChanged(string propertyName)
			=> PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
	}
}
