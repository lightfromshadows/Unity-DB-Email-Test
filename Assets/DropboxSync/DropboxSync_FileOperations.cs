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
        
        private static readonly string MOVE_ENDPOINT = "https://api.dropboxapi.com/2/files/move_v2";
        private static readonly string DELETE_ENDPOINT = "https://api.dropboxapi.com/2/files/delete_v2";
	
		// FILE OPERATIONS

		/// <summary>
		/// Deletes file or folder on Dropbox
		/// </summary>
		/// <param name="dropboxPath">Path to file or folder on Dropbox or inside of Dropbox App folder (depending on accessToken type). Should start with "/". Example:/DropboxSyncExampleFolder/image.jpg</param>
		/// <param name="onResult">Callback function that receives DropboxRequestResult with DBXItem metadata of deleted file or folder</param>
        public void Delete(string dropboxPath, Action<DropboxRequestResult<DBXItem>> onResult){
            var prms = new DropboxDeletePathRequestParams();
            prms.path = dropboxPath;

            MakeDropboxRequest(DELETE_ENDPOINT, prms, (jsonStr) => {

				DBXItem metadata = null;

				try {
					var root = JSON.FromJson<Dictionary<string, object>>(jsonStr);
                    var metadata_dict = root["metadata"] as Dictionary<string, object>;

                    if(metadata_dict[".tag"].ToString() == "file"){
                        metadata = DBXFile.FromDropboxDictionary(metadata_dict);
                    }else if(metadata_dict[".tag"].ToString() == "folder"){ 
                        metadata = DBXFolder.FromDropboxDictionary(metadata_dict);
                    }
					
				}catch(Exception ex){
					_mainThreadQueueRunner.QueueOnMainThread(() => {
						onResult(DropboxRequestResult<DBXItem>.Error(new DBXError(ex.Message, DBXErrorType.ParsingError)));
					});
					return;
				}							
				
				_mainThreadQueueRunner.QueueOnMainThread(() => {
					onResult(new DropboxRequestResult<DBXItem>(metadata));
				});				
			}, onProgress: (progress) => {}, onWebError: (error) => {
				
				_mainThreadQueueRunner.QueueOnMainThread(() => {
					onResult(DropboxRequestResult<DBXItem>.Error(error));
				});
			});

        }

		/// <summary>
		/// Moves file or folder from dropboxFromPath to dropboxToPath
		/// </summary>
		/// <param name="dropboxFromPath">From path</param>
		/// <param name="dropboxToPath">To path</param>
		/// <param name="onResult">Result callback containing metadata of moved object</param>
        public void Move(string dropboxFromPath, string dropboxToPath,
                                 Action<DropboxRequestResult<DBXItem>> onResult) {
			

			var prms = new DropboxMoveFileRequestParams();
			prms.from_path = dropboxFromPath;
            prms.to_path = dropboxToPath;

			MakeDropboxRequest(MOVE_ENDPOINT, prms, (jsonStr) => {

				DBXItem metadata = null;

				try {
					var root = JSON.FromJson<Dictionary<string, object>>(jsonStr);
                    var metadata_dict = root["metadata"] as Dictionary<string, object>;

                    if(metadata_dict[".tag"].ToString() == "file"){
                        metadata = DBXFile.FromDropboxDictionary(metadata_dict);
                    }else if(metadata_dict[".tag"].ToString() == "folder"){ 
                        metadata = DBXFolder.FromDropboxDictionary(metadata_dict);
                    }
					
				}catch(Exception ex){
					_mainThreadQueueRunner.QueueOnMainThread(() => {
						onResult(DropboxRequestResult<DBXItem>.Error(new DBXError(ex.Message, DBXErrorType.ParsingError)));
					});
					return;
				}							
				
				_mainThreadQueueRunner.QueueOnMainThread(() => {
					onResult(new DropboxRequestResult<DBXItem>(metadata));
				});				
			}, onProgress: (progress) => {}, onWebError: (error) => {
				
				_mainThreadQueueRunner.QueueOnMainThread(() => {
                    if(error.ErrorType == DBXErrorType.RemotePathAlreadyExists){
                        error.ErrorDescription = "Can't move file: "+dropboxToPath+" already exists";
                    }
					onResult(DropboxRequestResult<DBXItem>.Error(error));
				});
			});
		}
		
		
	}
}
