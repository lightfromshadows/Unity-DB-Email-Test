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

		private static readonly string DOWNLOAD_FILE_ENDPOINT = "https://content.dropboxapi.com/2/files/download";

		// GETTING FILE

		/// <summary>
		/// Asynchronously retrieves file from Dropbox and tries to produce object of specified type T.
		/// </summary>
		/// <param name="dropboxPath">Path to file on Dropbox or inside of Dropbox App folder (depending on accessToken type). Should start with "/". Example: /DropboxSyncExampleFolder/image.jpg</param>
		/// <param name="onResult">Result callback</param>
		/// <param name="onProgress">Callback function that receives progress as float from 0 to 1.</param>
		/// <param name="useCachedFirst">If True then first tries to get data from cache, if not cached then downloads.</param>
		/// <param name="useCachedIfOffline">If True and there's no Internet connection then retrieves file from cache if cached, otherwise produces error.</param>
		/// <param name="receiveUpdates">If True, then when there are remote updates on Dropbox, callback function onResult will be triggered again with updated version of the file.</param>
		public void GetFile<T>(string dropboxPath, Action<DropboxRequestResult<T>> onResult, Action<float> onProgress = null, bool useCachedFirst = false,
			bool useCachedIfOffline = true, bool receiveUpdates = false) where T : class{
			Action<DropboxRequestResult<byte[]>> onResultMiddle = null;

			if(typeof(T) == typeof(string)){
				//Log("GetFile: text type");

				// TEXT DATA
				onResultMiddle = (res) => {		
					if(res.error != null || res.data == null){
						_mainThreadQueueRunner.QueueOnMainThread(() => {
							onResult(DropboxRequestResult<T>.Error(res.error));
						});
					}else{
						_mainThreadQueueRunner.QueueOnMainThread(() => {
							onResult(new DropboxRequestResult<T>(DropboxSyncUtils.GetAutoDetectedEncodingStringFromBytes(res.data) as T));										
						});
					}
				};				
			}
			else if(typeof(T) == typeof(JsonObject) || typeof(T) == typeof(JsonArray)){
				//Log("GetFile: JSON type");

				// JSON OBJECT/ARRAY
				onResultMiddle = (res) => {					
					if(res.error != null){
						_mainThreadQueueRunner.QueueOnMainThread(() => {
							onResult(DropboxRequestResult<T>.Error(res.error));
						});
					}else{
						_mainThreadQueueRunner.QueueOnMainThread(() => {
							onResult(new DropboxRequestResult<T>(JSON.FromJson<T>(
								DropboxSyncUtils.GetAutoDetectedEncodingStringFromBytes(res.data)
							)));
						});
					}
				};	
			}
			else if(typeof(T) == typeof(Texture2D)){
				//Log("GetFile: Texture2D type");
				// IMAGE DATA
				onResultMiddle = (res) => {				
					if(res.error != null){
						_mainThreadQueueRunner.QueueOnMainThread(() => {
							onResult(DropboxRequestResult<T>.Error(res.error));
						});
					}else{
						_mainThreadQueueRunner.QueueOnMainThread(() => {
							onResult(new DropboxRequestResult<T>(DropboxSyncUtils.LoadImageToTexture2D(res.data) as T));
						});
					}
				};	
			}
			else{
				_mainThreadQueueRunner.QueueOnMainThread(() => {
					onResult(DropboxRequestResult<T>.Error(
								new DBXError(string.Format("Dont have a mapping byte[] -> {0}. Type {0} is not supported.", typeof(T).ToString()),
											DBXErrorType.NotSupported
								)
							)
					);
				});
				return;
			}

			GetFileAsBytes(dropboxPath, onResultMiddle, onProgress, useCachedFirst, useCachedIfOffline, receiveUpdates);
		}


		/// <summary>
		/// Asynchronously retrieves file from Dropbox and returns path to local filesystem cached copy.f
		/// </summary>
		/// <param name="dropboxPath">Path to file on Dropbox or inside of Dropbox App folder (depending on accessToken type). Should start with "/". Example: /DropboxSyncExampleFolder/image.jpg</param>
		/// <param name="onResult">Result callback</param>
		/// <param name="onProgress">Callback function that receives progress as float from 0 to 1.</param>
		/// <param name="useCachedFirst">If True then first tries to get data from cache, if not cached then downloads.</param>
		/// <param name="useCachedIfOffline">If True and there's no Internet connection then retrieves file from cache if cached, otherwise produces error.</param>
		/// <param name="receiveUpdates">If True, then when there are remote updates on Dropbox, callback function onResult will be triggered again with updated version of the file.</param>
		public void GetFileAsLocalCachedPath(string dropboxPath, Action<DropboxRequestResult<string>> onResult, Action<float> onProgress = null, bool useCachedFirst = false,
			bool useCachedIfOffline = true, bool receiveUpdates = false){
			Action<DropboxRequestResult<byte[]>> onResultMiddle = (res) => {					
				if(res.error != null){
					_mainThreadQueueRunner.QueueOnMainThread(() => {
						onResult(DropboxRequestResult<string>.Error(res.error));
					});
				}else{
					if(res.data != null){
						_mainThreadQueueRunner.QueueOnMainThread(() => {
							onResult(new DropboxRequestResult<string>(GetPathInCache(dropboxPath)));
						});
					}else{
						_mainThreadQueueRunner.QueueOnMainThread(() => {
							onResult(new DropboxRequestResult<string>(null));
						});
					}					
				}
			};	

			GetFileAsBytes(dropboxPath, onResultMiddle, onProgress, useCachedFirst, useCachedIfOffline, receiveUpdates);
		}

		/// <summary>
		/// Asynchronously retrieves file from Dropbox as byte[]
		/// </summary>
		/// <param name="dropboxPath">Path to file on Dropbox or inside of Dropbox App folder (depending on accessToken type). Should start with "/". Example: /DropboxSyncExampleFolder/image.jpg</param>
		/// <param name="onResult">Result callback</param>
		/// <param name="onProgress">Callback function that receives progress as float from 0 to 1.</param>
		/// <param name="useCachedFirst">If True then first tries to get data from cache, if not cached then downloads.</param>
		/// <param name="useCachedIfOffline">If True and there's no Internet connection then retrieves file from cache if cached, otherwise produces error.</param>
		/// <param name="receiveUpdates">If true , then when there are remote updates on Dropbox, callback function onResult will be triggered again with updated version of the file.</param>
		public void GetFileAsBytes(string dropboxPath, Action<DropboxRequestResult<byte[]>> onResult, Action<float> onProgress = null, bool useCachedFirst = false,
						bool useCachedIfOffline = true, bool receiveUpdates = false){
			if(DropboxSyncUtils.IsBadDropboxPath(dropboxPath)){
				onResult(DropboxRequestResult<byte[]>.Error(
							new DBXError("Cant get file: bad path "+dropboxPath, DBXErrorType.BadRequest)							
						)
				);
				return;
			}

			Action returnCachedResult = () => {
				var cachedFilePath = GetPathInCache(dropboxPath);

				if(File.Exists(cachedFilePath)){
					var bytes = File.ReadAllBytes(cachedFilePath);
					_mainThreadQueueRunner.QueueOnMainThread(() => {
						onResult(new DropboxRequestResult<byte[]>(bytes));
					});
				}else{
					Log("cache doesnt have file");
					_mainThreadQueueRunner.QueueOnMainThread(() => {
						onResult(
							DropboxRequestResult<byte[]>.Error(
								new DBXError("File "+dropboxPath+" is removed on remote", DBXErrorType.RemotePathNotFound)
							)
						);
					});
				}				
			};

			Action subscribeToUpdatesAction = () => {
				SubscribeToFileChanges(dropboxPath, (fileChange) => {					
					UpdateFileFromRemote(dropboxPath, onSuccess: () => {							
						// return updated cached result
						returnCachedResult();
					}, onProgress: (progress) => {
						if(onProgress != null){
							_mainThreadQueueRunner.QueueOnMainThread(() => {					
								onProgress(progress);
							});
						}	
					}, onError: (error) => {
						_mainThreadQueueRunner.QueueOnMainThread(() => {
							onResult(DropboxRequestResult<byte[]>.Error(error));
						});
					});					
				});
			};

			// maybe no need to do any remote requests
			if((useCachedFirst) && IsFileCached(dropboxPath)){	
				Log("GetFile: using cached version");			
				returnCachedResult();

				if(receiveUpdates){
					subscribeToUpdatesAction();
				}
			}else{
				//Log("GetFile: check if online");
				// now check if we online
				
				DropboxSyncUtils.IsOnlineAsync((isOnline) => {
					try {
						if(isOnline){
							Log("GetFile: internet available");
							// check if have updates and load them
							UpdateFileFromRemote(dropboxPath, onSuccess: () => {
								Log("GetFile: state of dropbox file is "+dropboxPath+" is synced now");
								// return updated cached result
								
								returnCachedResult();
								

								if(receiveUpdates){
									subscribeToUpdatesAction();
								}
							}, onProgress: (progress) => {
								if(onProgress != null){
									_mainThreadQueueRunner.QueueOnMainThread(() => {					
										onProgress(progress);
									});
								}
							}, onError: (error) => {
								//Log("error");
								_mainThreadQueueRunner.QueueOnMainThread(() => {
									onResult(DropboxRequestResult<byte[]>.Error(error));
								});

								if(receiveUpdates){
									subscribeToUpdatesAction();
								}
							});
						}else{
							Log("GetFile: internet not available");

							if(useCachedIfOffline && IsFileCached(dropboxPath)){
								Log("GetFile: cannot check for updates - using cached version");
								
								returnCachedResult();
								
								
								if(receiveUpdates){
									subscribeToUpdatesAction();
								}
							}else{
								if(receiveUpdates){
									// try again when internet recovers
									_internetConnectionWatcher.SubscribeToInternetConnectionRecoverOnce(() => {
										GetFileAsBytes(dropboxPath, onResult, onProgress, useCachedFirst, useCachedIfOffline, receiveUpdates);								
									});

									subscribeToUpdatesAction();
								}else{
									// error
									_mainThreadQueueRunner.QueueOnMainThread(() => {
										onResult(DropboxRequestResult<byte[]>.Error(
													new DBXError("GetFile: No internet connection", DBXErrorType.NetworkProblem)
												)
										);	
									});
								}						
							}
						}
					}catch(Exception ex){
						Debug.LogException(ex);
					}
					
				});
				
				
			}

			
		}

		void DownloadFileBytes(string dropboxPath, Action<DropboxFileDownloadRequestResult<byte[]>> onResult, Action<float> onProgress = null){
			var prms = new DropboxDownloadFileRequestParams(dropboxPath);
			MakeDropboxDownloadRequest(DOWNLOAD_FILE_ENDPOINT, prms,
			onResponse: (fileMetadata, data) => {
				onResult(new DropboxFileDownloadRequestResult<byte[]>(data, fileMetadata));
			},
			onProgress: onProgress,
			onWebError: (webErrorStr) => {
				onResult(DropboxFileDownloadRequestResult<byte[]>.Error(webErrorStr));
			});
		}

		



		private void SyncFolderFromDropbox(string dropboxFolderPath, Action onSuccess, Action<float> onProgress, Action<DBXError> onError){
			FolderGetRemoteChanges(dropboxFolderPath, onResult:(res) => {
				if(res.error != null){
					onError(res.error);
				}else{
					var fileChanges = res.data;

					var thread = new Thread(() => {
						var i = 0;
						foreach(DBXFileChange fileChange in fileChanges){
							var finishedCachingFile = false;
							var wasError = false;

							if(fileChange.changeType == DBXFileChangeType.Added || fileChange.changeType == DBXFileChangeType.Modified){
								DownloadToCache(fileChange.file.path, onSuccess: () => {
									finishedCachingFile = true;
								}, 
								onProgress: (progress) => {
									onProgress(((float)i + progress)/fileChanges.Count);
								},
								onError: (errorStr) => {
									onError(errorStr);
									wasError = true;
									finishedCachingFile = true;
								});
							}else if(fileChange.changeType == DBXFileChangeType.Deleted){
								// file deleted on remote
								DeleteFileFromCache(fileChange.file.path);
								finishedCachingFile = true;
							}else{
								// no changes
								finishedCachingFile = true;
							}							

							while(!finishedCachingFile){
								Thread.Sleep(200);
							}

							if(wasError){							
								break;
							}

							i++;
						}

						onSuccess();
					});

					thread.IsBackground = true;
					thread.Start();
					thread.Join();
				}			
			});
		}

		void UpdateFileFromRemote(DBXFile dropboxFile, Action onSuccess, Action<float> onProgress, Action<DBXError> onError){
			UpdateFileFromRemote(dropboxFile.path, onSuccess, onProgress, onError);
		}

		void UpdateFileFromRemote(string dropboxPath, Action onSuccess, Action<float> onProgress, Action<DBXError> onError){
			Log("UpdateFileFromRemote");
			FileGetRemoteChanges(dropboxPath, onResult: (fileChange) => {
				Log("FileGetRemoteChanges:onResult");
				
				if(fileChange.changeType == DBXFileChangeType.Modified || fileChange.changeType == DBXFileChangeType.Added){
					Log("File was created or modified - download new version");
					DownloadToCache(dropboxPath, onSuccess: onSuccess, onProgress: onProgress, onError: onError);
				}else if(fileChange.changeType == DBXFileChangeType.Deleted){
					Log("File was deleted on remote - delete locally from cache");
					DeleteFileFromCache(dropboxPath);
					onSuccess();
				}else{
					// no changes on remote
					Log("No changes on remote");
					if(!GetLocalMetadataForFile(dropboxPath).deletedOnRemote){
						// check if file actually downloaded, not only metadata
						if(IsFileCached(dropboxPath)){
							onSuccess();
						}else{
							DownloadToCache(dropboxPath, onSuccess: onSuccess, onProgress: onProgress, onError: onError);
						}
					}else{
						DeleteFileFromCache(dropboxPath);
						// no changes, file is deleted locally and on remote - synced
						Log("no changes, file is deleted locally and on remote - synced");
						onSuccess();
					}									
				}
			}, onError: onError, saveChangesInfoLocally:true);									
		}
		
	}
}
