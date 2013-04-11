using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using SharedClasses;

namespace SoftwareDebuggingTool
{
	public class AppUser : INotifyPropertyChanged
	{
		private const string cPhotoFileName = "Photo.jpg";
		private const int cInterfaceXmlModifiedMinutesAgo = 30;
		private const int cAppDllModifiedMinutesAgo = 120;
		private const string cTodaysFoldernameDateFormat = "yyyy-MM-dd";
		private const string cRootBinariesDir = @"C:\devKiln\build_albion\tundra-output\win32-baked-rtest-default";

		public static readonly string cApplicationName = SettingsSimple.SoftwareDebuggingTool.Instance.ApplicationName;
		public static readonly int cApplicationVersion = SettingsSimple.SoftwareDebuggingTool.Instance.ApplicationVersion;
		public static readonly string cSharedFoldersRoot = SettingsSimple.SoftwareDebuggingTool.Instance.SharedFoldersRoot;//@"R:\Francois\Debug";
		public static readonly string cLocalFoldersRoot = SettingsSimple.SoftwareDebuggingTool.Instance.LocalFoldersRoot;//@"C:\Programming\Wadiso6\Debug data";

		public string Name { get; private set; }
		public string PhotoFile { get; private set; }
		public bool HasLocalFolder { get { return Directory.Exists(GetLocalFolderPath()); } }
		public bool HasSharedFolder { get { return Directory.Exists(GetSharedFolderPath()); } }

		public AppUser(string Name)
		{
			this.Name = Name;
			this.CheckIfHasPhotoFile();
		}

		private void CheckIfHasPhotoFile()
		{
			string tmpPhotoFile = Path.Combine(cSharedFoldersRoot, this.Name, cPhotoFileName);
			if (File.Exists(tmpPhotoFile))
				this.PhotoFile = tmpPhotoFile;
		}

		private string GetLocalFolderPath() { return Path.Combine(cLocalFoldersRoot, this.Name); }
		private string GetSharedFolderPath() { return Path.Combine(cSharedFoldersRoot, this.Name); }

		private static void _ensureDirectoryOfFileExists(string filepath)
		{
			string dir = Path.GetDirectoryName(filepath);
			if (!Directory.Exists(dir))
				Directory.CreateDirectory(dir);
		}

		private T DoActionCatchException<T>(Func<T> action, T returnValueIfError, string actionDescriptorUserInErrorMessage)
		{
			try
			{
				return action();
			}
			catch (Exception exc)
			{
				UserMessages.ShowErrorMessage(string.Format("{0} for '{1}': {2}{3}{4}{5}",
					actionDescriptorUserInErrorMessage, this.Name, exc.Message, Environment.NewLine, exc.StackTrace));
				return returnValueIfError;
			}
		}
		private void DoActionCatchException(Action action, string actionDescriptorUserInErrorMessage)
		{
			DoActionCatchException<object>(delegate { action(); return null; }, null, actionDescriptorUserInErrorMessage);
		}

		private string GetTodaysDirectoryInLocalFolder()
		{
			return DoActionCatchException<string>(
				delegate
				{
					string dir = Path.Combine(this.GetLocalFolderPath(), DateTime.Now.ToString(cTodaysFoldernameDateFormat));
					if (!Directory.Exists(dir))
						Directory.CreateDirectory(dir);
					return dir;
				},
				null,
				"Error getting today's directory in Local folder");
		}

		private string GetTodaysDirectoryInSharedFolder()
		{
			return DoActionCatchException<string>(
				delegate
				{
					string dir = Path.Combine(this.GetSharedFolderPath(), DateTime.Now.ToString(cTodaysFoldernameDateFormat));
					if (!Directory.Exists(dir))
						Directory.CreateDirectory(dir);
					return dir;
				},
				null,
				"Error getting today's directory in Shared folder");
		}

		private void ExploreTo(string directory)
		{
			ThreadingInterop.PerformOneArgFunctionSeperateThread<string>(
				dir => Process.Start(dir),
				directory,
				false);
			/*ThreadingInterop.PerformOneArgFunctionSeperateThread<string>(
				dir => Process.Start("explorer", "/select,\"" + dir + "\""),
				directory,
				false);*/
		}

