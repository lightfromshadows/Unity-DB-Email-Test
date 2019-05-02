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

	public partial class DropboxSync : MonoBehaviour {
		// <CONSTS
		static readonly string DROPBOX_SYNC_VERSION = "2.0";
		static readonly float CHECK_REMOTE_UPDATES_INTERVAL_SECONDS = 7;
		static readonly DropboxSyncLogLevel LOG_LEVEL = DropboxSyncLogLevel.Warnings;
		// CONSTS>


		// SINGLETONE
		private static DropboxSync _instance;
		public static DropboxSync Main {
			get {
				if(_instance == null){
					_instance = FindObjectOfType<DropboxSync>();
					if(_instance != null){						
					}else{
						Debug.LogError("DropboxSync script wasn't found on the scene.");						
					}
				}

				return _instance;				
			}
		}

		// <INSPECTOR
		public string DropboxAccessToken = "<YOUR ACCESS TOKEN>";
		// INSPECTOR>		

		// INTERNET CONNECTION
		InternetConnectionWatcher _internetConnectionWatcher;
		
		// TIMERS
		float _lastTimeCheckedForSubscribedItemsChanges = -999999;

		// WEB CLIENTS
		List<DBXWebClient> _activeWebClientsList = new List<DBXWebClient>();

		// MAIN THREAD
		private MainThreadQueueRunner _mainThreadQueueRunner;

		// OTHER
		string _PersistentDataPath = null;

		// MONOBEHAVIOUR
		void Awake(){
			Initialize();
		}

		void Update () {
			_internetConnectionWatcher.Update();
			_mainThreadQueueRunner.PerformQueuedTasks();
			
			// check remote changes for subscribed
			if(Time.unscaledTime - _lastTimeCheckedForSubscribedItemsChanges > CHECK_REMOTE_UPDATES_INTERVAL_SECONDS){									
				DropboxSyncUtils.IsOnlineAsync((isOnline) => {
					if(isOnline){
						try {
							CheckChangesForSubscribedItems();
						}catch(Exception ex){
							Debug.LogException(ex);
						}						
					}
				});
								
				_lastTimeCheckedForSubscribedItemsChanges = Time.unscaledTime;
			}			
		}

		// METHODS

		void Initialize(){
			
			Debug.Log(string.Format("DropboxSync v{0}", DROPBOX_SYNC_VERSION));

			_PersistentDataPath = Application.persistentDataPath;	

			_internetConnectionWatcher = new InternetConnectionWatcher();
			_internetConnectionWatcher.OnLostInternetConnection += () => {
				LogWarning("Lost internet connection.");
			};
			_internetConnectionWatcher.OnInternetConnectionRecovered += () => {
				LogWarning("Internet connection recovered.");
			};

			_mainThreadQueueRunner = new MainThreadQueueRunner();
			_mainThreadQueueRunner.InitFromMainThread();

			// trust all certificates
			// TODO: do something smarter instead of this
			ServicePointManager.ServerCertificateValidationCallback =
    							((sender, certificate, chain, sslPolicyErrors) => true);	

			ServicePointManager.DefaultConnectionLimit = 20;	
		}

		void OnDestroy(){
			// cancel unfinished downloads/uploads
			foreach(var wc in _activeWebClientsList){
				if(wc != null){
					wc.CancelAsync();
				}				
			}
		}

	} // class
} // namespace
