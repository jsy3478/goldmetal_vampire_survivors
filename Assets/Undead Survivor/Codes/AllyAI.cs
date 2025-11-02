using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// [아군 유닛] AI 추적 및 공격 로직을 담당합니다. (Enemy.cs와 동일한 구조)
/// Targetable.cs (생명)과 Rigidbody2D (물리)에 의존합니다.
/// </summary>
public class AllyAI : MonoBehaviour
{
    [Header("기본 능력치")]
    public float speed = 2.5f;

    [Header("AI 설정")]
    /// <summary>공격할 대상의 레이어 (인스펙터에서 'Enemy'로 설정해야 함)</summary>
    public LayerMask targetLayer;
    /// <summary>적을 감지할 수 있는 최대 반경</summary>
    public float detectionRadius = 15f;
    /// <summary>AI가 새로운 타겟을 탐색하는 주기 (초). 너무 낮으면 성능 저하, 높으면 반응이 느려짐.</summary>
    private float aiUpdateFrequency = 0.5f;

    [Header("공격 설정")]
    /// <summary>'몸통 박치기' 공격의 데미지</summary>
    public float attackDamage = 1f;
    /// <summary>공격 주기 (초). 1f = 1초에 한 번 공격</summary>
    public float attackCooldown = 1f;
    /// <summary>마지막으로 공격한 시간을 저장 (쿨다운 계산용)</summary>
    private float lastAttackTime;

    // 컴포넌트 참조 (성능을 위해 Awake에서 미리 캐싱)
    private Rigidbody2D rigid;
    private SpriteRenderer spriter;
    /// <summary>AI 타겟 탐색 코루틴(Coroutine)을 제어하기 위한 변수</summary>
    private Coroutine aiCoroutine;

    // 타겟 관련
    /// <summary>현재 추적 중인 대상 (Targetable 컴포넌트)</summary>
    private Targetable currentTarget;

    /// <summary>현재 넉백 상태인지 여부. true이면 AI 이동/공격/타겟팅이 모두 중지됩니다.</summary>
    private bool isKnockedBack = false;

    /// <summary>
    /// [Unity 이벤트] Awake() - 스크립트가 처음 로드될 때 1회 호출
    /// </summary>
    void Awake()
    {
        // GetComponent<T>()는 무거운 작업이므로,
        // Update()에서 매번 호출하지 않고 Awake()에서 한 번만 호출하여 변수에 저장해 둡니다. (캐싱)
        rigid = GetComponent<Rigidbody2D>();
        spriter = GetComponent<SpriteRenderer>();
    }

    /// <summary>
    /// [Unity 이벤트] OnEnable() - 오브젝트가 활성화될 때 (풀에서 재사용될 때) 호출
    /// </summary>
    void OnEnable()
    {
        // 재사용될 때 넉백 상태를 항상 false로 초기화합니다.
        isKnockedBack = false; // ★★★ [신규] 활성화 시 넉백 상태 초기화

        // AI 타겟 탐색 코루틴을 시작합니다. (코루틴이 중복 실행되지 않도록 체크)
        if (aiCoroutine == null)
        {
            // StartCoroutine은 "UpdateTargetCoroutine" 함수를 '백그라운드'에서 
            // 독립적으로 실행시킵니다. (Update()와 별개로 동작)
            aiCoroutine = StartCoroutine(UpdateTargetCoroutine());
        }
    }

    /// <summary>
    /// [Unity 이벤트] OnDisable() - 오브젝트가 비활성화될 때 (죽거나 풀로 반납될 때) 호출
    /// </summary>
    void OnDisable()
    {
        // 오브젝트가 비활성화되면 코루틴도 멈춰야 합니다. (불필요한 연산 방지)
        if (aiCoroutine != null)
        {
            StopCoroutine(aiCoroutine);
            aiCoroutine = null;
        }
        currentTarget = null; // 타겟 정보 초기화
        rigid.linearVelocity = Vector2.zero; // 혹시 모를 잔여 속도 제거
    }

