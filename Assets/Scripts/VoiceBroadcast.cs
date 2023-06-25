using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VoiceBroadcast : MonoBehaviour
{
    //音乐
    public AudioSource audioSource;

    //判断
    bool PlayerAtExit = false;
    bool HasAudioplayed;//音频只播放一次

    //触发器事件，传入控制的触发器
    private void OnTriggerEnter(Collider other)
    {
        //如果进入触发器的是玩家
        if (other.name == "Sport Coupe Collider Base")
        {
            PlayerAtExit = true;
        }
    }

    //每帧调用此函数
    void Update()
    {
        if (PlayerAtExit)
        {
            if (!HasAudioplayed)
            {
                audioSource.Play();
                HasAudioplayed = true;
            }
        }
    }
}
