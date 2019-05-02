// DropboxSync v2.0
// Created by George Fedoseev 2018

using System;

namespace DBXSync {

    [Serializable]
	public enum DBXErrorType {
		Unknown,
		NotAuthorized,
		RemotePathNotFound,
        LocalPathNotFound,
		BadRequest,
		LocalFileSystemError,
        NetworkProblem,
        DropboxAPIError,
        ParsingError,
        UserCancelled,
        NotSupported,
        RemotePathAlreadyExists
	}

    [Serializable]
    public class DBXError {
        private string _errorDescription;
        public string ErrorDescription {
            get {
                return _errorDescription;
            }

            set {
                _errorDescription = value;
            }
        }

        private DBXErrorType _errorType = DBXErrorType.Unknown;
        public DBXErrorType ErrorType {
            get {
                return _errorType;
            }

            set {
                _errorType = value;
            }
        }

        public DBXError(string errorDescription, DBXErrorType errorType){
            _errorDescription = errorDescription;
            _errorType = errorType;
        }


        public static DBXErrorType DropboxAPIErrorSummaryToErrorType(string errorSummary){
            if(errorSummary.Contains("not_found")){
                return DBXErrorType.RemotePathNotFound;
            } else if(errorSummary.Contains("to/conflict/file")){
                return DBXErrorType.RemotePathAlreadyExists;
            }

            return DBXErrorType.DropboxAPIError;
        }
    }
}
