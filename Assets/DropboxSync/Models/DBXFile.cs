// DropboxSync v2.0
// Created by George Fedoseev 2018

using System;
using System.Collections.Generic;
using DBXSync.Utils;

namespace DBXSync.Model {

    [Serializable]
    public class DBXFile : DBXItem {

        public string clientModified;
        public string serverModified;

        public string revision_id;
        public long filesize;
        public string contentHash;

        public bool deletedOnRemote = false;

        public DBXFile() {
            type = DBXItemType.File;
        }

        public DBXFile(string p) {
            type = DBXItemType.File;
            path = DropboxSyncUtils.NormalizePath(p);
        }

        public static DBXFile DeletedOnRemote(string p) {
            return new DBXFile{path=p, deletedOnRemote=true};
        }



        public static DBXFile FromDropboxDictionary(Dictionary<string, object> obj){
            
            return new DBXFile() {
                id = obj["id"] as string,
                name = obj["name"] as string,           
                path = obj["path_lower"] as string,

                clientModified = obj["client_modified"] as string,
                serverModified = obj["server_modified"] as string,
                revision_id = obj["rev"] as string,
                filesize = long.Parse(obj["size"].ToString()),
                contentHash = obj["content_hash"] as string
            };
        }
    }

}