    /// <summary>
    /// [AI] 타겟 탐색 코루틴 (Coroutine)
    /// 'aiUpdateFrequency' 주기마다 가장 가까운 적을 탐색합니다. (Update()와 별도로 동작)
    /// </summary>
    IEnumerator UpdateTargetCoroutine()
    {
        // 이 오브젝트가 활성화되어 있는 동안 무한 반복
        while (gameObject.activeSelf)
        {
            // [핵심] 넉백 중이 아닐 때만 타겟을 새로 고칩니다.
            if (!isKnockedBack)
            {
                currentTarget = FindClosestTarget();
            }
            // 'aiUpdateFrequency' (예: 0.5초) 만큼 기다렸다가
            // while 루프의 처음(if (!isKnockedBack)...)으로 돌아갑니다.
            yield return new WaitForSeconds(aiUpdateFrequency);
        }
    }

    /// <summary>
    /// [AI] 'detectionRadius' 반경 내의 'targetLayer'에서 가장 가까운 타겟을 찾아 반환합니다.
    /// </summary>
    /// <returns>가장 가까운 Targetable. 못 찾으면 null 반환.</returns>
    Targetable FindClosestTarget()
    {
        float closestDistance = float.MaxValue;
        Targetable bestTarget = null;

        // 1. 유니티 물리 엔진을 사용해, 내 위치(transform.position)를 중심으로
        //    'detectionRadius' 반경 안에 있는 모든 'targetLayer'('Enemy') 콜라이더(Collider)를 검색합니다.
        Collider2D[] targetsInView = Physics2D.OverlapCircleAll(transform.position, detectionRadius, targetLayer);

        // 2. 찾은 모든 콜라이더들을 순회합니다.
        foreach (Collider2D collider in targetsInView)
        {
            // 3. 콜라이더에서 Targetable 컴포넌트를 가져옵니다. (죽었는지 확인해야 하므로)
            Targetable potentialTarget = collider.GetComponent<Targetable>();

            // 4. 대상이 존재하고, 아직 죽지 않았다면
            if (potentialTarget != null && !potentialTarget.isDead)
            {
                // 5. 대상과의 거리를 계산합니다.
                float distance = Vector3.Distance(transform.position, potentialTarget.transform.position);

                // 6. '가장 가까운 거리(closestDistance)' 기록을 갱신할 수 있다면
                if (distance < closestDistance)
                {
                    closestDistance = distance; // 거리 갱신
                    bestTarget = potentialTarget; // 타겟 갱신
                }
            }
        }
        // 7. 찾은 'bestTarget' (가장 가까운 대상)을 반환합니다. (못 찾았다면 null이 반환됨)
        return bestTarget;
    }

    /// <summary>
    /// [Unity 이벤트] FixedUpdate() - 고정된 물리 프레임마다 호출 (기본 0.02초)
    /// Rigidbody(rigid)를 이용한 이동은 FixedUpdate에서 처리해야 안정적입니다.
    /// </summary>
    void FixedUpdate()
    {
        // [핵심] 넉백 상태일 때는 AI 이동 로직을 실행하지 않습니다.
        if (isKnockedBack)
        {
            return; // 넉백 코루틴이 속도(velocity)를 제어하므로, AI는 여기서 멈춰야 함.
        }

        // 1. 타겟이 없으면(null) 그 자리에 멈춥니다.
        if (currentTarget == null)
        {
            rigid.linearVelocity = Vector2.zero; // 이동 속도를 0으로
            return;
        }

        // 2. 타겟을 향하는 방향 벡터를 계산합니다. (목표 위치 - 현재 위치)
        Vector2 dirVec = currentTarget.transform.position - transform.position;
        // 3. 이번 물리 프레임에 이동할 '속도 벡터(nextVec)'를 계산합니다.
        Vector2 nextVec = dirVec.normalized * speed * Time.fixedDeltaTime;

        // 4. Rigidbody의 위치를 이동시킵니다.
        rigid.MovePosition(rigid.position + nextVec);
        // 5. MovePosition은 물리적 힘이 아니므로, 혹시 모를 다른 충돌 속도를 0으로 만듭니다.
        rigid.linearVelocity = Vector2.zero;
    }

