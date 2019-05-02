// DropboxSync v2.0
// Created by George Fedoseev 2018

using System;
using System.Collections.Generic;


namespace DBXSync.Model {

	


	[Serializable]
	public class DropboxRequestParams {

	}


	// FOLDER REQUESTS
	[Serializable]
	public class DropboxCreateFolderRequestParams: DropboxRequestParams {
		public string path;
		public bool autorename = false;
	}

	[Serializable]
	public class DropboxListFolderRequestParams : DropboxRequestParams {
		public string path;
		public bool recursive = true;
		public bool include_media_info = false;
		public bool include_deleted = false;
		public bool include_has_explicit_shared_members = false;
		public bool include_mounted_folders = false;
	}

	[Serializable]
	public class DropboxContinueWithCursorRequestParams : DropboxRequestParams {
		public string cursor;

		public DropboxContinueWithCursorRequestParams(string cur) {
			cursor = cur;
		}
	}


	// METADATA

	[Serializable]
	public class DropboxGetMetadataRequestParams : DropboxRequestParams {
		public string path;	
		public bool include_media_info = false;
		public bool include_deleted = false;
		public bool include_has_explicit_shared_members = false;	

		public DropboxGetMetadataRequestParams(string _path){
			path = _path;
		}
	}

	// DOWNLOAD FILE
	[Serializable]
	public class DropboxDownloadFileRequestParams : DropboxRequestParams {
		public string path;

		public DropboxDownloadFileRequestParams(string _path){
			path = _path;
		}
	}

	// UPLOAD FILE
	[Serializable]
	public class DropboxUploadFileRequestParams : DropboxRequestParams {
		public string path;
		public string mode;


		public DropboxUploadFileRequestParams(string _path){
			path = _path;
			mode = "overwrite";
		}
	}

	// MOVE FILE
	[Serializable]
	public class DropboxMoveFileRequestParams: DropboxRequestParams {
		public string from_path;
		public string to_path;
		public bool allow_shared_folder = false;
		public bool autorename = false;
		public bool allow_ownership_transfer = false;
	}

	// DELETE PATH
	[Serializable]
	public class DropboxDeletePathRequestParams: DropboxRequestParams {
		public string path;
	}


	// RESULTS

	public class DropboxRequestResult<T> {
		public T data;
		public DBXError error = null;

		public DropboxRequestResult(T res){
			this.data = res;
		}

		public static DropboxRequestResult<T> Error(DBXError error){
			var inst = new DropboxRequestResult<T>(default(T));
			inst.error = error;
			return inst;
		}
	}

	public class DropboxFileDownloadRequestResult<T> {
		public T data;
		public DBXFile fileMetadata;
		public DBXError error = null;


		public DropboxFileDownloadRequestResult(T res, DBXFile metadata){
			this.data = res;
			fileMetadata = metadata;
		}

		public static DropboxFileDownloadRequestResult<T> Error(DBXError error){
			var inst = new DropboxFileDownloadRequestResult<T>(default(T), null);
			inst.error = error;
			return inst;
		}
	}
}