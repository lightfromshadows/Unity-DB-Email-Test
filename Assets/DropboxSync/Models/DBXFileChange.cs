// DropboxSync v2.0
// Created by George Fedoseev 2018

namespace DBXSync.Model {

    public enum DBXFileChangeType {
        None,
        Modified,
        Deleted,
        Added
    }

    public class DBXFileChange {
        public DBXFile file;
        public DBXFileChangeType changeType;    
        

        public DBXFileChange(DBXFile f, DBXFileChangeType c){
            file = f;
            changeType = c;        
        }
    }
}