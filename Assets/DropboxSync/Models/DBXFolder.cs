// DropboxSync v2.0
// Created by George Fedoseev 2018

using System;
using System.Collections.Generic;
using DBXSync.Utils;

namespace DBXSync.Model {

    [Serializable]
    public class DBXFolder : DBXItem {

        public List<DBXItem> items;    

        public DBXFolder() {
            type = DBXItemType.Folder;
        }

        public DBXFolder(string p) {
            type = DBXItemType.Folder;
            path = DropboxSyncUtils.NormalizePath(p);
        }

        public static DBXFolder FromDropboxDictionary(Dictionary<string, object> obj){                       
            
            return new DBXFolder() {
                id = obj["id"] as string,
                name = obj["name"] as string,           
                path = obj["path_lower"] as string,
                items = new List<DBXItem>()
            };
        }
    }
}