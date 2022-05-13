using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class LobbyScene : MonoBehaviour {

    public static LobbyScene Instance { set; get; }

    public InputField username;
    public InputField creationPass;
    public InputField email;
    public InputField usernameOrEmail;
    public InputField loginPass;
    public TMP_Text welcomeMessage;
    public TMP_Text authenticationMessage;
    public Canvas canvas;

    private void Start() {
        Instance = this;
    }
    public void OnClickCreateAccount() {
        DisableInputs();

        string name = username.text;
        string pass = creationPass.text;
        string mail = email.text;

        Client.Instance.SendCreateAccount(name, pass, mail);
    }
    public void OnClickLoginRequest(){
        DisableInputs();

        string nameOrEmail = usernameOrEmail.text;
        string pass = loginPass.text;

        Client.Instance.SendLoginRequest(nameOrEmail, pass);

    }

    public void ChangeWelcomeMessage(string msg) {
        welcomeMessage.text = msg;
    }

    public void ChangeAuthenticationMessage(string msg) {
        authenticationMessage.text = msg;
    }

    public void EnableInputs() {
        canvas.GetComponent<CanvasGroup>().interactable = true;
    }
    public void DisableInputs()
    {
        canvas.GetComponent<CanvasGroup>().interactable = false;
    }
}
