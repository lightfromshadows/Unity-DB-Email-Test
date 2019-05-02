// DropboxSync v2.0
// Created by George Fedoseev 2018

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Text;
using UnityEngine;

using DBXSync.Model;
using DBXSync.Utils;
using UnityEngine.UI;
using System.IO;
using System.Threading;

namespace DBXSync {
	public partial class DropboxSync: MonoBehaviour {

		// SUBSCRIBING TO CHANGES

		
		Dictionary<DBXItem, List<Action<List<DBXFileChange>>>> OnChangeCallbacksDict = new Dictionary<DBXItem, List<Action<List<DBXFileChange>>>>();
		void CheckChangesForSubscribedItems(){
			if(OnChangeCallbacksDict.Count == 0){
				return;
			}

			Log("CheckChangesForSubscribedItems ("+OnChangeCallbacksDict.Count.ToString()+")");
			

			foreach(var kv in OnChangeCallbacksDict){
				var item = kv.Key;
				var callbacks = kv.Value;
				
				switch(item.type){
					case DBXItemType.File:
					FileGetRemoteChanges(item.path, (fileChange) => {
						if(fileChange.changeType != DBXFileChangeType.None){
							foreach(var cb in callbacks){
								cb(new List<DBXFileChange>(){fileChange});
							}
						}						
					}, (error) => {
						if(error.ErrorType != DBXErrorType.UserCancelled){
							LogError("Failed to check file changes: "+error.ErrorDescription);
						}else{
							LogWarning("File changes check cancellled by user");
						}					
					}, saveChangesInfoLocally: true);
					break;
					case DBXItemType.Folder:
					FolderGetRemoteChanges(item.path, (res) => {
						if(res.error == null){
							if(res.data.Count > 0){
								foreach(var cb in callbacks){
									cb(res.data);
								}
							}
								
						}else{
							if(res.error.ErrorType != DBXErrorType.UserCancelled){
								LogError("Failed to check folder changes: "+res.error.ErrorDescription);
							}else{
								LogWarning("Folder changes check canceled by user");
							}
						}
					}, saveChangesInfoLocally: true);
					break;
					default:
					break;
				}
			}
		}

		/// <summary>
		/// Subscribes to file changes on Dropbox.
		/// Callback fires once, when change is being registered and changed file checksum is cached in local metadata.
		/// If change was made not during app runtime, callback fires as soon as app is running and checking for updates.
		/// Update interval can be changed using `DBXChangeForChangesIntervalSeconds` (default values if 5 seconds).
		/// </summary>
		/// <param name="dropboxFilePath">
		///  Path to file on Dropbox or inside Dropbox App (depending on accessToken type).
		///  Should start with "/". Example: /DropboxSyncExampleFolder/image.jpg
		/// </param>
		/// <param name="onChange">
		/// Callback function that receives `DBXFileChange` that contains `changeType` and `DBXFile` (updated file metadata).
		/// </param>
		public void SubscribeToFileChanges(string dropboxFilePath, Action<DBXFileChange> onChange){
			var item = new DBXFile(dropboxFilePath);
			SubscribeToChanges(item, (changes) => {
				onChange(changes[0]);
			});
		}
		
		/// <summary>
		/// Subscribes to file changes on Dropbox in specified folder (and recursively to all subfolders and their files).
		/// Callback fires once, when change is being registered and changed file checksum is cached in local metadata.
		/// If change was made not during app runtime, callback fires as soon as app is running and checking for updates.
		/// Update interval can be changed using `DBXChangeForChangesIntervalSeconds` (default values if 5 seconds).
		/// </summary>
		/// <param name="dropboxFolderPath">
		/// Path to folder on Dropbox or inside Dropbox App (depending on accessToken type). 
		/// Should start with "/". Example: /DropboxSyncExampleFolder
		/// </param>
		/// <param name="onChange">
		/// Callback function that receives list consisting of file changes.
		/// Each file change contains `changeType` and `DBXFile` (updated file metadata).
		/// </param>
		public void SubscribeToFolderChanges(string dropboxFolderPath, Action<List<DBXFileChange>> onChange){
			var item = new DBXFolder(dropboxFolderPath);
			SubscribeToChanges(item, onChange);
		}

		void SubscribeToChanges(DBXItem item, Action<List<DBXFileChange>> onChange){
			if(!OnChangeCallbacksDict.ContainsKey(item)){
				// create new list for callbacks
				OnChangeCallbacksDict.Add(item, new List<Action<List<DBXFileChange>>>());
			}

			OnChangeCallbacksDict[item].Add(onChange);			
		}
			
		/// <summary>
		/// Unsubscribes all subscribers from changes on specified Dropbox path
		/// </summary>
		/// <param name="dropboxPath">Path from which to unsubscribe</param>
		public void UnsubscribeAllFromChangesOnPath(string dropboxPath){
			dropboxPath = DropboxSyncUtils.NormalizePath(dropboxPath);

			var removeKeys = OnChangeCallbacksDict.Where(p => p.Key.path == dropboxPath).Select(p => p.Key).ToList();
			foreach(var k in removeKeys){
				OnChangeCallbacksDict.Remove(k);
			}
		}


		/// <summary>
		/// Unsubscribe specific callback from changes on specified Dropbox path
		/// </summary>
		/// <param name="dropboxPath">Path from which to unsubscribe</param>
		/// <param name="onChange">Callback reference</param>
		public void UnsubscribeFromChangesOnPath(string dropboxPath, Action<List<DBXFileChange>> onChange){
			dropboxPath = DropboxSyncUtils.NormalizePath(dropboxPath);

			var item = OnChangeCallbacksDict.Where(p => p.Key.path == dropboxPath).Select(p => p.Key).FirstOrDefault();
			if(item != null){
				OnChangeCallbacksDict[item].Remove(onChange);
			}
		}
		
	}
}
