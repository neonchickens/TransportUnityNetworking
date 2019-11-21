using System;
using System.Collections;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class UserLogin : MonoBehaviour
{
    public Button btnLogin;
    public Button btnRegister;

    public InputField txtUsername;
    public InputField txtPassword;

    public Text txtNotify;

    // Start is called before the first frame update
    void Start()
    {
        txtUsername.text = PlayerPrefs.GetString("username");

        btnLogin.onClick.AddListener(delegate
        {
            PlayerPrefs.SetString("username", txtUsername.text);
            StartCoroutine(Login());
        });

        btnRegister.onClick.AddListener(delegate
        {
            StartCoroutine(Register());
        });
    }

    public string HashPass(string pass)
    {
        byte[] buffer = Encoding.UTF8.GetBytes(pass);
        SHA512Managed hashAlgorithm = new SHA512Managed();
        byte[] hash = hashAlgorithm.ComputeHash(buffer);
        return Encoding.UTF8.GetString(hash);
    }

    IEnumerator Login()
    {
        //Connect to questions database
        string domain = "http://34.205.7.163/";
        string attempts_url = domain + "mymmo_login.php";

        // Create a form object for sending data to the server
        WWWForm form = new WWWForm();
        form.AddField("username", txtUsername.text.ToString());
        form.AddField("password", HashPass(txtPassword.text.ToString()));

        var download = UnityWebRequest.Post(attempts_url, form);

        // Wait until the download is done
        yield return download.SendWebRequest();

        if (download.isNetworkError || download.isHttpError)
        {
            Debug.Log("Error downloading: " + download.error);
            txtNotify.text = "Error with server.";
        }
        else
        {
            Debug.Log(download.downloadHandler.text);
            bool result = false;
            if (bool.TryParse(download.downloadHandler.text, out result) && result)
            {
                SceneManager.LoadScene(1);
            } else
            {

                txtNotify.text = "User doesn't exist or password incorrect.";
            }
        }
    }

    IEnumerator Register()
    {
        //Connect to questions database
        string domain = "http://34.205.7.163/";
        string attempts_url = domain + "mymmo_register.php";

        // Create a form object for sending data to the server
        WWWForm form = new WWWForm();
        form.AddField("username", txtUsername.text.ToString());
        form.AddField("password", HashPass(txtPassword.text.ToString()));

        var download = UnityWebRequest.Post(attempts_url, form);

        // Wait until the download is done
        yield return download.SendWebRequest();

        if (download.isNetworkError || download.isHttpError)
        {
            Debug.Log("Error downloading: " + download.error);
            txtNotify.text = "Error with server.";
        }
        else
        {
            Debug.Log(download.downloadHandler.text);
            bool result = false;
            if (bool.TryParse(download.downloadHandler.text, out result) && result)
            {
                //Success
                txtNotify.text = "You have successfully registered, you may log in.";

            } else
            {
                //Failure
                txtNotify.text = "Username in use.";
            }
        }
    }

}
