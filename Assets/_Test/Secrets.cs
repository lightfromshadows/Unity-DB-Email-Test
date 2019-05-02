/// <summary>
/// Secret information, do not upload your secret information to Git Hub.
/// I used git update-index --assume-unchanged [file]
/// </summary>
public class Secrets
{
    public static readonly string DB_TOKEN = "<Your Token Here>";       // Dropbox Access Token: https://blogs.dropbox.com/developers/2014/05/generate-an-access-token-for-your-own-account/
    public static readonly string FROM = "<Gmail account>";             // outgoing email account
    public static readonly string PASSWORD = "<Gmail app password>";    // Gmail App Password: https://support.google.com/mail/answer/185833?hl=en
    public static readonly string CC = "foo@bar.com;baz@qux.com";       // Someone(s) to always get a copy, semi-colon delimited
}
