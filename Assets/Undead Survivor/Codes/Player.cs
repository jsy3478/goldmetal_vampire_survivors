using UnityEngine;
using UnityEngine.InputSystem; // 유니티의 새로운 인풋 시스템(Input System)을 사용하기 위한 네임스페이스입니다.

/// <summary>
/// 플레이어의 입력을 받고, 이동 및 애니메이션을 처리합니다.
/// 이 스크립트는 'Player Input' 컴포넌트와 'Animator' 컴포넌트에 의존합니다.
/// </summary>
public class Player : MonoBehaviour
{
    [Header("입력 및 속도")]
    /// <summary>
    /// 'Player Input' 컴포넌트로부터 받은 현재 입력 방향 (x, y)
    /// (예: 키보드 'W'를 누르면 (0, 1), 'A'를 누르면 (-1, 0))
    /// </summary>
    public Vector2 inputVec;
    /// <summary>플레이어의 이동 속도</summary>
    public float speed;

    // [private] 이 스크립트 내부에서만 사용할 컴포넌트 참조 변수들
    /// <summary>플레이어의 물리(Physics) 컴포넌트 (이동 처리에 사용)</summary>
    Rigidbody2D rigid;
    /// <summary>플레이어의 스프라이트(이미지) 컴포넌트 (좌우 반전에 사용)</summary>
    SpriteRenderer spriter;
    /// <summary>플레이어의 애니메이션 제어기 (애니메이션 상태 변경에 사용)</summary>
    Animator anim;

    /// <summary>
    /// [Unity 이벤트] Awake() - 스크립트가 로드될 때 1회 호출
    /// </summary>
    private void Awake()
    {
        // GetComponent<T>()는 비용이 많이 드는 함수이므로,
        // 매번 호출하지 않고 Awake()에서 한 번만 호출하여 변수에 저장해 둡니다. (캐싱, Caching)
        rigid = GetComponent<Rigidbody2D>();
        spriter = GetComponent<SpriteRenderer>();
        anim = GetComponent<Animator>();

        // 플레이어 초기 속도 설정
        speed = 8;
    }

    /// <summary>
    /// [Input System 이벤트] 'Player Input' 컴포넌트가 'Move' 액션(Action)을 감지했을 때 호출됩니다.
    /// (이 함수는 유니티 이벤트 시스템에 의해 자동으로 호출됩니다.)
    /// </summary>
    /// <param name="value">입력 값 (Vector2)</param>
    void OnMove(InputValue value)
    {
        // 입력된 Vector2 값을 inputVec 변수에 저장합니다.
        // (키를 떼면 (0, 0)이 다시 입력됩니다.)
        inputVec = value.Get<Vector2>();
    }

    /// <summary>
    /// [Unity 이벤트] FixedUpdate() - 고정된 물리 프레임마다 호출 (기본 0.02초)
    /// Rigidbody를 이용한 이동/충돌 처리는 이 함수에서 수행해야 안정적입니다.
    /// </summary>
    private void FixedUpdate()
    {
        // 1. 이번 프레임에 이동할 '다음 위치 벡터(nextVec)'를 계산합니다.
        //    (방향 * 속도 * 고정 프레임 시간)
        //    Time.fixedDeltaTime을 곱해야 프레임 속도와 관계없이 일정한 속도로 이동합니다.
        Vector2 nextVec = inputVec * speed * Time.fixedDeltaTime;
        //기본 벡터+프레임 속도 고정 nextVec

        // 2. Rigidbody의 현재 위치(rigid.position)에 nextVec를 더하여 물리적으로 이동시킵니다.
        rigid.MovePosition(rigid.position + nextVec);
    }

    /// <summary>
    /// [Unity 이벤트] LateUpdate() - 모든 Update() 계열 함수가 실행된 후, 프레임이 끝나기 직전에 호출됩니다.
    /// (이동(FixedUpdate)이 끝난 후 시각적(Visual) 처리를 하기에 적합합니다.)
    /// </summary>
    //after update fun
    private void LateUpdate()
    {
        // 1. 애니메이터(Animator)의 "Speed" 파라미터 값을 inputVec의 크기(magnitude)로 설정합니다.
        //    inputVec.magnitude는 벡터의 길이(크기)를 반환합니다. 
        //    (예: (0, 0)이면 0 (정지), (1, 0)이면 1 (이동))
        //    -> Animator 뷰에서 "Speed" 파라미터가 0보다 크면 'Run' 애니메이션을 재생하도록 설정되어 있을 것입니다.
        anim.SetFloat("Speed", inputVec.magnitude);
        //변경하고 싶은 값 , 변수 .magnitude는 그냥 크 

        // 2. x축 입력이 있을 때 (0이 아닐 때)
        if (inputVec.x != 0)
        {
            // 3. 입력 방향에 따라 스프라이트를 좌우로 뒤집습니다(flipX).
            //    inputVec.x가 0보다 작으면(왼쪽) true -> 스프라이트 반전
            //    inputVec.x가 0보다 크면(오른쪽) false -> 스프라이트 원본
            spriter.flipX = inputVec.x < 0;
        }
        //flip x
    }
}
