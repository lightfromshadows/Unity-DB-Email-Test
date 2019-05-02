// DropboxSync v2.0
// Created by George Fedoseev 2018

using System;

namespace DBXSync.Model {
    
    [Serializable]
    public enum DBXItemType {
        File,
        Folder
    }

    [Serializable]
    public class DBXItem {
        public string id;
        public string name;
        public DBXItemType type;
        public string path;        
    }


}