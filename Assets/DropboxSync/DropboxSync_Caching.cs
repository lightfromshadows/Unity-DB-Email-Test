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

		// CACHING 

		string CacheFolderPathForToken {
			get {
				DropboxSyncUtils.ValidateAccessToken(DropboxAccessToken);

				var accessTokeFirst5Characters = DropboxAccessToken.Substring(0, 5);
				return Path.Combine(_PersistentDataPath, accessTokeFirst5Characters);
			}		
		}

		void DeleteFileFromCache(string dropboxPath){
			Log("DeleteFileFromCache: "+dropboxPath);
			var localFilePath = GetPathInCache(dropboxPath);
			if(File.Exists(localFilePath)){
				File.Delete(localFilePath);
			}		
		}

		void DownloadToCache (string dropboxPath, Action onSuccess, Action<float> onProgress, Action<DBXError> onError){
				Log("DownloadToCache");
				DownloadFileBytes(dropboxPath, (res) => {
					if(res.error != null){
						onError(res.error);						
					}else{
						var localFilePath = GetPathInCache(dropboxPath);
						//Log("Cache folder path: "+CacheFolderPathForToken	);
						//Log("Local cached file path: "+localFilePath);

						// make sure containing directory exists
						var fileDirectoryPath = Path.GetDirectoryName(localFilePath);
						//Log("Local cached directory path: "+fileDirectoryPath);
						Directory.CreateDirectory(fileDirectoryPath);

						File.WriteAllBytes(localFilePath, res.data);						

						// write metadata
						SaveFileMetadata(res.fileMetadata);

						onSuccess();
					}
				}, onProgress: onProgress);
		}

		bool IsFileCached(string dropboxPath){
			var metadata = GetLocalMetadataForFile(dropboxPath);
			var localFilePath = GetPathInCache(dropboxPath);
			if(metadata != null){
				if(File.Exists(localFilePath)){
					return metadata.filesize == new FileInfo(localFilePath).Length;
				}
			}
			return false;
		}

		string GetPathInCache(string dropboxPath){
			var relativeDropboxPath = dropboxPath.Substring(1);			
			if(relativeDropboxPath.Last() == '/'){
				relativeDropboxPath = relativeDropboxPath.Substring(relativeDropboxPath.Length-1);
			}
			var fullPath = Path.Combine(CacheFolderPathForToken, relativeDropboxPath);
			// replace slashes with backslashes if needed
			fullPath = Path.GetFullPath(fullPath);
			return fullPath;
		}	

		string GetMetadataFilePath(string dropboxPath){
			return GetPathInCache(dropboxPath)+".dbxsync";
		}
	}
}
