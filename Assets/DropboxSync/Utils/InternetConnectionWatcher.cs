// DropboxSync v2.0
// Created by George Fedoseev 2018

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace DBXSync.Utils {

	public class InternetConnectionWatcher {
		float INTERNET_CONNECTION_CHECK_INTERVAL_SECONDS = 5f;
		
		public Action OnLostInternetConnection = () => {};
		public Action OnInternetConnectionRecovered = () => {};

		bool _wasConnectedWhenCheckedLastTime = true;
		

		private List<Action> _onInternetRecoverOnceCallbacks = new List<Action>();

		float _lastTimeCheckedInternetConnection = -1;

		
		public void Update(){
			if(Time.unscaledTime - _lastTimeCheckedInternetConnection > INTERNET_CONNECTION_CHECK_INTERVAL_SECONDS){
				DropboxSyncUtils.IsOnlineAsync((isOnline) => {
					if(isOnline){
						if(!_wasConnectedWhenCheckedLastTime && _lastTimeCheckedInternetConnection > -1){			

							OnInternetConnectionRecovered();		

							foreach(var a in _onInternetRecoverOnceCallbacks){
								a();
							}
							_onInternetRecoverOnceCallbacks.Clear();
						}

						_wasConnectedWhenCheckedLastTime = true;					
					}else{
						if(_wasConnectedWhenCheckedLastTime && _lastTimeCheckedInternetConnection > -1){
							OnLostInternetConnection();
						}	

						_wasConnectedWhenCheckedLastTime = false;
					}
				});
				_lastTimeCheckedInternetConnection = Time.unscaledTime;
			}
			
		}

		// METHODS

		public void SubscribeToInternetConnectionRecoverOnce(Action a){
			_onInternetRecoverOnceCallbacks.Add(a);

		}
		
	}
}