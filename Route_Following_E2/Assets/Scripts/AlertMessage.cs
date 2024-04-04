using UnityEngine;
using UnityEngine.UI;

public class AlertMessage : MonoBehaviour
{
    private Text alertText;
    public Image backgroundImage; // Reference to the Image component representing the background


    private void Start()
    {
        alertText = GetComponent<Text>();
        HideAlert();

        // Hide the background image initially
        backgroundImage.enabled = false;
    }

    public void ShowAlert(string message)
    {
        alertText.text = message;
        gameObject.SetActive(true);

        // Show the background image if there is text present
        if (!string.IsNullOrEmpty(message))
        {
            backgroundImage.enabled = true;
        }
        else
        {
            backgroundImage.enabled = false;
        }
    }

    public void HideAlert()
    {
        gameObject.SetActive(false);
        // Hide the background image
        backgroundImage.enabled = false;
    }
}