using UnityEngine;
using UnityEngine.UI;

public class ViewManager : MonoBehaviour
{
    [Header("View References")]
    public GameObject ReaperView;
    public GameObject ImportView;
    public GameObject SettingsView;
    public GameObject screen;

    [Header("Button References")]
    public Button settingsButton;

    private GameObject currentView;
    private GameObject previousView;
    private void Start()
    {
        currentView = ReaperView;
        settingsButton.onClick.AddListener(ShowSettingsView);
    }

    public void ShowReaperView()
    {
        currentView = ReaperView;
        ReaperView.SetActive(true);
        ImportView.SetActive(false);
        SettingsView.SetActive(false);
        screen.SetActive(false);
        settingsButton.gameObject.SetActive(true);
    }

    public void ShowImportView()
    {
        currentView = ImportView;
        ReaperView.SetActive(false);
        ImportView.SetActive(true);
        SettingsView.SetActive(false);
        screen.SetActive(false);
    }

    public void ShowSettingsView()
    {
        previousView = currentView;
        ReaperView.SetActive(false);
        ImportView.SetActive(false);
        SettingsView.SetActive(true);
        screen.SetActive(false);
        settingsButton.gameObject.SetActive(false);
    }

    public void HideSettingsView()
    {
        previousView.SetActive(true);
        SettingsView.SetActive(false);
        settingsButton.gameObject.SetActive(true);
    }

    public void ShowVideoScreen()
    {
        ReaperView.SetActive(false);
        ImportView.SetActive(false);
        SettingsView.SetActive(false);
        screen.SetActive(true);
    }

    public void CloseVideoScreen()
    {
        screen.SetActive(false);
        ImportView.SetActive(true);
    }
}