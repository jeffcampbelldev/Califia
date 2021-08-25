using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class YoutubeDemoScript : MonoBehaviour
{
    [SerializeField]
    private YoutubePlayer player;
    [SerializeField]
    private DemoCustomPlayerScript customPlayer;
    [SerializeField]
    private InputField urlInput;

    public void PreLoadTheVideoOnly()
    {
        //Good for when you want to keep showing the thumbnail to play the video later.
        player.PreLoadVideo("https://www.youtube.com/watch?v=l0U7SxXHkPY");
    }

    public void LoadUrlForACustomPlayer()
    {
        //this can be a reference to the custom video player, avpro component for examploe
        //First register the event callback referencing the custom player function, then call the youtube player to load.
        player.OnYoutubeUrlAreReady.AddListener(customPlayer.Play);
        player.LoadUrl("https://www.youtube.com/watch?v=l0U7SxXHkPY");
    }

    public void PlayFromUrlField()
    {
        //Simple call to start playing
        player.Play(urlInput.text);
    }

    public void PlayFromUrlFieldStartingAt()
    {
        //Simple call to start playing starting from second. in this case 10 seconds
        player.Play(urlInput.text, 10);
    }

    public void PlayCustomPlaylist()
    {
        //Play a custom playlist
        string[] playlist = new string[2];
        playlist[0] = "https://www.youtube.com/watch?v=l0U7SxXHkPY";
        playlist[1] = "https://www.youtube.com/watch?v=fHI8X4OXluQ";

        player.autoPlayNextVideo = true;
        player.Play(playlist);
    }
}
