// DropboxSync v2.0
// Created by George Fedoseev 2018

using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

public class MainThreadQueueRunner {

	private object _mainThreadQueuedActionsLock = new object();
	private List<Action> _mainThreadQueuedActions = new List<Action>();

	private Thread _unityThread = null;

	public void InitFromMainThread(){
		_unityThread = Thread.CurrentThread;
	}

	
	public void PerformQueuedTasks () {
		// Debug.LogWarning(string.Format("PerformQueuedTasks, isMainThread: {0}", Thread.CurrentThread == _unityThread));
	
		lock(_mainThreadQueuedActionsLock){				
			foreach(var a in _mainThreadQueuedActions){
				if(a != null){
					a();
				}						
			}

			_mainThreadQueuedActions.Clear();
		}
	}

	public void QueueOnMainThread(Action a){
		// Debug.LogWarning(string.Format("QueueOnMainThread, isMainThread: {0}", Thread.CurrentThread == _unityThread));

		if(Thread.CurrentThread == _unityThread){
			a();
			return;
		}

		lock(_mainThreadQueuedActionsLock){
			_mainThreadQueuedActions.Add(a);
		}
	}
}