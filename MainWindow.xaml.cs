using Microsoft.Win32;
using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using static AgsEventAdder.XamlExtensions;

namespace AgsEventAdder
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		public MainWindow()
		{
			InitializeComponent();
			// Set the data context from code behind because WPF will not
			// allow this for 'App' specifically on the XAML side, making
			// up lame excuses.
			DataContext = Application.Current as App;

			_game_path_ofd = new OpenFileDialog()
			{
				AddExtension = true,
				CheckFileExists = true,
				CheckPathExists = true,
				ValidateNames = true,
				DefaultExt = ".ags",
				Filter = "AGS Games (*.agf)|*.agf|All files (*.*)|*.*",
				FilterIndex = 0,
			};

		}

		/// <summary>
		/// For Open File dialogs that ask for a game in_roster_path
		/// </summary>
		private readonly OpenFileDialog _game_path_ofd = null;

		private void GamePathBrowseBtn_Click(object sender, RoutedEventArgs e)
		{
			if (_game_path_ofd.ShowDialog() ?? false)
				GamePathTxt.Text = _game_path_ofd.FileName;
		}

		private void GamePathTxt_TextChanged(object sender, TextChangedEventArgs e)
		{
			if (sender is not TextBox tb || sender is null)
				return;

			// When this element has the focus, assume that the content is still
			// being edited, so no validation here
			if (tb.IsFocused)
				return;

			var new_text = TextBox_LoseKeyboardFocus(tb);
			GamePathTxt_HandleChanged(new_text);
		}

		/// <summary>
		/// Whenever the user has done changing the GamePathTxt field
		/// </summary>
		/// <param name="changed_path">Text that has been entered</param>
		private void GamePathTxt_HandleChanged(string changed_path)
		{
			App app = Application.Current as App;

			if (String.IsNullOrWhiteSpace(changed_path))
				changed_path = "";
			string current_path = app?.AgsGame?.Path ?? "";

			if (changed_path == current_path)
				return;
			if (!CloseAnyOpenGame())
				return;

			if (changed_path == "")
			{
				GamePathErrorTxt.Text = "";
				GameDescBlock.Text = "";
				return;
			}

			AgsGame.Factory(changed_path, out AgsGame game, out string errtext);
			if (game is null)
			{
				if (String.IsNullOrEmpty(errtext))
					errtext = (GamePathErrorTxt?.FindResource("Default") as string) ?? "Error";
				GamePathErrorTxt.Text = errtext;
				GameDescBlock.Text = "";
				return;
			}

			app.AgsGame = game;
			GamePathErrorTxt.Text = "";
			GameDescBlock.Text = game.Desc;

			game.Overview.Root.PropertyChanged += notify_app_about_changes_pending;		
			OverviewTV.ItemsSource = game.Overview.Root.Items;
			int char_grid_fixed_columns = (int) CharacterDGrid.FindResource("FixedColumnCount");
			SetDataGridFactColumns(CharacterDGrid, char_grid_fixed_columns, game.EventDescs.CharacterEvents);

			static void notify_app_about_changes_pending(object? sender, PropertyChangedEventArgs e)
			{
				if (sender is not OverviewCompo oc)
					return;
				if (Application.Current is not App app || app is null)
					return;
				if (e.PropertyName != nameof(oc.ChangesPending))
					return;
				app.ChangesPending = oc.ChangesPending;
			}
		}


		/// <summary>
		/// If a game is open, close it unless the user aborts
		/// </summary>
		/// <returns>
		/// false → user aborts
		/// </returns>
		private bool CloseAnyOpenGame()
		{
			App app = Application.Current as App;
			if (app?.AgsGame is not null)
			{
				if (!ConfirmWhenPending())
					return false; // User has chickened out

				app.AgsGame.Unlock();
				app.AgsGame = null;
			}
			return true;
		}

		private void TextBox_GotKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
		{
			if (sender is not TextBox tb || tb is null)
				return;

			if (tb.Text.Trim() == tb.FindResource("EmptyText") as string)
				tb.Text = string.Empty;
		}

		/// <summary>
		/// Substitute the empty text if the text box is empty.
		/// </summary>
		/// <param name="tb"></param>
		/// <returns>The original content of the textbox</returns>
		private static string TextBox_LoseKeyboardFocus(TextBox tb)
		{
			var new_text = tb.Text.Trim();
			var empty_text = tb.FindResource("EmptyText") as string ?? string.Empty;

			if (string.IsNullOrWhiteSpace(tb.Text))
				tb.Text = empty_text;
			else if (tb.Text == empty_text)
				new_text = string.Empty;
			return new_text;
		}

		private void GamePathTxt_LostFocus(object sender, RoutedEventArgs e)
		{
			if (sender is not TextBox tb || tb is null)
				return;

			var new_text = TextBox_LoseKeyboardFocus(tb);
			GamePathTxt_HandleChanged(new_text);
		}

		private void GamePathTxtReturnBtn_Click(object sender, RoutedEventArgs e)
		{
			GamePathTxt_HandleChanged(GamePathTxt.Text);
		}

		private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
		{
			e.Cancel = !ConfirmWhenPending();
		}

		private void OverviewTV_MouseDoubleClick(object sender, MouseButtonEventArgs e)
		{
			var lvi = sender as ListViewItem;
			if (lvi?.DataContext is not OverviewItem selected)
				return;
			switch (selected.EventCarrier)
			{
				default:
					return;
				case EventCarrier.Characters:
					OverviewTV_Characters_MouseDoubleClick(selected as CharacterTable);
					return;
				case EventCarrier.Guis:
					OverviewTV_Guis_MouseDoubleClick();
					return;
				case EventCarrier.Hotspots:
					{
						int room = GetRoomFromOverviewItem(selected);
						OverviewTV_Hotspots_MouseDoubleClick(room);
						return;
					}
				case EventCarrier.InvItems:
					OverviewTV_InvItems_MouseDoubleClick();
					return;
				case EventCarrier.Objects:
					{
						int room = GetRoomFromOverviewItem(selected);
						OverviewTV_Objects_MouseDoubleClick(room);
						return;
					}
				case EventCarrier.Regions:
					{
						int room = GetRoomFromOverviewItem(selected);
						OverviewTV_Regions_MouseDoubleClick(room);
						return;
					}
				case EventCarrier.Rooms:
					{
						int room = GetRoomFromOverviewItem(selected);
						OverviewTV_Rooms_MouseDoubleClick(room);
						return;
					}
			}
		}

		private static int GetRoomFromOverviewItem(OverviewItem oit)
		{
			for (OverviewCompo compo = oit; compo is not null; compo = compo.Parent)
				if (compo is OverviewRoom)
					return (compo as OverviewRoom).Number;

			return -1;
		}

		private void OverviewTV_Characters_MouseDoubleClick(CharacterTable selected)
		{
			CharacterGrid.Visibility = Visibility.Visible;
			OverviewGrid.Visibility = Visibility.Collapsed;
			CharacterDGrid.ItemsSource = selected.Lines;
			// Stash the informaion about what table is shown in 'Tag' so that we
			// can get at the underlying table when we've got the data grid
			CharacterDGrid.Tag = selected;
		}

		/// <summary>
		/// Dynamically create the columns that pertain to the events in 'desc'
		/// </summary>
		/// <param name="descs"></param>
		/// <exception cref="NotImplementedException"></exception>
		private static void SetDataGridFactColumns(DataGrid dgrid, int fixed_column_count, List<EventDesc> descs)
		{
			var cols = dgrid.Columns;
			while (cols.Count > fixed_column_count)
				cols.RemoveAt(cols.Count - 1);

			var facts_column_cm = dgrid.FindResource("FactsColumnCtxMenu") as ContextMenu;
			var facts_cell_cm = dgrid.FindResource("FactsCellCtxMenu") as ContextMenu;

			Style grey_when_folder_style = DefineGreyWhenFolderStyle();

			for (var desc_idx = 0; desc_idx < descs.Count; desc_idx++)
			{
				var outermost_stckp = new FrameworkElementFactory(typeof(StackPanel));
				outermost_stckp.SetValue(
					StackPanel.OrientationProperty,
					Orientation.Vertical);
				outermost_stckp.SetBinding(
					VisibilityProperty,
					new Binding("IsFolder")
					{
						Mode = BindingMode.OneWay,
						Converter = new CollapsedWhenConverter(),
						ConverterParameter = true,
					});
				var new_compo = MakeComponentForNew(desc_idx, facts_cell_cm);
				outermost_stckp.AppendChild(new_compo);
				var current_compo = MakeComponentForCurrent(desc_idx);
				outermost_stckp.AppendChild(current_compo);
				outermost_stckp.SetValue(
					StackPanel.ContextMenuProperty,
					facts_cell_cm);
				outermost_stckp.SetValue(
					StackPanel.TagProperty,
					desc_idx);

				var dgrid_template = new DataTemplate
				{
					VisualTree = outermost_stckp
				};
				
				// Wrap a StackPanel around the header_stkp text so that this StackPanel
				// can get the column header context menu
				var header_stckp = new StackPanel
				{
					Children = { new TextBlock { Text = descs[desc_idx].Name } },
					ContextMenu = facts_column_cm,
					Tag = desc_idx,
				};
				dgrid.Columns.Add(
					new DataGridTemplateColumn()
					{
						Header = header_stckp,
						CellTemplate = dgrid_template,
						CellStyle = grey_when_folder_style
					});
			}

			Style DefineGreyWhenFolderStyle()
			{
				var grey_when_folder_style = new Style(typeof(DataGridCell));
				var trigger = new DataTrigger
				{
					Binding = new Binding("IsFolder"),
					Value = true,
				};
				trigger.Setters.Add(new Setter(DataGridCell.BackgroundProperty, Brushes.LightGray));
				trigger.Setters.Add(new Setter(DataGridCell.BorderBrushProperty, Brushes.LightGray));
				grey_when_folder_style.Triggers.Add(trigger);
				return grey_when_folder_style;
			}

			FrameworkElementFactory MakeComponentForNew(int desc_idx, ContextMenu main_cm)
			{
				var stackpanel = new FrameworkElementFactory(typeof(StackPanel));
				stackpanel.SetValue(
					StackPanel.OrientationProperty,
					Orientation.Horizontal);
				var in_roster_txtbx = new FrameworkElementFactory(typeof(TextBox));
				stackpanel.AppendChild(in_roster_txtbx);
				var in_roster_txtbx_name = NewRandomXamlName();
				in_roster_txtbx.SetValue(
					TextBox.NameProperty,
					in_roster_txtbx_name);
				var in_roster_txtbx_path = $"Facts[{desc_idx}].NewInRoster";
				in_roster_txtbx.SetValue(
					TextBox.TextProperty,
					new Binding(in_roster_txtbx_path)
					{
						Mode = BindingMode.TwoWay,
						UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged,
					});
				in_roster_txtbx.SetValue(
					TextBox.ContextMenuProperty,
					main_cm);

				var in_code_stackpanel = MakeComponentForInCode(desc_idx, in_roster_txtbx_name);
				stackpanel.AppendChild(in_code_stackpanel);
				
				return stackpanel;
			}

			

			FrameworkElementFactory MakeComponentForInCode(int desc_idx, string in_roster_compo_name)
			{
				var not_in_code_mbinding = new MultiBinding()
				{
					Converter = new HiddenWhenInCodeMConverter(),
				};
				not_in_code_mbinding.Bindings.Add(new Binding($"Facts[{desc_idx}].NewIsInCode"));
				not_in_code_mbinding.Bindings.Add(new Binding($"Facts[{desc_idx}].NewInRoster"));

				var stackpanel = new FrameworkElementFactory(typeof(StackPanel));
				stackpanel.SetValue(
					StackPanel.OrientationProperty,
					Orientation.Horizontal);
				stackpanel.SetBinding(
					Label.VisibilityProperty,
					not_in_code_mbinding);
				var not_in_code_lbl = new FrameworkElementFactory(typeof(Label));
				stackpanel.AppendChild(not_in_code_lbl);
				not_in_code_lbl.SetValue(
					Label.VisibilityProperty,
					new Binding($"Facts[{desc_idx}].MustAddStubToCode")
					{
						Mode = BindingMode.OneWay,
						Converter = new CollapsedWhenConverter(),
						ConverterParameter = true,
					});
				not_in_code_lbl.SetResourceReference(
					Label.ContentProperty,
					"NotInCode");
				var target_binding = new Binding() { ElementName = in_roster_compo_name, };
				not_in_code_lbl.SetValue(
					Label.TargetProperty,
					target_binding);
				var add_to_code_lbl = new FrameworkElementFactory(typeof(Label));
				stackpanel.AppendChild(add_to_code_lbl);
				add_to_code_lbl.SetValue(
					Label.VisibilityProperty,
					new Binding($"Facts[{desc_idx}].MustAddStubToCode")
					{
						Mode = BindingMode.OneWay,
						Converter = new CollapsedWhenConverter(),
						ConverterParameter = false,
					});
				add_to_code_lbl.SetResourceReference(
					Label.ContentProperty,
					"AddToCode");
				add_to_code_lbl.SetValue(
					Label.TargetProperty,
					target_binding);
				return stackpanel;
			}

			FrameworkElementFactory MakeComponentForCurrent(int desc_idx)
			{
				var roster_change_mbinding = new MultiBinding()
				{
					Converter = new CollapsedWhenEqualMConverter(),
				};
				roster_change_mbinding.Bindings.Add(new Binding($"Facts[{desc_idx}].CurrentInRoster"));
				roster_change_mbinding.Bindings.Add(new Binding($"Facts[{desc_idx}].NewInRoster"));
				var in_roster_txtblk = new FrameworkElementFactory(typeof(TextBlock));
				in_roster_txtblk.SetBinding(
					TextBlock.VisibilityProperty,
					roster_change_mbinding);
				in_roster_txtblk.SetValue(
					TextBlock.ForegroundProperty,
					new SolidColorBrush(Color.FromRgb(60, 60, 60)));
				in_roster_txtblk.SetValue(
					TextBlock.TextDecorationsProperty,
					TextDecorations.Strikethrough);
				in_roster_txtblk.SetValue(
					TextBlock.FontStyleProperty,
					FontStyles.Italic);
				in_roster_txtblk.SetValue(
					TextBlock.VerticalAlignmentProperty,
					VerticalAlignment.Center);
				var in_roster_path = $"Facts[{desc_idx}].CurrentInRoster";
				in_roster_txtblk.SetValue(
					TextBlock.TextProperty,
					new Binding() { Path = new PropertyPath(in_roster_path), });
				return in_roster_txtblk;
			}
		}

		private void OverviewTV_Guis_MouseDoubleClick()
		{
			throw new NotImplementedException();
		}

		private void OverviewTV_Hotspots_MouseDoubleClick(in int room)
		{
			throw new NotImplementedException();
		}

		private void OverviewTV_InvItems_MouseDoubleClick()
		{
			throw new NotImplementedException();
		}

		private void OverviewTV_Objects_MouseDoubleClick(in int room)
		{
			throw new NotImplementedException();
		}

		private void OverviewTV_Regions_MouseDoubleClick(in int room)
		{
			throw new NotImplementedException();
		}

		private void OverviewTV_Rooms_MouseDoubleClick(in int room)
		{
			throw new NotImplementedException();
		}

		private void CommitAll_Click(object sender, RoutedEventArgs e)
		{
			if (Application.Current is not App app || app.AgsGame is null)
				return;

			app.AgsGame.UpdatePendingAndSave();
		}

		private void RejectAll_Click(object sender, RoutedEventArgs e)
		{
			throw new NotImplementedException();
		}

		private void BackFromCharactersButton_Click(object sender, RoutedEventArgs e)
		{
			CharacterGrid.Visibility = Visibility.Collapsed;
			OverviewGrid.Visibility = Visibility.Visible;
		}

		private void FactsColumn_AddDefaultEvents_Click(object sender, RoutedEventArgs e)
		{
			bool failed = GetColumnIdentification(sender, out DataGrid data_grid, out TableOverviewItem toi, out int facts_index);
			if (failed)
				return;

			toi.InsertDefaultWhereverEmpty(data_grid.SelectedItems, facts_index, true);
		}

		private void FactsColumn_AddMissingEventsToCode_Click(object sender, RoutedEventArgs e)
		{
			bool failed = GetColumnIdentification(sender, out DataGrid data_grid, out TableOverviewItem toi, out int facts_index);
			if (failed)
				return;

			toi.AddEventsToCodeWheneverMissing(data_grid.SelectedItems, facts_index);
		}

		private void FactsColumn_ClearEventsWithoutCode_Click(object sender, RoutedEventArgs e)
		{
			bool failed = GetColumnIdentification(sender, out DataGrid data_grid, out TableOverviewItem toi, out int facts_index);
			if (failed)
				return;

			toi.ClearEventsWithoutCode(data_grid.SelectedItems, facts_index);
		}

		private void FactsColumn_CancelAllPendingChanges_Click(object sender, RoutedEventArgs e)
		{
			bool failed = GetColumnIdentification(sender, out DataGrid data_grid, out TableOverviewItem toi, out int facts_index);
			if (failed)
				return;

			toi.CancelAllPendingChanges(data_grid.SelectedItems, facts_index);
		}

		private static bool GetColumnIdentification(object sender, out DataGrid data_grid, out TableOverviewItem toi, out int facts_index)
		{
			data_grid = null;
			toi = null;
			facts_index = -1;

			if (sender is not MenuItem mi ||
				mi.Parent is not ContextMenu cm ||
				cm.PlacementTarget is not StackPanel header_stckp ||
				header_stckp is null ||
				header_stckp.Tag is not int facts_index_tag)
				return true;

			data_grid = FindParent<DataGrid>(header_stckp);
			if (data_grid is null ||
				data_grid.Tag is not TableOverviewItem toi_tag)
				return true;
			
			toi = toi_tag;
			facts_index = facts_index_tag;
			return false;
		}

		private void FactsCell_ChangeToDefaultAddStub_Click(object sender, RoutedEventArgs e)
		{
			if (sender is not MenuItem menu_item)
				return;
			EventFacts facts = GetEventFactsFromMenuItem(menu_item);
			if (facts is null)
				return;

			facts.ChangeToDefault();
			facts.AddStubToCode();
		}

		private void FactsCell_ChangeToDefaultEvent_Click(object sender, RoutedEventArgs e)
		{
			if (sender is not MenuItem menu_item)
				return;
			EventFacts facts = GetEventFactsFromMenuItem(menu_item);
			if (facts is null)
				return;

			facts.ChangeToDefault();
		}

		private void FactsCell_ChangeToCurrentEvent_Click(object sender, RoutedEventArgs e)
		{
			if (sender is not MenuItem menu_item)
				return;
			EventFacts facts = GetEventFactsFromMenuItem(menu_item);
			if (facts is null)
				return;

			facts.ChangeToCurrent();
		}

		private void FactsCell_AddMissingStubToCode_Click(object sender, RoutedEventArgs e)
		{
			if (sender is not MenuItem menu_item)
				return;
			EventFacts facts = GetEventFactsFromMenuItem(menu_item);
			if (facts is null)
				return;

			facts.AddStubToCode();
		}

		private void FactsCell_DontAddStubToCode_Click(object sender, RoutedEventArgs e)
		{
			if (sender is not MenuItem menu_item)
				return;
			EventFacts facts = GetEventFactsFromMenuItem(menu_item);
			if (facts is null)
				return;

			facts.DontAddToCode();
		}


		private void FactsCell_ClearEvent_Click(object sender, RoutedEventArgs e)
		{
			if (sender is not MenuItem menu_item)
				return;
			EventFacts facts = GetEventFactsFromMenuItem(menu_item);
			if (facts is null)
				return;

			facts.ClearEvent();
		}

		private void FactsCell_CancelPendingChanges_Click(object sender, RoutedEventArgs e)
		{
			if (sender is not MenuItem menu_item)
				return;
			EventFacts facts = GetEventFactsFromMenuItem(menu_item);
			if (facts is null)
				return;

			facts.CancelPendingChanges();
		}

		private static EventFacts? GetEventFactsFromMenuItem(MenuItem menu_item)
		{
			if (menu_item.Parent is not ContextMenu context_menu)
				return null;
			return GetEventFactsFromContextMenu(context_menu);
		}

		/// <summary>
		/// Customize the context menu that opens when user clicks on a cell
		/// </summary>
		/// <param name="sender">The context menu</param>
		/// <param name="e">(unused)</param>
		private void FactsCellCtxMenu_Opened(object sender, RoutedEventArgs e)
		{
			if (sender is not ContextMenu context_menu)
				return;
			EventFacts facts = GetEventFactsFromContextMenu(context_menu);
			if (facts is null)
				return;

			foreach (var item in context_menu.Items.OfType<MenuItem>())
			{
				// Retrieve the original prototype header from resources
				if (item.TryFindResource("header") is string header && header is not null)
					item.Header = header;
				switch (item.Name)
				{
					case "FactsCellCtxItem_ChangeToDefaultAddStub":
						item.Visibility = Visibility.Visible;
						if (string.IsNullOrEmpty(facts.DefaultName) ||
							facts.NewInRoster.Trim() == facts.DefaultName ||
							facts.NewIsInCode)
							item.Visibility = Visibility.Collapsed;
						item.Header = string.Format(item.Header as string, facts.DefaultName);
						break;

					case "FactsCellCtxItem_ChangeToDefault":
						item.Visibility = Visibility.Visible;
						if (string.IsNullOrEmpty(facts.DefaultName) ||
							facts.NewInRoster.Trim() == facts.DefaultName)
							item.Visibility = Visibility.Collapsed;
						item.Header = string.Format(item.Header as string, facts.DefaultName);
						break;

					case "FactsCellCtxItem_ChangeToCurrent":
						item.Visibility = Visibility.Visible;
						if (string.IsNullOrEmpty(facts.CurrentInRoster) || 
							facts.NewInRoster.Trim() == facts.CurrentInRoster ||
							facts.CurrentInRoster == facts.DefaultName)
							item.Visibility = Visibility.Collapsed;
						item.Header = string.Format(item.Header as string, facts.CurrentInRoster);
						break;

					case "FactsCellCtxItem_AddStub":
						item.Visibility = Visibility.Visible;
						if (string.IsNullOrEmpty(facts.NewInRoster) ||
							facts.NewIsInCode ||
							facts.MustAddStubToCode)
							item.Visibility = Visibility.Collapsed;
						item.Header = string.Format(item.Header as string, facts.NewInRoster);
						break;

					case "FactsCellCtxItem_NoAddStub":
						item.Visibility = Visibility.Visible;
						if (string.IsNullOrEmpty(facts.NewInRoster) ||
							facts.NewIsInCode ||
							!facts.MustAddStubToCode)
							item.Visibility = Visibility.Collapsed;
						item.Header = string.Format(item.Header as string, facts.NewInRoster);
						break;


					case "FactsCellCtxItem_ClearEvent":
						item.Visibility = string.IsNullOrEmpty(facts.NewInRoster) ?
							Visibility.Collapsed : Visibility.Visible;
						break;

					case "FactsCellCtxItem_CancelPendingChanges":
						item.Visibility = facts.HasPendingChanges ?
							Visibility.Visible : Visibility.Collapsed;
						break;
				}
			}
		}

		private static EventFacts? GetEventFactsFromContextMenu(ContextMenu context_menu)
		{
			if (context_menu is null ||
				context_menu.PlacementTarget is not FrameworkElement placement_target ||
				placement_target is null)
				return null;

			if (placement_target.DataContext is not TableLine table_line ||
				table_line is null)
				return null;

			// Need to find the framework element that has the event index
			while(placement_target is not null &&
					(placement_target is not StackPanel ||
					placement_target.Tag is not int))
			{
				placement_target = FindParent<StackPanel>(placement_target);
			}

			var index = placement_target?.Tag;
			if (index == null) 
				return null;
			return table_line.Facts[(int)placement_target.Tag];
		}



		/// <summary>
		/// Gets called whenever user left-clicks on a DataGrid.
		/// Unselects the selected grid line when the click isn't on an interactive element.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void DataGrid_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
		{
			if (sender is not DataGrid dgrid)
				return;

			// Ignore clicks on interactive elements so that those are processed normally.
			if (e.OriginalSource is not DependencyObject clicked_element ||
				IsInteractiveElement(clicked_element))
				return;

			if ((dgrid.SelectionMode == DataGridSelectionMode.Single && dgrid.SelectedItem is null) ||
				dgrid.SelectedItems.Count == 0)
				return;

			dgrid.UnselectAllCells();
			dgrid.UnselectAll();
			_ = dgrid.Focus(); // Unfocus the last selected cell by focusing elsewhere
			e.Handled = true; // Prevent default selection behavior
		}

		/// <summary>
		/// When changes or saves are pending, confirm that the user wants to continue.
		/// </summary>
		/// <returns>true → nothing pending or user wants to continue</returns>
		private bool ConfirmWhenPending()
		{
			if (Application.Current is not App app)
				return true;
			if (!app.SaveIsPending && 0 == app.ChangesPending)
				return true;

			var answer = MessageBox.Show(
				caption: this.FindResource("ConfirmDiscardChanges_Header").ToString(),
				messageBoxText: this.FindResource("ConfirmDiscardChanges_Question").ToString(),
				button: MessageBoxButton.OKCancel,
				icon: MessageBoxImage.Warning);
			return answer != MessageBoxResult.Cancel;
		}
	}
}
