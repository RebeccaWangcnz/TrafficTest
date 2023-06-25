using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VoiceBroadcast : MonoBehaviour
{
    //����
    public AudioSource audioSource;

    //�ж�
    bool PlayerAtExit = false;
    bool HasAudioplayed;//��Ƶֻ����һ��

    //�������¼���������ƵĴ�����
    private void OnTriggerEnter(Collider other)
    {
        //������봥�����������
        if (other.name == "Sport Coupe Collider Base")
        {
            PlayerAtExit = true;
        }
    }

    //ÿ֡���ô˺���
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
