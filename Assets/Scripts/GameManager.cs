using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;              // 장면 관리 담당

public class GameManager : MonoBehaviour
{
    [Header("------------[ Core ]")]
    public int score;
    public int maxLevel;
    public bool isOver;

    [Header("------------[ Object Pooling ]")]
    public GameObject donglePrefab;
    public Transform dongleGroup;
    public List<Dongle> donglePool;

    public GameObject effectPrefab;
    public Transform effectGroup;
    public List<ParticleSystem> effectPool;

    [Range(1, 30)]
    public int poolSize;        // 오브젝트풀 관리를 위해 사이즈, 커버 변수 추가
    public int poolCursor;
    public Dongle lastDongle;

    [Header("------------[ Audio ]")]

    public AudioSource bgmPlayer;
    public AudioSource[] sfxPlayer;
    public AudioClip[] sfxClip;
    public enum Sfx { LevelUp, Next, Attach, Button, Over };
    int sfxCursor;

    [Header("------------[ UI ]")]
    public GameObject startGroup;
    public GameObject endGroup;
    public Text scoreText;
    public Text maxScoreText;
    public Text subScoreText;

    [Header("------------[ ETC ]")]
    public GameObject line;
    public GameObject bottom;

    void Awake()
    {
        Application.targetFrameRate = 60;           // 프레임 고정

        donglePool = new List<Dongle>();
        effectPool = new List<ParticleSystem>();

        for (int index = 0; index < poolSize; index++)      // for 문으로 지정해놓은 poolSize만큼 동글 생성
        {
            MakeDongle();
        }
        if (!PlayerPrefs.HasKey("MaxScore"))
        {
            PlayerPrefs.SetInt("MaxScore", 0);
        }

        maxScoreText.text = PlayerPrefs.GetInt("MaxScore").ToString();
    }

    public void GameStart()
    {
        line.SetActive(true);
        bottom.SetActive(true);
        scoreText.gameObject.SetActive(true);
        maxScoreText.gameObject.SetActive(true);
        startGroup.SetActive(false);

        bgmPlayer.Play();
        SfxPlay(Sfx.Button);
        Invoke("NextDongle", 1.5f);   // 함수 호출에 딜레이를 주고싶을 때 사용하는 함수
    }

    Dongle MakeDongle()
    {
        // 이펙트 생성
        GameObject instantEffectObj = Instantiate(effectPrefab, effectGroup);
        instantEffectObj.name = "Effect" + effectPool.Count;        // 이펙트 오브젝트가 생성 될 때 이름 지정
        ParticleSystem instantEffect = instantEffectObj.GetComponent<ParticleSystem>(); // 오브젝트 컴포넌트 초기화
        effectPool.Add(instantEffect);      // List.Add : 해당 리스트에 데이터를 추가하는 함수

        // 동글 생성
        GameObject instantDongleObj = Instantiate(donglePrefab, dongleGroup);    // Instantiate 함수 그대로 써도 가능하지만 두번째 인자값이 생성할 때 지정되는 부모 오브젝트
        instantDongleObj.name = "Dongle" + donglePool.Count;
        Dongle instantDongle = instantDongleObj.GetComponent<Dongle>();
        instantDongle.manager = this;               // 생성된 동글 오브젝트의 매니저 초기화
        instantDongle.effect = instantEffect;        // 동글 생성하면서 바로 이펙트 변수를 생성했던 것으로 초기화
        donglePool.Add(instantDongle);
        return instantDongle;       // 반환값 동글
    }

    Dongle GetDongle()
    {
        for (int index = 0; index < donglePool.Count; index++)
        {
            poolCursor = (poolCursor + 1) % donglePool.Count;
            if (!donglePool[poolCursor].gameObject.activeSelf)          // 현재 리스트 위치에 있는 오브젝트가 비활성화 되었는지 확인
            {
                return donglePool[poolCursor];      // 탐색하여 만난 오브젝트가 비활성화라면 반환
            }
        }

        return MakeDongle();    // 모든 것이 활성화중(사용중) 이라면 return에 생성 함수 반환, 해당 함수에 이미 반환이 되고 있으므로 가능
    }

