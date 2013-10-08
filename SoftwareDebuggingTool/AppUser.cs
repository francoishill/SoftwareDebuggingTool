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
		private const string cOlderFoldername = "_older";
		private const string cPhotoFileName = "Photo.jpg";
		private const int cInterfaceXmlModifiedMinutesAgo = 30;
		private const int cAppDllModifiedMinutesAgo = 120;
		private const string cTodaysFoldernameDateFormat = "yyyy-MM-dd";
		private const string c32bitSubfolder = @"32bit_ProgramFiles_x86";
		private const string c64bitSubfolder = @"64bit_ProgramFiles";
		private const string cRootBinaries32bitDir = @"C:\devKiln\build_albion\tundra-output\win32-2012-rtest-default";//win32-baked-rtest-default
		private const string cRootBinaries64bitDir = @"C:\devKiln\build_albion\tundra-output\win64-2012-rtest-default";//win64-baked-rtest-default

		public static readonly string cApplicationName = SettingsSimple.SoftwareDebuggingTool.Instance.ApplicationName;
		public static readonly int cApplicationVersion = SettingsSimple.SoftwareDebuggingTool.Instance.ApplicationVersion;
		public static readonly string cSharedFoldersRoot = SettingsSimple.SoftwareDebuggingTool.Instance.SharedFoldersRoot;//@"R:\Francois\Debug";
		public static readonly string cLocalFoldersRoot = SettingsSimple.SoftwareDebuggingTool.Instance.LocalFoldersRoot;//@"C:\Programming\Wadiso6\Debug data";

		private bool _isVisible;
		public bool IsVisible { get { return _isVisible; } set { _isVisible = value; OnPropertyChanged(u => u.IsVisible); } }

		private bool _isSelected;
		public bool IsSelected { get { return _isSelected; } set { _isSelected = value; OnPropertyChanged(u => u.IsSelected); } }

		public string Name { get; private set; }
		public string PhotoFile { get; private set; }
		public bool HasLocalFolder { get { return Directory.Exists(GetLocalFolderPath()); } }
		public bool HasSharedFolder { get { return Directory.Exists(GetSharedFolderPath()); } }

		private AppUser()
		{
			this._isSelected = false;
			this._isVisible = true;
		}

		public AppUser(string Name)
			: this()
		{
			this.Name = Name;
			this.CheckIfHasPhotoFile();
		}

		private void CheckIfHasPhotoFile()
		{
			string tmpPhotoFile = Path.Combine(this.GetSharedFolderPath(), cPhotoFileName);
			if (File.Exists(tmpPhotoFile))
				this.PhotoFile = tmpPhotoFile;
		}

		private string GetLocalFolderPath() { return Path.Combine(cLocalFoldersRoot, this.Name); }
		private string GetSharedFolderPath() { return Path.Combine(cSharedFoldersRoot, this.Name); }

		private static void _ensureDirectoryExists(string dir) { if (!Directory.Exists(dir))Directory.CreateDirectory(dir); }
		private static void _ensureDirectoryOfFileExists(string filepath) { _ensureDirectoryExists(Path.GetDirectoryName(filepath)); }

		private T DoActionCatchException<T>(Func<T> action, T returnValueIfError, string actionDescriptorUserInErrorMessage)
		{
			try
			{
				return action();
			}
			catch (Exception exc)
			{
				UserMessages.ShowErrorMessage(string.Format("{0} for '{1}': {2}{3}{3}{4}",
					actionDescriptorUserInErrorMessage, this.Name, exc.Message, Environment.NewLine, exc.StackTrace));
				return returnValueIfError;
			}
		}
		private void DoActionCatchException(Action action, string actionDescriptorUserInErrorMessage)
		{
			DoActionCatchException<object>(delegate { action(); return null; }, null, actionDescriptorUserInErrorMessage);
		}

		public void EnsureLocalFolderExists()
		{
			DoActionCatchException(delegate { _ensureDirectoryExists(this.GetLocalFolderPath()); }, "Ensure local folder exists");
		}

		public void EnsureSharedFolderExists()
		{
			DoActionCatchException(delegate { _ensureDirectoryExists(this.GetSharedFolderPath()); }, "Ensure shared folder exists");
		}

		private void _moveDirectoryIntoItsSiblingOlderFolder(string siblingOfOtherDirectory)
		{
			var parentDir = Path.GetDirectoryName(siblingOfOtherDirectory);
			var olderDir = Path.Combine(parentDir, cOlderFoldername);
			if (!Directory.Exists(olderDir))
				Directory.CreateDirectory(olderDir);
			var destinationFolder = Path.Combine(olderDir, Path.GetFileName(siblingOfOtherDirectory));
			Directory.Move(
				siblingOfOtherDirectory,
				destinationFolder);
		}

		private void _moveFileIntoItsSiblingOlderFolder(string filepath)
		{
			var parentDir = Path.GetDirectoryName(filepath);
			var olderDir = Path.Combine(parentDir, cOlderFoldername);
			if (!Directory.Exists(olderDir))
				Directory.CreateDirectory(olderDir);
			var destinationFile = Path.Combine(olderDir, Path.GetFileName(filepath));
			File.Move(
				filepath,
				destinationFile);
		}

		private void MoveAllNonTodayFoldersAndFilesInto_older(string todayFolderpath)
		{
			var parentDir = Path.GetDirectoryName(todayFolderpath);

			var directories = Directory.GetDirectories(parentDir);
			foreach (var siblingDir in directories)
			{
				var trimChars = new char[] { '\\', '"', '\'' };
				if (siblingDir.Trim(trimChars).Equals(todayFolderpath.Trim(trimChars), StringComparison.InvariantCultureIgnoreCase))
					continue;
				if (siblingDir.EndsWith("_older", StringComparison.InvariantCultureIgnoreCase))
					continue;
				_moveDirectoryIntoItsSiblingOlderFolder(siblingDir);
			}

			var files = Directory.GetFiles(parentDir);
			foreach (var siblingFile in files)
			{
				_moveFileIntoItsSiblingOlderFolder(siblingFile);
			}
		}

		private string GetTodaysDirectoryInLocalFolder() { return Path.Combine(this.GetLocalFolderPath(), DateTime.Now.ToString(cTodaysFoldernameDateFormat)); }
		private string GetTodaysDirectoryInSharedFolder() { return Path.Combine(this.GetSharedFolderPath(), DateTime.Now.ToString(cTodaysFoldernameDateFormat)); }

		private string CreateAndGetTodaysDirectoryInLocalFolder()
		{
			return DoActionCatchException<string>(
				delegate
				{
					string todayPath = this.GetTodaysDirectoryInLocalFolder();
					if (!Directory.Exists(todayPath))
						Directory.CreateDirectory(todayPath);
					MoveAllNonTodayFoldersAndFilesInto_older(todayPath);
					return todayPath;
				},
				null,
				"Error getting today's directory in Local folder");
		}

		private string CreateAndGetTodaysDirectoryInSharedFolder()
		{
			return DoActionCatchException<string>(
				delegate
				{
					string todayPath = this.GetTodaysDirectoryInSharedFolder();
					if (!Directory.Exists(todayPath))
						Directory.CreateDirectory(todayPath);
					MoveAllNonTodayFoldersAndFilesInto_older(todayPath);
					return todayPath;
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

		public void TidyUpLocalAndSharedFoldersNow()
		{
			DoActionCatchException(delegate
			{
				var localDir = this.GetTodaysDirectoryInLocalFolder();
				MoveAllNonTodayFoldersAndFilesInto_older(localDir);
			},
			"Move non-today folder into _older of local directory");
			DoActionCatchException(delegate
			{
				var sharedDir = this.GetTodaysDirectoryInSharedFolder();
				MoveAllNonTodayFoldersAndFilesInto_older(sharedDir);
			},
			"Move non-today folder into _older of shared directory");
		}

		private void _copyBinaryFileToTodaysSharedFolder(string fullFilePath, string subfolderInTodaysFolder,
			string fileIdentifierForUsermessages, Func<string, bool> checkOnSourceFile, string destinationFolderPathForBatchFile)
		{
			DoActionCatchException(
				   delegate
				   {
					   string sourceDllPath = fullFilePath;
					   if (!File.Exists(sourceDllPath))
						   UserMessages.ShowWarningMessage("File does not exist: " + sourceDllPath);
					   if (!checkOnSourceFile(sourceDllPath))
						   return;

					   string todaysDirectoryInShared = CreateAndGetTodaysDirectoryInSharedFolder();
					   if (todaysDirectoryInShared == null) return;
					   string destinationFile = Path.Combine(todaysDirectoryInShared, subfolderInTodaysFolder, Path.GetFileName(fullFilePath));
					   _ensureDirectoryOfFileExists(destinationFile);
					   File.Copy(sourceDllPath, destinationFile, true);
					   if (!string.IsNullOrWhiteSpace(destinationFolderPathForBatchFile))
					   {
						   string batchFilePath = Path.Combine(Path.GetDirectoryName(destinationFile), "zzzOpenDestinationFolder.bat");
						   File.WriteAllText(batchFilePath, string.Format(@"explorer ""{0}""", destinationFolderPathForBatchFile.Trim('"', '\'')));
					   }
					   if (UserMessages.Confirm("Successfully copied " + fileIdentifierForUsermessages + " to shared folder of " + this.Name + ", do you want to place the directory path in your clipboard?", "Set clipboard to directory path"))
						   Clipboard.SetText(Path.GetDirectoryName(destinationFile));
				   },
			   "Could not copy DLL");
		}

		private string _getApplicationPathInProgramFiles32bit()
		{
			return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "GLS", cApplicationName + cApplicationVersion);
		}

		private string _getApplicationPathInProgramFiles64bit()
		{
			//return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "GLS", cApplicationName + cApplicationVersion);
			return Path.Combine(@"C:\Program Files", "GLS", cApplicationName + cApplicationVersion);
		}

		public void Copy32bitRTestDllToShared()
		{
			var filenameOnly = string.Format("{0}{1}.dll", cApplicationName, cApplicationVersion);
			_copyBinaryFileToTodaysSharedFolder(
				Path.Combine(cRootBinaries32bitDir, filenameOnly),
				c32bitSubfolder,
				"32bit RTest DLL",
				(sourceFileToCheck) =>
				{
					var lastModifiedDate = File.GetLastWriteTime(sourceFileToCheck);
					return DateTime.Now.Subtract(lastModifiedDate).TotalMinutes < cAppDllModifiedMinutesAgo
						|| UserMessages.Confirm(string.Format("The file '{0}' has been modified more than {1} minutes ago, do you want to use this file?", sourceFileToCheck, cAppDllModifiedMinutesAgo), "File modified a while ago");
				},
				_getApplicationPathInProgramFiles32bit());
		}

		public void Copy64bitRTestDllToShared()
		{
			var filenameOnly = string.Format("{0}{1}.dll", cApplicationName, cApplicationVersion);
			_copyBinaryFileToTodaysSharedFolder(
				Path.Combine(cRootBinaries64bitDir, filenameOnly),
				c64bitSubfolder,
				"64bit RTest DLL",
				(sourceFileToCheck) =>
				{
					var lastModifiedDate = File.GetLastWriteTime(sourceFileToCheck);
					return DateTime.Now.Subtract(lastModifiedDate).TotalMinutes < cAppDllModifiedMinutesAgo
						|| UserMessages.Confirm(string.Format("The file '{0}' has been modified more than {1} minutes ago, do you want to use this file?", sourceFileToCheck, cAppDllModifiedMinutesAgo), "File modified a while ago");
				},
				_getApplicationPathInProgramFiles64bit());
		}

		public void CopyXmlInterfaceTo32bitShared()
		{
			var filenameOnly = string.Format("{0}{1}Interface.xml", cApplicationName, cApplicationVersion);
			_copyBinaryFileToTodaysSharedFolder(
				Path.Combine(cRootBinaries32bitDir, filenameOnly),
				c32bitSubfolder,
				filenameOnly,
				(sourceFileToCheck) =>
				{
					var lastModifiedDate = File.GetLastWriteTime(sourceFileToCheck);
					return DateTime.Now.Subtract(lastModifiedDate).TotalMinutes < cInterfaceXmlModifiedMinutesAgo
						|| UserMessages.Confirm(string.Format("The file '{0}' has been modified more than {1} minutes ago, do you want to use this file?", sourceFileToCheck, cInterfaceXmlModifiedMinutesAgo), "File modified a while ago");
				},
				_getApplicationPathInProgramFiles32bit());
		}

		public void CopyXmlInterfaceTo64bitShared()
		{
			var filenameOnly = string.Format("{0}{1}Interface.xml", cApplicationName, cApplicationVersion);
			_copyBinaryFileToTodaysSharedFolder(
				Path.Combine(cRootBinaries32bitDir, filenameOnly),
				c64bitSubfolder,
				filenameOnly,
				(sourceFileToCheck) =>
				{
					var lastModifiedDate = File.GetLastWriteTime(sourceFileToCheck);
					return DateTime.Now.Subtract(lastModifiedDate).TotalMinutes < cInterfaceXmlModifiedMinutesAgo
						|| UserMessages.Confirm(string.Format("The file '{0}' has been modified more than {1} minutes ago, do you want to use this file?", sourceFileToCheck, cInterfaceXmlModifiedMinutesAgo), "File modified a while ago");
				},
				_getApplicationPathInProgramFiles64bit());
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
