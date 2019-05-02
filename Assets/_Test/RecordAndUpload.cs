using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DBXSync;
using DBXSync.Model;
using UnityEngine.UI;
using System.Linq;

/// <summary>
/// This was a quick proof of concept to see if Dropbox or Drive was better for a particular need.
/// 
/// We settled on just emailing it directly.
/// </summary>
public class RecordAndUpload : MonoBehaviour
{
    class VideoResultCallback : AndroidJavaProxy // C# definition of a Java interface
    {
        public RecordAndUpload script;
        public VideoResultCallback(RecordAndUpload _script) : base("com.quadratron.dbutest.IVideoResultListener") 
        {
            script = _script;
        }
        public void OnVideoResult(string path)
        {
            script.VideoPath = path;
            script.videoPathChanged = true;
        }
    }

    public DropboxSync client;  // Handy dandy dropbox client, RTM: Assets/DropboxSync/Documentation
    public Text uriText;
    public Text uploadStatus;

    private bool videoPathChanged = false;
    public string VideoPath { get; private set; }

    public string DB_TOKEN => Secrets.DB_TOKEN; // Dropbox Access Token: https://blogs.dropbox.com/developers/2014/05/generate-an-access-token-for-your-own-account/

    public string FROM => Secrets.FROM;         // outgoing email
    public string PASSWORD => Secrets.PASSWORD;
    public string CC => Secrets.CC;             // Always gets a copy


    private AndroidJavaClass _CPAClass;         // Our hook out to our Android code CustomPlayerActivity.java
    protected AndroidJavaClass CPAClass => _CPAClass ?? (_CPAClass = new AndroidJavaClass("com.quadratron.dbutest.CustomPlayerActivity")); // Lazy singleton
    private VideoResultCallback callback;       // Hook back from Android to Unity

    // Called before first update loop
    void Start()
    {
        // all the client seems to need
        client.DropboxAccessToken = DB_TOKEN;

        // setup callback for later
        callback = new VideoResultCallback(this);
        CPAClass.CallStatic("AddRecordVideoListener", callback);
    }

    void Update()
    {
        // has to be updated roundabout since Unity will throw a fit if we touch anything from outside the main thread
        if (videoPathChanged)
        {
            uriText.text = VideoPath;
            videoPathChanged = false;
        }
    }

    /// <summary>
    /// Requests video capture no longer than 45 seconds at low quality.
    /// </summary>
    public void RequestCaptureVideo()
    {
        // BUT it might come back longer than 45 seconds in high quality because asking isn't always getting.
        CPAClass.CallStatic("CaptureVideo", 45, false);
    }

    // Clean up after ourselves
    private void OnDestroy()
    {
        CPAClass.CallStatic("RemoveRecordVideoListener", callback);
    }

    /// <summary>
    /// Gets the media content. May require external (sdcard) read permission.
    /// </summary>
    /// <returns>The bytes of the video file.</returns>
    /// <param name="uri">URI.</param>
    public byte[] GetMediaContentBytes(string uri)
    {
        // HACK May trigger a heap defrag. Not recommended for longer or higher quality videos (YMMV)
        var javaByteArray = CPAClass.CallStatic<AndroidJavaObject>("GetMediaContentBytes", uri);
        if (javaByteArray.GetRawObject().ToInt32() == 0) {
            return null; // We're talking to Java from C#, so we _obviously_ need to check for null pointers. This is fine. Everything is fine.
        }
        return AndroidJNIHelper.ConvertFromJNIArray<byte[]>(javaByteArray.GetRawObject());
    }

    public void UploadVideo()
    {
        if (VideoPath == "") return; // Oops!

        string filename = VideoPath.Split('/').Last();
        uploadStatus.text = "Preparing Video...";                   // We have a filename...

        byte[] data = GetMediaContentBytes(VideoPath);
        uploadStatus.text = "Preparing Upload...";                  // ...we have the file...

        client.UploadFile("/test/" + filename + "low45.mp4", data,
            (DropboxRequestResult<DBXFile> result) =>
            {
                if (result.error == null && result.data != null) {
                    Debug.Log("Result : " + result.data.id);        // ...success!
                    uploadStatus.text = string.Format("{0} uploaded to {1}. [{2:###,###.# 'kB'}]", result.data.name, result.data.path, result.data.filesize / 1000f);
                }
                else {
                    Debug.Log("Error : " + result.error.ErrorDescription);
                }
            },
            (float progress) => uploadStatus.text = string.Format("Upload {0:###.00}%", progress * 100f));

        // Reset
        VideoPath = "";
        uriText.text = "";
    }
}
