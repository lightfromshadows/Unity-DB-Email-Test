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

public class DropboxSyncDownloadExampleScript : MonoBehaviour {

	private static readonly string EXAMPLE_TXT_PATH = "/DropboxSyncExampleFolder/earth.txt";
	private static readonly string EXAMPLE_JSON_PATH = "/DropboxSyncExampleFolder/object.json";
	private static readonly string EXAMPLE_IMAGE_PATH = "/DropboxSyncExampleFolder/image.jpg";
	private static readonly string EXAMPLE_VIDEO_PATH = "/DropboxSyncExampleFolder/video.mp4";


	public Text planetDescriptionText;

	public Text planetInfoText;

	public RawImage rawImage;

	public VideoPlayer videoPlayer;
	RenderTexture videoRenderTexture = null;

	// Use this for initialization
	void Start () {
		
		// DropboxSync.Main.
		

		// TEXT
		DropboxSync.Main.GetFile<string>(EXAMPLE_TXT_PATH, (res) => {
			if(res.error != null){
				Debug.LogError("Error getting text string: "+res.error.ErrorDescription);
			}else{
				Debug.Log("Received text string from Dropbox!");
				var textStr = res.data;
				UpdatePlanetDescription(textStr);
			}
		}, receiveUpdates:true);

		// JSON OBJECT
		DropboxSync.Main.GetFile<JsonObject>(EXAMPLE_JSON_PATH, (res) => {
			if(res.error != null){
				Debug.LogError("Error getting JSON object: "+res.error.ErrorDescription);
			}else{
				Debug.Log("Received JSON object from Dropbox!");
				var jsonObject = res.data;
				UpdatePlanetInfo(jsonObject);
			}
		}, receiveUpdates:true);

		
		// IMAGE
		DropboxSync.Main.GetFile<Texture2D>(EXAMPLE_IMAGE_PATH, (res) => {
			if(res.error != null){
				Debug.LogError("Error getting picture from Dropbox: "+res.error.ErrorDescription);
			}else{
				Debug.Log("Received picture from Dropbox!");
				var tex = res.data;
				UpdatePicture(tex);
			}
		}, useCachedFirst:true);


		// VIDEO
		DropboxSync.Main.GetFileAsLocalCachedPath(EXAMPLE_VIDEO_PATH, (res) => {
			if(res.error != null){
				Debug.LogError("Error getting video from Dropbox: "+res.error.ErrorDescription);
			}else{
				Debug.Log("Received video from Dropbox!");
				var filePathInCache = res.data;
				UpdateVideo(filePathInCache);
			}
		}, receiveUpdates:true);


		// BYTES ARRAY
		DropboxSync.Main.GetFileAsBytes(EXAMPLE_IMAGE_PATH, (res) => {
			if(res.error != null){
				Debug.LogError("Failed to get file bytes: "+res.error.ErrorDescription);
			}else{
				var imageBytes = res.data;
				Debug.Log("Got file as bytes array, length: "+imageBytes.Length.ToString()+" bytes");
			}
		}, receiveUpdates:true);		
		
	}


	// UI-update methods

	void UpdatePlanetDescription(string desc){
		planetDescriptionText.text = desc;
	}

	void UpdatePlanetInfo(JsonObject planet){
		planetInfoText.text = "";
		foreach(var kv in planet){
			var valStr = "";
			if(kv.Value is List<object>){
				valStr = string.Join(", ", ((List<object>)kv.Value).Select(x => x.ToString()).ToArray()); 
			}else{
				valStr = kv.Value.ToString();
			}

			planetInfoText.text += string.Format("<b>{0}:</b> {1}\n", kv.Key, valStr);
		}	
	}		

	void UpdatePicture(Texture2D tex){
		rawImage.texture = tex;
		rawImage.GetComponent<AspectRatioFitter>().aspectRatio = (float)tex.width/tex.height;
	}

	void UpdateVideo(string localVideoPath){
		if(localVideoPath == null){
			videoPlayer.Stop();
			videoPlayer.source = VideoSource.VideoClip;
			videoPlayer.GetComponentInChildren<RawImage>().texture = null;		
			return;
		}
		
		videoPlayer.source = VideoSource.Url;
		videoPlayer.url = localVideoPath;
		videoPlayer.isLooping = true;

		if(videoRenderTexture == null){			
			videoRenderTexture = new RenderTexture(1024, 728, 16, RenderTextureFormat.ARGB32);
        	videoRenderTexture.Create();
		}		
		
		videoPlayer.targetTexture = videoRenderTexture;
		videoPlayer.GetComponentInChildren<RawImage>().texture = videoRenderTexture;
		videoPlayer.Play();
		
	}

}
