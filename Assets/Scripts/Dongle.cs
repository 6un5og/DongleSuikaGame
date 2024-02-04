using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

public class Dongle : MonoBehaviour
{
    public GameManager manager;
    public ParticleSystem effect;
    public bool isDrag;
    public int level;
    public bool isMerge;
    public bool isAttach;

    public Rigidbody2D rigid;
    CircleCollider2D circle;
    Animator anim;
    SpriteRenderer spriteRenderer;

    float deadTime;

    void Awake()
    {
        rigid = GetComponent<Rigidbody2D>();
        circle = GetComponent<CircleCollider2D>();
        anim = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    void OnEnable()
    {
        anim.SetInteger("Level", level);
    }

    void OnDisable()        // 오브젝트 풀링 비활성화 시 재사용을 위해 원래 가지고 있는 모든 정보를 초기화할 함수 필요 (자동 사용 됨)
    {
        // 동글 속성 초기화
        level = 0;
        isDrag = false;
        isMerge = false;
        isAttach = false;

        // 동글 트랜스폼 초기화
        transform.localPosition = Vector3.zero;             // 동글이 모두 동글 그룹 안에 들어가 있기 때문에 position이 아닌 localPosition
        transform.localRotation = quaternion.identity;      // rotation은 쿼터니언, 쿼터니언 초기화는 identity
        transform.localScale = Vector3.zero;                // 레벨에 비례해 크기가 다르기 떄문에 크기도 초기화

        // 동글 물리 초기화
        rigid.simulated = false;                // 프리펩에서 껐던거처럼 꺼주기
        rigid.velocity = Vector2.zero;          // 리지드바디2D 쓰기 때문에 Vector2 사용
        rigid.angularVelocity = 0;              // 회전속도값은 float값이기 때문에 0
        circle.enabled = true;                  // 숨겨진 동글은 circleCollider 비활성화 했었기 떄문에 활성화해주기
    }

    // Update is called once per frame
    void Update()
    {
        if (isDrag)
        {
            Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);     // Input.mousePosition 은 스크린좌표계 Vector3 는 월드좌표계
            // 공이기 때문에 반지름 구해서 더하고 빼주기 transform(자신의)localScale(크기)x(x값) /(나누기) 2(반)
            float leftBorder = -4.2f + transform.localScale.x / 2f;
            float rightBorder = 4.2f - transform.localScale.x / 2f;

            if (mousePos.x < leftBorder)
            {
                mousePos.x = leftBorder;
            }
            else if (mousePos.x > rightBorder)
            {
                mousePos.x = rightBorder;
            }

            mousePos.y = 8;
            mousePos.z = 0;
            transform.position = Vector3.Lerp(transform.position, mousePos, 0.2f);
        }
    }

    public void Drag()
    {
        isDrag = true;
    }

    public void Drop()
    {
        isDrag = false;
        rigid.simulated = true;         // 손을 뗐을 때 물리적용 받게 하기
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        StartCoroutine(AttachRoutine());
    }

    IEnumerator AttachRoutine()
    {
        if (isAttach)
        {
            yield break;    // 코루틴에서 탈출은 return 이 아닌 break
        }

        isAttach = true;

        manager.SfxPlay(GameManager.Sfx.Attach);        // 충돌음의 주기

        yield return new WaitForSeconds(0.2f);
        isAttach = false;
    }

