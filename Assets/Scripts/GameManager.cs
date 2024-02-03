using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public Dongle lastDongle;
    public GameObject donglePrefab;
    public Transform dongleGroup;

    void Start()
    {
        NextDongle();
    }

    Dongle GetDongle()
    {
        GameObject instant = Instantiate(donglePrefab, dongleGroup);    // Instantiate 함수 그대로 써도 가능하지만 두번째 인자값이 생성할 때 지정되는 부모 오브젝트
        Dongle instantDongle = instant.GetComponent<Dongle>();

        return instantDongle;

    }

    void NextDongle()
    {
        Dongle newDongle = GetDongle();
        lastDongle = newDongle;         // 동글 생성 시 보관 용도로 동글 변수에 저장
    }

    public void TouchDown()
    {
        if (lastDongle == null)
            return;

        lastDongle.Drag();
    }

    public void TouchUp()
    {
        if (lastDongle == null)
            return;

        lastDongle.Drop();
        lastDongle = null;              // 손을 떼면 더 이상 터치 못하게 막기
    }
}
