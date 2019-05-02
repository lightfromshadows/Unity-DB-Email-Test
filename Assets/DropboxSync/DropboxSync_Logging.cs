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

	public enum DropboxSyncLogLevel {
		Debug = 0,
		Warnings = 1,
		Errors = 2
	}

	public partial class DropboxSync: MonoBehaviour {

		// LOGGING		

		void Log(string message){
			if(LOG_LEVEL <= DropboxSyncLogLevel.Debug)
				Debug.Log("[DropboxSync] "+message);
		}

		void LogWarning(string message){
			if(LOG_LEVEL <= DropboxSyncLogLevel.Warnings)
				Debug.LogWarning("[DropboxSync] "+message);
		}

		void LogError(string message){
			if(LOG_LEVEL <= DropboxSyncLogLevel.Errors)
				Debug.LogError("[DropboxSync] "+message);
		}
		
	}
}
