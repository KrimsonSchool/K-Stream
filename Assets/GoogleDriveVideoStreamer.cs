using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using UnityEngine.Video;
using System.Collections;
using System;

public class GoogleDriveVideoStreamer : MonoBehaviour
{
    [Header("Google Drive Settings")]
    public string folderId = "1SP8DNx_e_erHcmUFxccSvxoCtBdAhgU_";
    public string apiKey = "AIzaSyAMyrvUvtOphE0ebjS6T76DeVhfGC_6vzY";

    [Header("UI References")]
    public GameObject buttonPrefab;
    public Transform buttonContainer;
    public RawImage videoDisplay;
    public VideoPlayer videoPlayer;

    [Serializable]
    public class DriveFile
    {
        public string id;
        public string name;
    }

    [Serializable]
    public class DriveFileList
    {
        public DriveFile[] files;
    }

    private void Start()
    {
        StartCoroutine(FetchDriveVideos());
    }

    IEnumerator FetchDriveVideos()
    {
        // Correctly encode the full query
        string query = Uri.EscapeDataString($"'{folderId}' in parents and mimeType contains 'video/'");
        string url = $"https://www.googleapis.com/drive/v3/files?q={query}&fields=files(id,name)&key={apiKey}";

        Debug.Log("Sending request to: " + url);  // Good for debugging!

        UnityWebRequest www = UnityWebRequest.Get(url);
        yield return www.SendWebRequest();

        if (www.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("Drive fetch error: " + www.error);
            Debug.LogError("Raw response: " + www.downloadHandler.text);
            yield break;
        }

        DriveFileList fileList = JsonUtility.FromJson<DriveFileList>(www.downloadHandler.text);

        foreach (DriveFile file in fileList.files)
        {
            CreateVideoButton(file.name, file.id);
        }
    }



    void CreateVideoButton(string name, string fileId)
    {
        GameObject buttonObj = Instantiate(buttonPrefab, buttonContainer);
        buttonObj.GetComponentInChildren<Text>().text = name;
        buttonObj.GetComponent<Button>().onClick.AddListener(() =>
        {
            StartCoroutine(PlayVideoFromDrive(fileId));
        });
    }

    IEnumerator PlayVideoFromDrive(string fileId)
    {
        string videoUrl = $"https://www.googleapis.com/drive/v3/files/{fileId}?alt=media&key={apiKey}";

        videoPlayer.source = VideoSource.Url;
        videoPlayer.url = videoUrl;
        videoPlayer.targetTexture = new RenderTexture(1920, 1080, 0);
        videoDisplay.texture = videoPlayer.targetTexture;
        videoDisplay.gameObject.SetActive(true);

        videoPlayer.Prepare();
        while (!videoPlayer.isPrepared)
            yield return null;

        videoPlayer.Play();
    }
}
