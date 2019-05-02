// DropboxSync v2.0
// Created by George Fedoseev 2018

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using DBXSync;
using UnityEngine.UI;
using System.Linq;
using System.Text;
using System.IO;

public class DropboxUploadFileExampleScript : MonoBehaviour {

	public InputField localFileInput;
	public Button uploadButton;
	public Text statusText;

	void Start(){
		localFileInput.onValueChanged.AddListener((val) => {
			ValidateLocalFilePath();
		});

		ValidateLocalFilePath();

		uploadButton.onClick.AddListener(UploadFile);
	}	

	void ValidateLocalFilePath(){
		if(File.Exists(localFileInput.text)){
			statusText.text = "Ready to upload.";
			uploadButton.interactable = true;
		}else{
			statusText.text = "<color=red>Specified file does not exist.</color>";
			uploadButton.interactable = false;
		}
	}

	void UploadFile(){
		uploadButton.interactable = false;
		var localFilePath = localFileInput.text;
		var uploadDropboxPath = Path.Combine("/DropboxSyncExampleFolder/", Path.GetFileName(localFilePath));

		Debug.Log(string.Format("Uploading {0} to Dropbox {1}...", localFilePath, uploadDropboxPath));

		DropboxSync.Main.UploadFile(uploadDropboxPath, localFilePath, (res) => {
			if(res.error != null){
				statusText.text = "<color=red>Failed to upload file: "+res.error.ErrorDescription+"</color>";
				Debug.LogError("Error uploading file: "+res.error.ErrorDescription);
				uploadButton.interactable = true;
			}else{
				statusText.text = "<color=green>File uploaded to "+uploadDropboxPath+"</color>";
				uploadButton.interactable = true;
			}
		}, (progress) => {
			statusText.text = "Uploading file... "+Mathf.RoundToInt(progress*100)+"%";
		});
	}

}
