// DropboxSync v2.0
// Created by George Fedoseev 2018

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using System.Linq;
using UnityEngine.UI;
using DBXSync;
using DBXSync.Model;

public class DropboxSyncFileExplorerExampleScript : MonoBehaviour {

	public Button goUpButton;

	public ScrollRect scrollRect;
	public Text fileStatusText;

	List<string> pathsHistory = new List<string>();

	// Use this for initialization
	void Start () {		
		RenderFolder("/");

		goUpButton.onClick.AddListener(() => {
			GoUp();
		});
	}

	void GoUp(){
		if(pathsHistory.Count > 1){
			pathsHistory.Remove(pathsHistory.Last());
			var prev = pathsHistory.Last();
			pathsHistory.Remove(prev);
			RenderFolder(prev);
		}
	}

	void RenderFolder(string dropboxFolderPath){
		Debug.Log("render folder "+dropboxFolderPath);
		RenderLoading();

		pathsHistory.Add(dropboxFolderPath);

		DropboxSync.Main.GetFolderItems(dropboxFolderPath, (res) => {
			if(res.error != null){
				Debug.LogError("Failed to get folder items for folder "+dropboxFolderPath+" "+res.error.ErrorDescription);
			}else{
				var folderItems = res.data;
				RenderFolderItems(folderItems);				
			}
		});
	}

	void RenderFolderItems(List<DBXItem> folderItems){
		// clear content
		foreach(Transform t in scrollRect.content.transform){
			Destroy(t.gameObject);
		}

		var orderedItems = folderItems.OrderBy(x => x.name).OrderByDescending(x => x.type);

		foreach(var item in orderedItems){
			var _item = item;
			switch(item.type){
				case DBXItemType.Folder:
					var go = Instantiate(Resources.Load("DBXExplorerFolderRow")) as GameObject;
					go.transform.SetParent(scrollRect.content.transform);
					go.transform.position = Vector3.zero;
					go.transform.rotation = Quaternion.identity;
					go.transform.localScale = Vector3.one;

					go.GetComponentInChildren<Text>().text = _item.name;
					go.GetComponentInChildren<Button>().onClick.AddListener(() => {
						RenderFolder(_item.path);						
					});

				break;
				case DBXItemType.File:
					var _go = Instantiate(Resources.Load("DBXExplorerFileRow")) as GameObject;
					_go.transform.SetParent(scrollRect.content.transform);
					_go.transform.position = Vector3.zero;
					_go.transform.rotation = Quaternion.identity;
					_go.transform.localScale = Vector3.one;

					_go.GetComponentInChildren<Text>().text = _item.name;
					_go.GetComponentInChildren<Button>().onClick.AddListener(() => {
						DisplayFileStatus("Downloading file "+_item.path+"...");
						DropboxSync.Main.GetFileAsBytes(_item.path, (res) => {
							if(res.error != null){
								DisplayFileStatus("Failed to download "+_item.path+" to cache: "+res.error.ErrorDescription);
							}else{
								DisplayFileStatus("Downloaded "+_item.path+" to cache.\nTotal: "+res.data.Length.ToString()+" bytes");
							}
						}, (progress) => {
							DisplayFileStatus("Downloading "+_item.path+"... "+(progress*100).ToString()+"%");
						});
					});

				break;
				default:
				break;
			}
		}
		
	}

	void RenderLoading(){
		// clear content
		foreach(Transform t in scrollRect.content.transform){
			Destroy(t.gameObject);
		}
		
		var go = Instantiate(Resources.Load("LoadingRow")) as GameObject;
		go.transform.SetParent(scrollRect.content.transform);
		go.transform.position = Vector3.zero;
		go.transform.rotation = Quaternion.identity;
		go.transform.localScale = Vector3.one;
	}

	void DisplayFileStatus(string status){
		fileStatusText.text = status;
	}

}
