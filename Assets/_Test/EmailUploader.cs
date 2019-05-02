using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.IO;
using System.ComponentModel;

/// <summary>
/// Bonus storage location, Gmail!
/// </summary>
public class EmailUploader : MonoBehaviour
{
    [SerializeField] RecordAndUpload videoScript;

    public void EmailVideo()
    {
        if (videoScript.VideoPath == "") return;

        var client = new SmtpClient("smtp.gmail.com", 587);
        string filename = videoScript.VideoPath.Split('/').Last() + "low45.mp4";
        try
        {
            client.EnableSsl = true;
            client.UseDefaultCredentials = false;
            client.Credentials = new NetworkCredential(Secrets.FROM, Secrets.PASSWORD);

            ServicePointManager.ServerCertificateValidationCallback = OnRemoteCertValidation;

            var message = new MailMessage();
            message.From = new MailAddress(Secrets.FROM, Application.productName);
            message.To.Add(new MailAddress(Secrets.CC));
            message.IsBodyHtml = false;
            message.Subject = "Video Delivery Test";
            message.Body = "It's dangerous to go alone, take this! -|-->";


            byte[] data = videoScript.GetMediaContentBytes(videoScript.VideoPath);
            MemoryStream stream = new MemoryStream(data);
            Attachment attachment = new Attachment(stream, filename);
            message.Attachments.Add(attachment);

            client.SendCompleted += OnSendComplete;
            client.SendAsync(message, "");
        }
        catch (Exception ex)
        {
            Debug.LogError("Exception Occured: " + ex.ToString());
        }
    }
    static void OnSendComplete(object sender, AsyncCompletedEventArgs e)
    {
        if (e.Error != null)
        {
            Debug.LogError("Send Error: " + e.Error);
        }
        else if (e.Cancelled)
        {
            Debug.LogWarning("Send Message Cancelled");
        }
    }

    static bool OnRemoteCertValidation(object sender, System.Security.Cryptography.X509Certificates.X509Certificate certificate, System.Security.Cryptography.X509Certificates.X509Chain chain, System.Net.Security.SslPolicyErrors sslPolicyErrors)
    {
        Debug.Log("" + certificate + chain + sslPolicyErrors);
        return true;
    }
}
