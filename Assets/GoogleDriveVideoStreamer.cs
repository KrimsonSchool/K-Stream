using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using UnityEngine.Video;
using System.Collections;
using System;
using UnityEngine.SceneManagement;

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

    public GameObject loader;
    public GameObject pause;
    public Text timeText;

    private string playingFileName;

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
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.Confined;
        StartCoroutine(FetchDriveVideos());
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            if (!videoPlayer.isPaused)
            {
                videoPlayer.Pause();
                pause.SetActive(true);
                timeText.text = "Paused";
                
                int totalSeconds = Mathf.RoundToInt(videoPlayer.frame / videoPlayer.frameRate);

                int minutes = totalSeconds / 60;
                int seconds = totalSeconds % 60;
                
                string past = minutes.ToString("00") + ":" + seconds.ToString("00");
                
                 totalSeconds = Mathf.RoundToInt(videoPlayer.frameCount / videoPlayer.frameRate);

                 minutes = totalSeconds / 60;
                 seconds = totalSeconds % 60;
                
                string to = minutes.ToString("00") + ":" + seconds.ToString("00");
                
                timeText.text = playingFileName+"  -  "+ past+"/"+ to;

            }
            else
            {
                videoPlayer.Play();
                pause.SetActive(false);
            }
        }

        if (Input.GetKeyDown(KeyCode.RightArrow))
        {
            videoPlayer.time += 10;
        }

        if (Input.GetKeyDown(KeyCode.LeftArrow))
        {
            videoPlayer.time -= 10;
        }

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            SceneManager.LoadScene(0);
        }
    }

    IEnumerator FetchDriveVideos()
    {
        string query = Uri.EscapeDataString($"'{folderId}' in parents and mimeType contains 'video'");
        string url = $"https://www.googleapis.com/drive/v3/files?q={query}&pageSize=1000&fields=files(id,name,mimeType)&key={apiKey}";

        Debug.Log("Google Drive URL: " + url);

        UnityWebRequest www = UnityWebRequest.Get(url);
        yield return www.SendWebRequest();

        if (www.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("Drive fetch error: " + www.error);
            Debug.LogError("Raw response: " + www.downloadHandler.text);
            yield break;
        }

        DriveFileList fileList = JsonUtility.FromJson<DriveFileList>(www.downloadHandler.text);

        Debug.Log($"Fetched {fileList.files.Length} videos.");
        
        Array.Sort(fileList.files, (a, b) => string.Compare(a.name, b.name, StringComparison.OrdinalIgnoreCase));

        foreach (DriveFile file in fileList.files)
        {
            CreateVideoButton(file.name, file.id);
        }
    }




    void CreateVideoButton(string fName, string fileId)
    {
        GameObject buttonObj = Instantiate(buttonPrefab, buttonContainer);
        buttonObj.GetComponentInChildren<Text>().text = fName;
        buttonObj.GetComponent<Button>().onClick.AddListener(() =>
        {
            StartCoroutine(PlayVideoFromDrive(fileId, fName));
        });
    }

    IEnumerator PlayVideoFromDrive(string fileId, string fileName)
    {        
        string videoUrl = $"https://www.googleapis.com/drive/v3/files/{fileId}?alt=media&key={apiKey}";
        
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
        loader.SetActive(true);
        playingFileName = fileName;

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
