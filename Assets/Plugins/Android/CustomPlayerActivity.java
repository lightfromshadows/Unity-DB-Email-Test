package com.quadratron.dbutest;

import android.content.Intent;
import android.net.Uri;
import android.os.Bundle;
import android.provider.MediaStore;
import com.unity3d.player.UnityPlayerActivity;

import java.io.FileInputStream;
import java.io.FileNotFoundException;
import java.io.InputStream;
import java.util.ArrayList;
import java.util.List;

public class CustomPlayerActivity extends UnityPlayerActivity {

    static final int REQUEST_VIDEO_CAPTURE = 1;
    private static List<IVideoResultListener> videoResultListeners = new ArrayList<IVideoResultListener>();
    private static UnityPlayerActivity _activity;

    @Override
    protected void onCreate(Bundle bundle) {
        super.onCreate(bundle);
        _activity = this;
    }

    public static void AddRecordVideoListener(IVideoResultListener listener)
    {
        videoResultListeners.add(listener);
    }

    public static void RemoveRecordVideoListener(IVideoResultListener listener)
    {
        videoResultListeners.remove(listener);
    }

    public static void CaptureVideo(int maxLength, boolean highQuality)
    {
        Intent intent = new Intent(MediaStore.ACTION_VIDEO_CAPTURE);
        intent.putExtra(MediaStore.EXTRA_DURATION_LIMIT, maxLength);
        intent.putExtra(MediaStore.EXTRA_VIDEO_QUALITY, highQuality ? 1 : 0);

        if (intent.resolveActivity(_activity.getPackageManager()) != null) {
            _activity.startActivityForResult(intent, REQUEST_VIDEO_CAPTURE);
        }
    }

    public static byte[] GetMediaContentBytes(String path)
    {
        Uri uri = Uri.parse(path);
        try {
            InputStream in = _activity.getContentResolver().openInputStream(uri);
            byte[] retval = new byte[in.available()];
            if (in.read(retval) > 0) {
                in.close();
                return retval;
            }
        }
        catch (Exception e) {
            e.printStackTrace();
        }
        return null;
    }

    @Override
    protected void onActivityResult(int requestCode, int resultCode, Intent data) {
        super.onActivityResult(requestCode, resultCode, data);

        if (requestCode == REQUEST_VIDEO_CAPTURE && resultCode == RESULT_OK) {
            for (IVideoResultListener vl : videoResultListeners)
            {
                String path = data.getData().toString();
                vl.OnVideoResult(path);
            }
        }
    }

}
