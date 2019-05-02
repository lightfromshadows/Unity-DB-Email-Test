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

		private static readonly string LIST_FOLDER_ENDPOINT = "https://api.dropboxapi.com/2/files/list_folder";
		private static readonly string LIST_FOLDER_CONTINUE_ENDPOINT = "https://api.dropboxapi.com/2/files/list_folder/continue";
		private static readonly string CREATE_FOLDER_ENDPOINT = "https://api.dropboxapi.com/2/files/create_folder_v2";

		// FOLDERS
		
		/// <summary>
		/// Checks if dropbox path (file or folder) exists
		/// </summary>
		/// <param name="dropboxPath">Path to file or folder on Dropbox or inside of Dropbox App folder (depending on accessToken type). Should start with "/". Example:/DropboxSyncExampleFolder/image.jpg</param>
		/// <param name="onResult">Result callback containing bool that indicates existance on the item</param>
		public void PathExists(string dropboxPath, Action<DropboxRequestResult<bool>> onResult){
			GetMetadata<DBXFolder>(dropboxPath, (res) => {
				if(res.error != null){
					if(res.error.ErrorType == DBXErrorType.RemotePathNotFound){
						// path not found
						_mainThreadQueueRunner.QueueOnMainThread(() => {
							onResult(new DropboxRequestResult<bool>(false));
						});
					}else{
						// some other error
						_mainThreadQueueRunner.QueueOnMainThread(() => {
							onResult(DropboxRequestResult<bool>.Error(res.error));
						});
					}					
				}else{
					// path exists
					_mainThreadQueueRunner.QueueOnMainThread(() => {
						onResult(new DropboxRequestResult<bool>(true));
					});					
				}
			});

		}

		/// <summary>
		/// Creates folder using path specified
		/// </summary>
		/// <param name="dropboxFolderPath">Path of folder to create</param>
		/// <param name="onResult">Result callback that contains metadata of the created folder</param>
		public void CreateFolder(string dropboxFolderPath, Action<DropboxRequestResult<DBXFolder>> onResult) {
			var path = DropboxSyncUtils.NormalizePath(dropboxFolderPath);

			var prms = new DropboxCreateFolderRequestParams();
			prms.path = path;

			MakeDropboxRequest(CREATE_FOLDER_ENDPOINT, prms, (jsonStr) => {

				DBXFolder folderMetadata = null;

				try {
					var root = JSON.FromJson<Dictionary<string, object>>(jsonStr);
					folderMetadata = DBXFolder.FromDropboxDictionary(root["metadata"] as Dictionary<string, object>);
				}catch(Exception ex){
					_mainThreadQueueRunner.QueueOnMainThread(() => {
						onResult(DropboxRequestResult<DBXFolder>.Error(new DBXError(ex.Message, DBXErrorType.ParsingError)));
					});
					return;
				}							
				
				_mainThreadQueueRunner.QueueOnMainThread(() => {
					onResult(new DropboxRequestResult<DBXFolder>(folderMetadata));
				});				
			}, onProgress: (progress) => {}, onWebError: (error) => {
				if(error.ErrorDescription.Contains("path/conflict/folder")){
					error.ErrorType = DBXErrorType.RemotePathAlreadyExists;
				}
				_mainThreadQueueRunner.QueueOnMainThread(() => {
					onResult(DropboxRequestResult<DBXFolder>.Error(error));
				});
			});
		}

		/// <summary>
		/// Retrieves structure of dropbox folders and files inside specified folder.
		/// </summary>
		/// <param name="dropboxFolderPath">Dropbox folder path</param>
		/// <param name="onResult">Callback function that receives result containing DBXFolder with all child nodes inside.</param>
		/// <param name="onProgress">Callback fnction that receives float from 0 to 1 intdicating the progress.</param>
		public void GetFolderStructure(string dropboxFolderPath, Action<DropboxRequestResult<DBXFolder>> onResult,
						 Action<float> onProgress = null){
			var path = DropboxSyncUtils.NormalizePath(dropboxFolderPath);

			_GetFolderItemsFlat(path, onResult: (items) => {
				DBXFolder rootFolder = null;

				// get root folder
				if(path == "/"){
					rootFolder = new DBXFolder{id="", path="/", name="", items = new List<DBXItem>()};			
				}else{
					rootFolder = items.Where(x => x.path == path).First() as DBXFolder;			
				}
				// squash flat results
				rootFolder = BuildStructureFromFlat(rootFolder, items);

				_mainThreadQueueRunner.QueueOnMainThread(() => {
					onResult(new DropboxRequestResult<DBXFolder>(rootFolder));
				});
			},
			onProgress: (progress) => {
				if(onProgress != null){
					_mainThreadQueueRunner.QueueOnMainThread(() => {					
						onProgress(progress);
					});
				}
			},
			onError: (errorStr) => {
				_mainThreadQueueRunner.QueueOnMainThread(() => {
					onResult(DropboxRequestResult<DBXFolder>.Error(errorStr));
				});
			}, recursive: true);
		}

		/// <summary>
		/// Gets files and folders inside specified folder as a list without structure.
		/// </summary>
		/// <param name="path">Path to folder</param>
		/// <param name="onResult">Vallback function that receives result containing list of DBXItems</param>
		/// <param name="onProgress">Callback fnction that receives float from 0 to 1 intdicating the progress.</param>
		/// <param name="recursive">If True then gets all items recursively.</param>
		public void GetFolderItems(string path, Action<DropboxRequestResult<List<DBXItem>>> onResult, Action<float> onProgress = null, bool recursive = false){
			_GetFolderItemsFlat(path, onResult: (items) => {
				_mainThreadQueueRunner.QueueOnMainThread(() => {
					onResult(new DropboxRequestResult<List<DBXItem>>(items));	
				});				
			},
			onProgress: (progress) => {
				if(onProgress != null){
					_mainThreadQueueRunner.QueueOnMainThread(() => {					
						onProgress(progress);
					});
				}
			},
			onError: (errorStr) => {
				_mainThreadQueueRunner.QueueOnMainThread(() => {
					onResult(DropboxRequestResult<List<DBXItem>>.Error(errorStr));
				});
			}, recursive: recursive);
		}

		DBXFolder BuildStructureFromFlat(DBXFolder rootFolder, List<DBXItem> pool){		
			foreach(var poolItem in pool){
				// if item is immediate child of rootFolder
				if(DropboxSyncUtils.IsPathImmediateChildOfFolder(rootFolder.path, poolItem.path)){
					// add poolItem to folder children
					if(poolItem.type == DBXItemType.Folder){
						//Debug.Log("Build structure recursive");
						rootFolder.items.Add(BuildStructureFromFlat(poolItem as DBXFolder, pool));	
					}else{
						rootFolder.items.Add(poolItem);	
					}				
					//Debug.Log("Added child "+poolItem.path);			
				}
			}

			return rootFolder;
		}

		void _GetFolderItemsFlat(string folderPath, Action<List<DBXItem>> onResult, Action<float> onProgress,
				 Action<DBXError> onError, bool recursive = false, string requestCursor = null, List<DBXItem> currentResults = null){
			folderPath = DropboxSyncUtils.NormalizePath(folderPath);

			if(folderPath == "/"){
				folderPath = ""; // dropbox error fix
			}

			string url;
			DropboxRequestParams prms;
			if(requestCursor == null){
				// first request
				currentResults = new List<DBXItem>();
				url = LIST_FOLDER_ENDPOINT;
				prms = new DropboxListFolderRequestParams{path=folderPath, recursive=recursive};
			}else{
				// have cursor to continue list
				url = LIST_FOLDER_CONTINUE_ENDPOINT;
				prms = new DropboxContinueWithCursorRequestParams(requestCursor);
			}
			
			MakeDropboxRequest(url, prms, onResponse: (jsonStr) => {
				//Log("Got reponse: "+jsonStr);

				Dictionary<string, object> root = null;
				try {
					root = JSON.FromJson<Dictionary<string, object>>(jsonStr);
				}catch(Exception ex){
					onError(new DBXError(ex.Message, DBXErrorType.ParsingError));
					return;
				}

				var entries = root["entries"] as List<object>;
				foreach(Dictionary<string, object> entry in entries){
					if(entry[".tag"].ToString() == "file"){
						currentResults.Add(DBXFile.FromDropboxDictionary(entry));
					}else if(entry[".tag"].ToString() == "folder"){
						currentResults.Add(DBXFolder.FromDropboxDictionary(entry));
					}else{
						onError(new DBXError("Unknown entry tag "+entry[".tag".ToString()], DBXErrorType.Unknown));
						return;
					}
				}

				if((bool)root["has_more"]){
					// recursion
					_GetFolderItemsFlat(folderPath, onResult, onProgress, onError, recursive: recursive,
					requestCursor:root["cursor"].ToString(), 
					currentResults: currentResults);
				}else{
					// done
					onResult(currentResults);
				}

			}, onProgress: onProgress,
				onWebError: (webErrorStr) => {
				//LogError("Got web err: "+webErrorStr);
				onError(webErrorStr);
			});
		}
		
	}
}
