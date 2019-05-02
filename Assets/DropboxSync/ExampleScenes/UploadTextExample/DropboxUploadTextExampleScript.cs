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

public class DropboxUploadTextExampleScript : MonoBehaviour {

	string TEXT_FILE_UPLOAD_PATH = "/DropboxSyncExampleFolder/uploaded_text.txt";

	public Text inputLabelText, outputLabelText;

	public InputField textToUploadInput;
	public Text downloadedText;
	public Button uploadTextButton;

	// Use this for initialization
	void Start () {


		inputLabelText.text = string.Format("Enter text to upload to <b>{0}</b>:", TEXT_FILE_UPLOAD_PATH);
		outputLabelText.text = string.Format("Remote Dropbox file: <b>{0}</b> contents (updated from Dropbox):", TEXT_FILE_UPLOAD_PATH);

		// subscribe to remote file changes
		DropboxSync.Main.GetFile<string>(TEXT_FILE_UPLOAD_PATH, (res) => {
			if(res.error != null){
				if(res.error.ErrorType == DBXErrorType.RemotePathNotFound){
					UpdateDownloadedText("<color=red>File "+TEXT_FILE_UPLOAD_PATH+" doesn't exist on Dropbox.</color> Try uploading new.");
				}else{
					Debug.LogError("Error getting text string: "+res.error.ErrorDescription);
					UpdateDownloadedText("Error: "+res.error.ErrorDescription);
				}
			}else{
				Debug.Log("Received text \""+res.data+"\" from Dropbox!");
				var textStr = res.data;
				UpdateDownloadedText(textStr);
			}
		}, receiveUpdates:true);

		// subscribe to upload button click
		uploadTextButton.onClick.AddListener(UploadTextButtonClicked);		
	}


	public void UploadTextButtonClicked(){
		textToUploadInput.interactable = false;
		uploadTextButton.interactable = false;

		Debug.Log("Upload text "+textToUploadInput.text);

		DropboxSync.Main.UploadFile(TEXT_FILE_UPLOAD_PATH, Encoding.UTF8.GetBytes(textToUploadInput.text), (res) => {
			if(res.error != null){
				Debug.LogError("Error uploading text file: "+res.error.ErrorDescription);
				textToUploadInput.interactable = true;
				uploadTextButton.interactable = true;
			}else{
				Debug.Log("Upload completed");
				textToUploadInput.text = "";
				textToUploadInput.interactable = true;
				uploadTextButton.interactable = true;
			}			
		}, (progress) => {
			Debug.Log("Upload progress: "+progress.ToString());
		});
	}
	
	void UpdateDownloadedText(string desc){
		downloadedText.text = desc;
	}

}