		public void ExploreToLocalFolder() { ExploreTo(GetLocalFolderPath()); }

		public void CreateLocalFolder()
		{
			DoActionCatchException(delegate
			{
				Directory.CreateDirectory(this.GetLocalFolderPath());
				OnPropertyChanged(u => u.HasLocalFolder);
			},
			"Create local folder");
		}

		public void ExploreToSharedFolder() { ExploreTo(GetSharedFolderPath()); }

		public void CreateSharedFolder()
		{
			DoActionCatchException(delegate
			{
				Directory.CreateDirectory(this.GetSharedFolderPath());
				OnPropertyChanged(u => u.HasSharedFolder);
			},
			"Create shared folder");
		}

		private void _copyBinaryFileToTodaysSharedFolder(string filenameOnly, string fileIdentifierForUsermessages, Func<string, bool> checkOnSourceFile)
		{
			DoActionCatchException(
				   delegate
				   {
					   string sourceDllPath = Path.Combine(cRootBinariesDir, filenameOnly);
					   if (!File.Exists(sourceDllPath))
						   UserMessages.ShowWarningMessage("File does not exist: " + sourceDllPath);
					   if (!checkOnSourceFile(sourceDllPath))
						   return;

					   string todaysDirectoryInShared = GetTodaysDirectoryInSharedFolder();
					   if (todaysDirectoryInShared == null) return;
					   string destinationFile = Path.Combine(todaysDirectoryInShared, "ProgramFiles", filenameOnly);
					   _ensureDirectoryOfFileExists(destinationFile);
					   File.Copy(sourceDllPath, destinationFile, true);
					   if (UserMessages.Confirm("Successfully copied " + fileIdentifierForUsermessages + " to shared folder of " + this.Name + ", do you want to place the directory path in your clipboard?", "Set clipboard to directory path"))
						   Clipboard.SetText(Path.GetDirectoryName(destinationFile));
				   },
			   "Could not copy DLL");
		}

		public void Copy32bitRTestDllToShared()
		{
			var filenameOnly = string.Format("{0}{1}.dll", cApplicationName, cApplicationVersion);
			_copyBinaryFileToTodaysSharedFolder(
				filenameOnly,
				"32bit RTest DLL",
				(sourceFileToCheck) =>
				{
					var lastModifiedDate = File.GetLastWriteTime(sourceFileToCheck);
					return DateTime.Now.Subtract(lastModifiedDate).TotalMinutes < cAppDllModifiedMinutesAgo
						|| UserMessages.Confirm(string.Format("The file '{0}' has been modified more than {1} minutes ago, do you want to use this file?", sourceFileToCheck, cAppDllModifiedMinutesAgo), "File modified a while ago");
				});
		}

		public void CopyXmlInterfaceToShared()
		{
			var filenameOnly = string.Format("{0}{1}Interface.xml", cApplicationName, cApplicationVersion);
			_copyBinaryFileToTodaysSharedFolder(
				filenameOnly,
				filenameOnly,
				(sourceFileToCheck) =>
				{
					var lastModifiedDate = File.GetLastWriteTime(sourceFileToCheck);
					return DateTime.Now.Subtract(lastModifiedDate).TotalMinutes < cInterfaceXmlModifiedMinutesAgo
						|| UserMessages.Confirm(string.Format("The file '{0}' has been modified more than {1} minutes ago, do you want to use this file?", sourceFileToCheck, cInterfaceXmlModifiedMinutesAgo), "File modified a while ago");
				});
		}

		public event PropertyChangedEventHandler PropertyChanged = delegate { };
		private void OnPropertyChanged(params Expression<Func<AppUser, object>>[] propertiesOrFieldsAsExpressions)
		{
			ReflectionInterop.DoForeachPropertOrField<AppUser>(
				this,
				propertiesOrFieldsAsExpressions,
				(instanceObj, memberInfo, memberValue) =>
				{
					PropertyChanged(instanceObj, new PropertyChangedEventArgs(memberInfo.Name));
				});
		}
	}
}
