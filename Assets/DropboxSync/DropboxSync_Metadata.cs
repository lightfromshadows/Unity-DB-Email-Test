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

		private static readonly string METADATA_ENDPOINT = "https://api.dropboxapi.com/2/files/get_metadata";

		// METADATA

		
		private void GetMetadata<T>(string dropboxPath, Action<DropboxRequestResult<T>> onResult) where T: DBXItem {
			var prms = new DropboxGetMetadataRequestParams(dropboxPath);

			Log("GetMetadata for "+dropboxPath);
			MakeDropboxRequest(METADATA_ENDPOINT, prms, 
			onResponse: (jsonStr) => {
				Log("GetMetadata onResponse");
				var dict = JSON.FromJson<Dictionary<string, object>>(jsonStr);				

				if(typeof(T) == typeof(DBXFolder)){
					var folderMetadata = DBXFolder.FromDropboxDictionary(dict);
					onResult(new DropboxRequestResult<T>(folderMetadata as T));
				}else if(typeof(T) == typeof(DBXFile)){
					var fileMetadata = DBXFile.FromDropboxDictionary(dict);
					onResult(new DropboxRequestResult<T>(fileMetadata as T));
				}
			},
			onProgress:null,
			onWebError: (error) => {
				Log("GetMetadata:onWebError");
				onResult(DropboxRequestResult<T>.Error(error));
			});
		}

		void SaveFileMetadata(DBXFile fileMetadata){		
			
			var localFilePath = GetPathInCache(fileMetadata.path);		
			
			// make sure containing directory exists
			var fileDirectoryPath = Path.GetDirectoryName(localFilePath);
			//Log("Local cached directory path: "+fileDirectoryPath);
			Directory.CreateDirectory(fileDirectoryPath);

			// write metadata to separate file near
			var newMetadataFilePath = GetMetadataFilePath(fileMetadata.path);
			File.WriteAllText(newMetadataFilePath, JsonUtility.ToJson(fileMetadata));
			//Log("Wrote metadata file "+newMetadataFilePath);
		}

		DBXFile GetLocalMetadataForFile(string dropboxFilePath){
			Log("GetLocalMetadataForFile "+dropboxFilePath);
			var metadataFilePath = GetMetadataFilePath(dropboxFilePath);
			Log("Local metadata path: "+metadataFilePath);
			return ParseLocalMetadata(metadataFilePath);
		}

		DBXFile ParseLocalMetadata(string localMetadataPath){
			if(File.Exists(localMetadataPath)){
				// get local content hash
				var fileJsonStr = File.ReadAllText(localMetadataPath);				
				
				try {
					return JsonUtility.FromJson<DBXFile>(fileJsonStr);					
				}catch{
					return null;
				}		
			}
			return null;
		}	
		
	}
}
