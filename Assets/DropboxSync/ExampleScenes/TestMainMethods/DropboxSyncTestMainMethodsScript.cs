// DropboxSync v2.0
// Created by George Fedoseev 2018

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using DBXSync;
using UnityEngine.UI;
using UnityEngine.Video;
using System.Linq;
using System.Text;
using System.Threading;

public class DropboxSyncTestMainMethodsScript : MonoBehaviour {

	private static readonly string TEST_UPLOAD_DIRECTORY = "/DropboxSyncExampleFolder/DropboxSyncTests";
	private static readonly string TEST_UPLOAD_TXT_FILE = "/DropboxSyncExampleFolder/DropboxSyncTests/test.txt";
	private static readonly string TEST_UPLOAD_TXT_FILE_MOVED = "/DropboxSyncExampleFolder/DropboxSyncTests/test_moved.txt";
	private static readonly string TEST_CREATE_FOLDER = "/DropboxSyncExampleFolder/DropboxSyncTests/test_folder/test/abc";
	private static string EXPECTED_TEXT_CONTENTS = "test me";

	public Text outputText;

	private MainThreadQueueRunner _mainThreadQueueRunner;
	private Thread _backgroundThread;

	private List<Func<string>> _testActions = new List<Func<string>>();

	// Use this for initialization
	void Start () {
		// init DropboxSync from the main thread
		var _ = DropboxSync.Main;

		outputText.text = "";

		_mainThreadQueueRunner = new MainThreadQueueRunner();

		// clean before testing
		_testActions.Add(CleanForTests);

		// add tests - order is important		
		_testActions.Add(TestUploadFile);
		
		_testActions.Add(TestMoveFile);
		_testActions.Add(TestDownloadFile);
		_testActions.Add(TestDeleteFile);
		_testActions.Add(TestCreateNestedFolder);

		

		// clean after testing
		_testActions.Add(CleanForTests);
		

		_backgroundThread = new Thread(RunAllTestsOnBackgroundThreadWorker);
		_backgroundThread.IsBackground = true;
		_backgroundThread.Start();	

				
	}

	void Update(){
		_mainThreadQueueRunner.PerformQueuedTasks();
		
	}

	// METHODS

	void RunAllTestsOnBackgroundThreadWorker(){
		try {
			foreach(var ta in _testActions){
				var error = ta();
				if(error != null){
					LogError(error);
					return;
				}else{
					LogSuccess();
				}
			}

			Log("All tests finished succesfully.");
		}catch(Exception ex){
			Debug.LogException(ex);
		}
		
	}


	// <TESTS

	string CleanForTests(){
		Log("Clean...");
		bool done = false;
		string error = null;

		// remove tests directory
		DropboxSync.Main.Delete(TEST_UPLOAD_DIRECTORY, (res) => {
			if(res.error != null){
				if(res.error.ErrorType == DBXErrorType.RemotePathNotFound){
					// directory doesnt exist - no problem					
				}else{
					error = "Failed to clean: "+res.error.ErrorDescription;
				}				
				done = true;
			}else{
				done = true;
			}
		});

		while(!done){Thread.Sleep(10);}

		return error;
	}

	string TestUploadFile(){
		Log("TestUploadFile...");

		bool done = false;
		string error = null;

		var textToUpload = EXPECTED_TEXT_CONTENTS;
		var bytesToUpload = Encoding.UTF8.GetBytes(textToUpload);
		
		DropboxSync.Main.UploadFile(TEST_UPLOAD_TXT_FILE, bytesToUpload, (res) => {			

			if(res.error != null){
				error = res.error.ErrorDescription;
				done = true;
			}else{				
				done = true;
			}
		});

		while(!done){Thread.Sleep(10);}

		return error;
	}


	

	string TestMoveFile(){
		Log("TestMoveFile...");

		bool done = false;
		string error = null;
		
		DropboxSync.Main.Move(TEST_UPLOAD_TXT_FILE, TEST_UPLOAD_TXT_FILE_MOVED, (res) => {			

			if(res.error != null){
				error = res.error.ErrorDescription;
				done = true;
			}else{
				// check if file exists at new location
				DropboxSync.Main.PathExists(TEST_UPLOAD_TXT_FILE_MOVED, (res_check) => {
					if(res_check.error != null){
						error = "Failed to check if file exists: "+res_check.error.ErrorDescription;
						done = true;
					}else{
						if(res_check.data){
							// file exists
						}else{
							error = "Didn't find moved file.";
						}
						done = true;
					}
				});

				
			}
		});

		while(!done){Thread.Sleep(10);}

		return error;
	}

	string TestDownloadFile(){
		Log("TestDownloadFile...");

		bool done = false;
		string error = null;
		
		DropboxSync.Main.GetFile<string>(TEST_UPLOAD_TXT_FILE_MOVED, (res) => {			

			if(res.error != null){
				error = res.error.ErrorDescription;
				done = true;
			}else{			
				if(res.data != EXPECTED_TEXT_CONTENTS){
					error = "Unexpected file contents.";
				}	
				done = true;
			}
		});

		while(!done){Thread.Sleep(10);}

		return error;
	}

	string TestDeleteFile(){
		Log("TestDeleteFile...");

		bool done = false;
		string error = null;
		
		DropboxSync.Main.Delete(TEST_UPLOAD_TXT_FILE_MOVED, (res) => {			

			if(res.error != null){
				error = res.error.ErrorDescription;
				done = true;
			}else{
				// check if file exists at new location
				DropboxSync.Main.PathExists(TEST_UPLOAD_TXT_FILE_MOVED, (res_check) => {
					if(res_check.error != null){
						error = "Failed to check if file exists: "+res_check.error.ErrorDescription;
						done = true;
					}else{
						if(res_check.data){
							// file exists
							error = "File wasn't removed.";
						}
						done = true;
					}
				});

				
			}
		});

		while(!done){Thread.Sleep(10);}

		return error;
	}

	string TestCreateNestedFolder(){
		Log("TestCreateNestedFolder...");

		bool done = false;
		string error = null;
		
		DropboxSync.Main.CreateFolder(TEST_CREATE_FOLDER, (res) => {			

			if(res.error != null){
				error = res.error.ErrorDescription;
				done = true;
			}else{
				// check if created folder exists
				DropboxSync.Main.PathExists(TEST_CREATE_FOLDER, (res_check) => {
					if(res_check.error != null){
						error = "Failed to check if new folder exists: "+res_check.error.ErrorDescription;
						done = true;
					}else{
						if(res_check.data){
							// folder exists							
						}else{
							error = "New folder not found at location.";
						}
						done = true;
					}
				});

				
			}
		});

		while(!done){Thread.Sleep(10);}

		return error;
	}


	// TESTS>


	// UTILS

	void LogSuccess(string text = null){
		Log("<color=green>Success"+(text != null?":":"")+"</color>" + (text ?? ""));
	}
	
	void LogError(string text){
		Log("<color=red>Failed: </color>"+text);
	}


	void Log(string text){
		_mainThreadQueueRunner.QueueOnMainThread(() => {
			if(outputText.text != ""){
				outputText.text += "\n";
			}
			outputText.text += text;
		});		
	}

	// EVENTS
	void OnDestroy(){
		if(_backgroundThread != null){
			if(_backgroundThread.IsAlive){
				_backgroundThread.Abort();
				_backgroundThread = null;
			}
		}
	}

}
