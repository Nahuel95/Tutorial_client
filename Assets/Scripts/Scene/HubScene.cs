using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HubScene : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI selfInformation;
    [SerializeField] private TMP_InputField addFollowInput;

    [SerializeField] private GameObject followPrefab;
    [SerializeField] private Transform followContainer;

    public static HubScene Instance { set; get; }
    private void Start() {
        Instance = this;
        selfInformation.text = Client.Instance.self.Username + "#" + Client.Instance.self.Discriminator;
        Client.Instance.SendRequestFollow();
    }

    public void AddFollowToUi(Account follow) {
        Debug.Log("DEBUG!!");

        GameObject followItem = Instantiate(followPrefab, followContainer);
        followItem.GetComponentInChildren<TextMeshProUGUI>().text = follow.Username + "#" + follow.Discriminator;
        followItem.transform.GetChild(1).GetComponent<Image>().color = (follow.Status != 0) ? Color.green : Color.red;
        followItem.transform.GetChild(2).GetComponent<Button>().onClick.AddListener(delegate { Destroy(followItem); });
        followItem.transform.GetChild(2).GetComponent<Button>().onClick.AddListener(delegate { OnClickRemoveFollow(follow.Username, follow.Discriminator); });

    }

    public void OnClickAddFollow() {
        string usernameDiscriminator = addFollowInput.text;

        if (!Utility.IsUsernameAndDiscriminator(usernameDiscriminator) && !Utility.IsEmail(usernameDiscriminator))
        {
            Debug.Log("Invalid format!");
            return;
        }
        Client.Instance.SendAddFollow(usernameDiscriminator);


    }

    public void OnClickRemoveFollow(string username, string discriminator)
    {
        Client.Instance.SendRemoveFollow(username + "#" + discriminator);

    }
}
