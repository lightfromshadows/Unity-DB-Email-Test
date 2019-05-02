// DropboxSync v2.0
// Created by George Fedoseev 2018

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using UnityEngine;

namespace DBXSync.Utils {

    public class DropboxSyncUtils {
        public static string NormalizePath(string strPath){	
			strPath = strPath.Trim();	
			var components = strPath.Split(new char[] {'/'}, StringSplitOptions.RemoveEmptyEntries);
			return ("/"+string.Join("/", components)).ToLower();
		}

        public static List<string> GetPathFolders(string dropboxPath){
            var result = new List<string>();

            dropboxPath = NormalizePath(dropboxPath);
            var components = dropboxPath.Split(new char[] {'/'}, StringSplitOptions.RemoveEmptyEntries).ToList();

            var build_components = new List<string>();
            foreach(var c in components){
                build_components.Add(c);
                var p = ("/"+string.Join("/", build_components.ToArray())).ToLower();
                if(!(  Path.HasExtension(p) && c == components.Last()  )){
                    result.Add(p);
                }
            }

            return result;
        }

        public static void ValidateAccessToken(string accessToken){
            if(DropboxSyncUtils.IsBadAccessToken(accessToken)){
                throw new Exception("Bad Dropbox access token. Please specify a valid access token.");					
            }
        }

        public static bool IsBadAccessToken(string accessToken){
            if(accessToken.Trim().Length == 0){
                return true;
            }

            if(accessToken.Length < 20){
                return true;
            }

            return false;
        }

        public static bool IsBadDropboxPath(string dropboxPath){
            if(dropboxPath.Length == 0){
                return true;
            }

            if(dropboxPath[0] != '/'){
                Debug.LogError("Dropbox paths should start with '/'");
                return true;
            }
            

            return false;
        }

		public static bool IsPathImmediateChildOfFolder(string folderPath, string candidatePath){
			if(folderPath == candidatePath){
				return false;
			}
			if (candidatePath.IndexOf(folderPath) != 0){
				return false;
			}
			// consider /rootfolder and /rootfolder/file.jpg or /rootfolder/otherfolder
			// replacing gives: /file.jpg /otherfolder
			// so count of slashes should be 1 or 0 (for root folder /)
			return candidatePath.Replace(folderPath, "").Count(c => c == '/') <= 1;			
		}

        public static Texture2D LoadImageToTexture2D(byte[] data) {
            Texture2D tex = null;
            tex = new Texture2D(2, 2);                     
            
            tex.LoadImage(data);
            //tex.filterMode = FilterMode.Trilinear; 	
            //tex.wrapMode = TextureWrapMode.Clamp;
            //tex.anisoLevel = 9;

            return tex;
        }

        public static string GetAutoDetectedEncodingStringFromBytes(byte[] bytes){
            using (var reader = new System.IO.StreamReader(new System.IO.MemoryStream(bytes), true)){
                var detectedEncoding = reader.CurrentEncoding;
                return detectedEncoding.GetString(bytes);
            }	
        }

        public static bool IsOnline(){
            try {
                using (WebClient client = new DBXWebClient()){
                    using (client.OpenRead("http://www.google.com/")){
                        return true;
                    }
                }
            }catch{
                return false;
            }
        }

         public static void IsOnlineAsync(Action<bool> onResult){
            var thread = new Thread(() => {                
                onResult(IsOnline());
            });
            thread.IsBackground = true;
            thread.Start();
        }

        public static string GetDropboxContentHashForFile(string filePath){
            // Debug.LogWarning("Calculate content hash");
            var hasher = new DropboxContentHasher();
            byte[] buf = new byte[1024];
            using (var file = File.OpenRead(filePath))
            {
                while (true)
                {
                    int n = file.Read(buf, 0, buf.Length);
                    if (n <= 0) break;  // EOF
                    hasher.TransformBlock(buf, 0, n, buf, 0);
                }
            }

            hasher.TransformFinalBlock(new byte[0], 0, 0);
            string hexHash = DropboxContentHasher.ToHex(hasher.Hash);

            return hexHash;
        }

    }


}