    void OnCollisionStay2D(Collision2D collision)       // 물리적 충돌중일 때 계속 실행되는 함수
    {
        if (collision.gameObject.tag == "Dongle")       // 부딪히는 오브젝트의 태그가 동글일 때
        {
            Dongle other = collision.gameObject.GetComponent<Dongle>();     // 오브젝트 동글의 컴포넌트 가져오기

            // 조건 4개 모두 충족 (본인 오브젝트의 레벨과 충돌한 오브젝트의 레벨이 같을 시, 내가 합치고 있지 않을 시, 상대 오브젝트가 합치고 있지 않을 시, 레벨이 7보다 작아야 함)
            if (level == other.level && !isMerge && !other.isMerge && level < 7)
            {
                // 나와 상대편 위치 가져오기
                float meX = transform.position.x;
                float meY = transform.position.y;
                float otherX = other.transform.position.x;
                float otherY = other.transform.position.y;
                // 1. 내가 아래에 있을 때
                // 2. 동일한 높이일 때, 내가 오른쪽에 있을 때
                if (meY < otherY || (meY == otherY && meX > otherX))        // 동일한 높이 + 오른쪽에 있다 두 조건을 묶는 소괄호 추가
                {
                    // 나는 레벨업하고 상대방은 숨기기
                    other.Hide(transform.position);     // 상대방이 내쪽으로 와서 합치기 때문에 변수에 내 위치 입력

                    LevelUp();
                }
            }
        }
    }

    public void Hide(Vector3 targetPos)
    {
        isMerge = true;

        rigid.simulated = false;
        circle.enabled = false;

        if (targetPos == Vector3.up * 100)          // 게임 오버에도 이펙트가 나오도록 로직 추가
        {
            EffectPlay();
        }

        StartCoroutine(HideRoutine(targetPos));
    }

    IEnumerator HideRoutine(Vector3 targetPos)      // 이동하는 코루틴, 성장하는 상대에게 이동하므로 Vector3 매개변수 추가
    {
        int frameCount = 0;

        while (frameCount < 20)         // while문을 update처럼 로직을 실행하여 이동
        {
            frameCount++;
            if (targetPos != Vector3.up * 100)
            {
                transform.position = Vector3.Lerp(transform.position, targetPos, 0.5f);
            }
            else if (targetPos == Vector3.up * 100)
            {
                transform.localScale = Vector3.Lerp(transform.localScale, Vector3.zero, 0.2f);  // 스케일 없애기
            }

            yield return null;          // 한 프레임씩 끊어주기
        }

        manager.score += (int)Mathf.Pow(2, level);      // Mathf.Pow는 float형 변환이기 때문에 명시적으로 변환(int)

        isMerge = false;                // 잠금 해제
        gameObject.SetActive(false);    // 숨기기
    }

    void LevelUp()
    {
        isMerge = true;

        // 레벨업 중에 방해가 될 수 있는 물리속도 제거
        rigid.velocity = Vector2.zero;
        rigid.angularVelocity = 0;

        StartCoroutine(LevelUpRoutine());
    }

    IEnumerator LevelUpRoutine()
    {
        yield return new WaitForSeconds(0.2f);

        anim.SetInteger("Level", level + 1);        // 변수 값에 ++level을 해버리면 애니메이션 시간이 부족함
        EffectPlay();
        manager.SfxPlay(GameManager.Sfx.LevelUp);

        yield return new WaitForSeconds(0.3f);
        level++;

        manager.maxLevel = Mathf.Max(level, manager.maxLevel);  // 인자값 중 최대값을 반환해주는 함수(순서 상관 없음)

        isMerge = false;
    }

    void OnTriggerStay2D(Collider2D collision)
    {
        if (collision.tag == "Finish")
        {
            deadTime += Time.deltaTime;         // 각 프레임의 시간을 계속 더함

            if (deadTime > 2)
            {
                spriteRenderer.color = new Color(0.9f, 0.2f, 0.2f);
            }
            if (deadTime > 5)
            {
                manager.GameOver();
            }
        }
    }

    void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.tag == "Finish")
        {
            deadTime = 0;
            spriteRenderer.color = Color.white;
        }
    }

    void EffectPlay()           // 파티클 위치와 크기 보정 함수
    {
        effect.transform.position = transform.position;
        effect.transform.localScale = transform.localScale;
        effect.Play();          // 파티클 함수 Play() 로 실행
    }
}
