using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using SharedClasses;

namespace SoftwareDebuggingTool
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		private ObservableCollection<AppUser> usersList = new ObservableCollection<AppUser>();
		private System.Windows.WindowState? stateBeforeMinimized = null;

		public MainWindow()
		{
			InitializeComponent();

			usersList.CollectionChanged += usersList_CollectionChanged;
		}

		private bool CanUseFolderNameAsPersonFolder(string foldername)
		{
			if (foldername.Equals("_old", StringComparison.InvariantCultureIgnoreCase))
				return false;
			else if (foldername.Equals("_own", StringComparison.InvariantCultureIgnoreCase))
				return false;
			else
				return true;
		}

		private void Window_Loaded_1(object sender, RoutedEventArgs e)
		{
			if (this.WindowState != System.Windows.WindowState.Minimized)
				stateBeforeMinimized = this.WindowState;

			var folderNames = 
				Directory.GetDirectories(AppUser.cLocalFoldersRoot).Select(dir => Path.GetFileName(dir))
				.Concat(Directory.GetDirectories(AppUser.cSharedFoldersRoot).Select(dir => Path.GetFileName(dir)))
				.Where(fn => CanUseFolderNameAsPersonFolder(fn))
				.OrderBy(s => s)
				.Distinct();
			foreach (var foldername in folderNames)
				usersList.Add(new AppUser(foldername));

			listboxUsers.ItemsSource = usersList;
		}

		private void HideWindow()
		{
			this.Hide();
		}
		private void ShowWindow()
		{
			this.Show();
			if (stateBeforeMinimized.HasValue)
				this.WindowState = stateBeforeMinimized.Value;
			this.Activate();
		}

		private void usersList_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
		{
			trayiconMenuItemUsers.MenuItems.Clear();

			var contextMenuForUser = this.Resources["contextmenuUserItem"] as ContextMenu;
			if (contextMenuForUser == null)
			{
				UserMessages.ShowWarningMessage("Cannot find 'contextmenuUserItem'.");
				return;
			}
			foreach (var user in usersList)
			{
				var userMenu = trayiconMenuItemUsers.MenuItems.Add(user.Name);
				for (int i = 0; i < contextMenuForUser.Items.Count; i++)
				{
					var mi = contextMenuForUser.Items[i] as MenuItem;
					if (mi == null)
					{
						var separator = contextMenuForUser.Items[i] as Separator;
						if (separator != null)
							userMenu.MenuItems.Add("-");
						continue;
					}

					var subItem = userMenu.MenuItems.Add(
						mi.Header.ToString(),
						(sn, ev) =>
						{
							var tmpClickedMenuItem = sn as System.Windows.Forms.MenuItem;
							if (tmpClickedMenuItem != null)
							{
								var wpfMenuItemAndUseritem = tmpClickedMenuItem.Tag as TempMenuitemAndUseritem;
								if (wpfMenuItemAndUseritem != null)
								{
									wpfMenuItemAndUseritem.menuItem.DataContext = wpfMenuItemAndUseritem.userItem;
									wpfMenuItemAndUseritem.menuItem.RaiseEvent(
										new RoutedEventArgs(MenuItem.ClickEvent, wpfMenuItemAndUseritem.userItem));
								}
							}
						});
					subItem.Tag = new TempMenuitemAndUseritem(mi, user);
				}
			}
		}

		private void Window_StateChanged_1(object sender, EventArgs e)
		{
			if (this.WindowState == System.Windows.WindowState.Minimized)
				HideWindow();
			else
				stateBeforeMinimized = this.WindowState;
		}

		private void Border_MouseLeftButtonDown_1(object sender, MouseButtonEventArgs e)
		{
			dockpanelCurrentUser.Visibility = System.Windows.Visibility.Hidden;
			dockpanelCurrentUser.DataContext = null;
			WPFHelper.DoActionIfObtainedItemFromObjectSender<AppUser>(sender,
				user =>
				{
					dockpanelCurrentUser.DataContext = user;
					dockpanelCurrentUser.Visibility = System.Windows.Visibility.Visible;
				});
			//UserMessages.ShowInfoMessage("Border clicked");
		}

		private void menuitemAbout_Click(object sender, RoutedEventArgs e)
		{
			AboutWindow2.ShowAboutWindow(new System.Collections.ObjectModel.ObservableCollection<DisplayItem>()
			{
				new DisplayItem("Author", "Francois Hill"),
				new DisplayItem("Icon(s) obtained from", "http://www.iconarchive.com/show/crystal-clear-icons-by-everaldo.html", "http://www.iconarchive.com/show/crystal-clear-icons-by-everaldo.html")
			});
		}

		private void menuitemExit_Click(object sender, RoutedEventArgs e)
		{
			this.Close();
		}

		private void menuitemExploreToLocalFolder_Click(object sender, RoutedEventArgs e)
		{
			WPFHelper.DoActionIfObtainedItemFromObjectSender<AppUser>(sender,
				user => user.ExploreToLocalFolder());
		}

		private void menuitemCreateLocalFolder_Click(object sender, RoutedEventArgs e)
		{
			WPFHelper.DoActionIfObtainedItemFromObjectSender<AppUser>(sender,
							user => user.CreateLocalFolder());
		}

		private void menuitemExploreToSharedFolder_Click(object sender, RoutedEventArgs e)
		{
			WPFHelper.DoActionIfObtainedItemFromObjectSender<AppUser>(sender,
				user => user.ExploreToSharedFolder());
		}

		private void menuitemCreateSharedFolder_Click(object sender, RoutedEventArgs e)
		{
			WPFHelper.DoActionIfObtainedItemFromObjectSender<AppUser>(sender,
				user => user.CreateSharedFolder());
		}

		private void menuitemCopy32bitRTestDllToShared_Click(object sender, RoutedEventArgs e)
		{
			WPFHelper.DoActionIfObtainedItemFromObjectSender<AppUser>(sender,
				user => user.Copy32bitRTestDllToShared());
		}

		private void menuitemCopy64bitRTestDllToShared_Click(object sender, RoutedEventArgs e)
		{
			WPFHelper.DoActionIfObtainedItemFromObjectSender<AppUser>(sender,
				user => user.Copy64bitRTestDllToShared());
		}

		private void menuitemCopyAppInterfaceXmlTo32bitShared_Click(object sender, RoutedEventArgs e)
		{
			WPFHelper.DoActionIfObtainedItemFromObjectSender<AppUser>(sender,
				   user => user.CopyXmlInterfaceTo32bitShared());
		}

		private void menuitemCopyAppInterfaceXmlTo64bitShared_Click(object sender, RoutedEventArgs e)
		{
			WPFHelper.DoActionIfObtainedItemFromObjectSender<AppUser>(sender,
				   user => user.CopyXmlInterfaceTo64bitShared());
		}

		private void menuitemTidyUpLocalAndSharedFolders_Click(object sender, RoutedEventArgs e)
		{
			WPFHelper.DoActionIfObtainedItemFromObjectSender<AppUser>(sender,
				   user => user.TidyUpLocalAndSharedFoldersNow());
		}

		private void OnNotificationArayIconMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
		{
			if (this.IsVisible)
				HideWindow();
			else
				ShowWindow();
		}

		private void OnMenuItemShowClick(object sender, EventArgs e)
		{
			ShowWindow();
		}

		private void OnMenuItemAboutClick(object sender, EventArgs e)
		{
			AboutWindow2.ShowAboutWindow(new System.Collections.ObjectModel.ObservableCollection<DisplayItem>()
			{
				new DisplayItem("Author", "Francois Hill"),
				new DisplayItem("Icon(s) obtained from", "http://www.iconarchive.com", "http://www.iconarchive.com/show/tulliana-2-icons-by-umut-pulat/bug-icon.html")
			});
		}

		private void OnMenuItemExitClick(object sender, EventArgs e)
		{
			this.Close();
		}

		private void buttonAddUser_Click(object sender, RoutedEventArgs e)
		{
			var newUsername = InputBoxWPF.Prompt("Please enter the user's name.", "Username required");
			if (newUsername == null) return;
			var newApp = new AppUser(newUsername);
			if (UserMessages.Confirm(string.Format("Create the local folder for user '{0}'?", newUsername), "Create local folder"))
				newApp.EnsureLocalFolderExists();
			if (UserMessages.Confirm(string.Format("Create the shared folder for user '{0}'?", newUsername), "Create shared folder"))
				newApp.EnsureSharedFolderExists();
			usersList.Add(newApp);
		}

		private void Button_Click_1(object sender, RoutedEventArgs e)
		{
			UserMessages.ShowInfoMessage("No functionality yet for this button.");
		}
	}

	public class TempMenuitemAndUseritem
	{
		public MenuItem menuItem;
		public AppUser userItem;
		public TempMenuitemAndUseritem(MenuItem menuItem, AppUser userItem)
		{
			this.menuItem = menuItem;
			this.userItem = userItem;
		}
	}
}