    /// <summary>
    /// [Unity 이벤트] LateUpdate() - 모든 Update()가 끝난 후 호출
    /// </summary>
    void LateUpdate()
    {
        // [핵심] 넉백 중이 아닐 때만 스프라이트 반전을 실행합니다.
        if (isKnockedBack) return;

        // 타겟이 없으면 방향 전환을 할 필요가 없습니다.
        if (currentTarget == null) return;

        // 타겟의 x좌표가 내 x좌표보다 작으면 (타겟이 왼쪽에 있으면)
        // spriter.flipX = true (스프라이트를 좌우 반전시킴)
        spriter.flipX = currentTarget.transform.position.x < rigid.position.x;
    }

    /// <summary>
    /// [공격] 다른 Collider2D와 계속 부딪히고 있는 동안 매 프레임 호출됩니다.
    /// (주의: 'Is Trigger'가 체크 해제된 Collider끼리 부딪혀야 호출됩니다.)
    /// </summary>
    /// <param name="collision">나와 부딪힌 대상의 물리 정보</param>
    void OnCollisionStay2D(Collision2D collision)
    {
        // 1. 타겟이 없거나, 쿨다운 중이거나, 넉백 중이면 공격하지 않습니다.
        if (currentTarget == null || Time.time < lastAttackTime + attackCooldown || isKnockedBack)
            return;

        // 2. 부딪힌 대상이 내 '현재 타겟(currentTarget)'이 맞는지 확인합니다.
        if (collision.gameObject == currentTarget.gameObject)
        {
            // 3. ★★★ [수정] 부딪힌 대상(currentTarget)의 Targetable 스크립트에 있는
            //    TakeDamage() 함수를 호출하여 데미지를 줍니다.
            //    이때 '나 자신(transform)'을 넘겨주어, 상대방이 넉백 방향을 계산할 수 있게 합니다.
            currentTarget.TakeDamage(attackDamage, transform);

            // 4. 마지막 공격 시간을 현재 시간으로 갱신 (쿨다운 시작)
            lastAttackTime = Time.time;
        }
    }

    // -----------------------------------------------------------------
    // ★★★ [신규] 넉백 수신 함수 ★★★
    // -----------------------------------------------------------------

    /// <summary>
    /// [넉백] Targetable.cs로부터 넉백 명령을 받아 코루틴을 실행합니다.
    /// (이 함수는 public이므로 Targetable.cs에서 호출할 수 있습니다.)
    /// </summary>
    public void ApplyKnockback(Vector2 knockbackDir, float power, float duration)
    {
        // 이미 넉백 중이라면 중복 실행을 방지합니다.
        if (isKnockedBack) return;

        // 실제 넉백 처리를 하는 코루틴을 시작시킵니다.
        StartCoroutine(KnockbackRoutine(knockbackDir, power, duration));
    }

    /// <summary>
    /// [넉백] 실제 넉백을 처리하는 코루틴 (Coroutine)
    /// </summary>
    private IEnumerator KnockbackRoutine(Vector2 knockbackDir, float power, float duration)
    {
        // 1. 넉백 상태로 변경 (-> AI 로직 멈춤)
        isKnockedBack = true;

        // 2. ★★★ [수정] .velocity 대신 .linearVelocity 사용 ★★★
        // Rigidbody의 속도(linearVelocity)에 넉백 방향*힘을 순간적으로 적용합니다.
        rigid.linearVelocity = knockbackDir * power;

        // 3. 지정된 넉백 시간(duration)만큼 '여기서 대기'합니다.
        yield return new WaitForSeconds(duration);

        // 4. ★★★ [수정] .velocity 대신 .linearVelocity 사용 ★★★
        // 넉백 시간이 끝나면 속도를 0으로 초기화합니다.
        rigid.linearVelocity = Vector2.zero;

        // 5. 넉백 상태를 해제합니다. (-> AI 로직이 다시 정상 작동 시작)
        isKnockedBack = false;
    }
}