    void NextDongle()
    {

        if (isOver)
        {
            return;
        }

        lastDongle = GetDongle();         // 동글 생성 시 보관 용도로 동글 변수에 저장
        lastDongle.level = Random.Range(0, maxLevel);  // maxRange는 포함이 안되기 때문에 7까지 하고싶으면 8로 설정
        lastDongle.gameObject.SetActive(true);  // 프리팹 active 꺼놓기

        SfxPlay(Sfx.Next);
        StartCoroutine(WaitNext());
    }

    IEnumerator WaitNext()              // 동글이 비워질 때 까지 기다리는 코루틴 생성
    {
        while (lastDongle != null)      // 동글이 비워질 때까지 무한루프 생성
        {
            yield return null;          // yield 없이 돌리면 무한루프에 빠져 유니티가 멈춤 
        }

        yield return new WaitForSeconds(2.5f);

        NextDongle();                   // 기다린 후에 다음 동글 실행
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

    public void GameOver()
    {
        if (isOver)
        {
            return;
        }
        isOver = true;

        StartCoroutine(GameOverRoutine());

    }

    IEnumerator GameOverRoutine()
    {
        // 1. 장면 안에 활성화 되어있는 모든 동글 가져오기
        Dongle[] dongles = FindObjectsOfType<Dongle>();         // FindObjectsOfType<T> : 장면에 올라온 T 컴포넌트들을 탐색

        // 2. 지우기 전에 모든 동글의 물리효과 비활성화
        for (int index = 0; index < dongles.Length; index++)
        {
            dongles[index].rigid.simulated = false;
        }


        // 3. 1번의 목록을 하나씩 접근해서 지우기
        for (int index = 0; index < dongles.Length; index++)
        {
            dongles[index].Hide(Vector3.up * 100);
            yield return new WaitForSeconds(0.1f);
        }

        yield return new WaitForSeconds(1.0f);

        int maxScore = Mathf.Max(score, PlayerPrefs.GetInt("MaxScore"));    // 최고점수 비교
        PlayerPrefs.SetInt("MaxScore", maxScore);       // 최고점수 업데이트

        subScoreText.text = "점수 : " + scoreText.text;
        endGroup.SetActive(true);

        bgmPlayer.Stop();       // 게임 종료되면 배경음악도 멈추도록 Stop함수 호출
        SfxPlay(Sfx.Over);
    }

    public void Reset()
    {
        SfxPlay(Sfx.Button);
        StartCoroutine(ResetCoroutine());
    }

    IEnumerator ResetCoroutine()
    {
        yield return new WaitForSeconds(1f);
        SceneManager.LoadScene("Main");
    }

    public void SfxPlay(Sfx type)
    {
        switch (type)
        {
            case Sfx.LevelUp:
                sfxPlayer[sfxCursor].clip = sfxClip[Random.Range(0, 3)];
                break;
            case Sfx.Next:
                sfxPlayer[sfxCursor].clip = sfxClip[3];
                break;
            case Sfx.Attach:
                sfxPlayer[sfxCursor].clip = sfxClip[4];
                break;
            case Sfx.Button:
                sfxPlayer[sfxCursor].clip = sfxClip[5];
                break;
            case Sfx.Over:
                sfxPlayer[sfxCursor].clip = sfxClip[6];
                break;
        }

        sfxPlayer[sfxCursor].Play();
        sfxCursor = (sfxCursor + 1) % sfxPlayer.Length;         // 일방적으로 인덱스 숫자를 올리면 OutOfRange 에러 발생
    }

    void Update()
    {
        if (Input.GetButtonDown("Cancel"))
        {
            Application.Quit();
        }
    }

    void LateUpdate()
    {
        scoreText.text = score.ToString();
    }
}
