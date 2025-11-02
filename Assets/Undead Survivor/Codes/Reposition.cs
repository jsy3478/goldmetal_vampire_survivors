using UnityEngine;

/// <summary>
/// '무한 맵(Infinite Map)' 로직을 구현하는 클래스입니다.
/// 플레이어가 특정 영역("Area")을 벗어났을 때, 배경(Ground)이나 적(Enemy)을 
/// 플레이어의 이동 방향으로 재배치(Reposition)시킵니다.
/// 
/// [작동 방식]
/// 1. 이 스크립트는 'Ground' 또는 'Enemy' 태그가 붙은 오브젝트에 부착됩니다.
/// 2. 이 오브젝트에는 'Is Trigger'가 체크된 Collider2D가 있어야 합니다.
/// 3. 플레이어(Player) 오브젝트에는 'Area' 태그가 붙은 **자식 오브젝트**가 있어야 합니다.
///    (이 'Area' 오브젝트의 Collider2D가 'Ground'의 Trigger 영역을 '빠져나갈' 때 이 스크립트가 동작합니다.)
/// </summary>
public class Reposition : MonoBehaviour
{
    /// <summary>이 스크립트가 붙어있는 오브젝트의 Collider2D 컴포넌트 (Enemy 태그일 때 사용)</summary>
    Collider2D coll;

    /// <summary>
    /// [Unity 이벤트] Awake() - 스크립트가 로드될 때 1회 호출
    /// </summary>
    private void Awake()
    {
        // 컴포넌트 캐싱(Caching)
        coll = GetComponent<Collider2D>();
        //콜라이더 모든도형에 대해 
    }

    /// <summary>
    /// [Unity 이벤트] OnTriggerExit2D()
    /// 다른 Collider가 나의 Trigger 영역을 '빠져나갔을 때' 1회 호출되는 함수입니다.
    /// </summary>
    /// <param name="collision">나의 영역을 벗어난 대상의 Collider</param>
    void OnTriggerExit2D(Collider2D collision)
    {
        // 1. "Area" 태그가 아닌 대상이 벗어났다면 무시
        //    (즉, 오직 'Player'의 'Area' 콜라이더가 벗어났을 때만 반응)
        if (!collision.CompareTag("Area"))
            return;

        // 2. 위치 및 방향 정보 가져오기
        Vector3 playerPos = GameManager.instance.player.transform.position; // 플레이어 현재 위치
        Vector3 myPos = transform.position; // 나의(이 스크립트가 붙은 오브젝트) 현재 위치

        // 3. 플레이어와 나의 x, y 거리 차이(절대값) 계산
        float diffX = Mathf.Abs(playerPos.x - myPos.x);
        float diffY = Mathf.Abs(playerPos.y - myPos.y);

        // 4. 플레이어의 현재 입력 방향 (이동 방향)
        Vector3 playerDir = GameManager.instance.player.inputVec;

        // 5. 플레이어의 x, y 방향을 -1(왼쪽/아래) 또는 1(오른쪽/위)로 단순화
        //    (삼항 연산자: (조건) ? (참일 때 값) : (거짓일 때 값))
        float dirX = playerDir.x < 0 ? -1 : 1;
        float dirY = playerDir.y < 0 ? -1 : 1;

        // 6. 이 스크립트가 붙은 오브젝트의 태그(Tag)에 따라 다르게 동작
        switch (transform.tag)
        {
            // (A) 태그가 "Ground"일 경우 (배경 타일)
            case "Ground":
                // x축 거리 차이(diffX)가 y축 거리 차이(diffY)보다 크다면 (즉, 좌우로 이동 중)
                if (diffX > diffY)
                {
                    // 플레이어의 x 이동 방향(dirX)으로 2칸 * 20 유닛만큼 순간이동(Translate)
                    // (예: 플레이어가 오른쪽(dirX=1)으로 이동 중이면, 
                    //      왼쪽 끝에 있던 배경을 오른쪽 끝(현재 위치 + (40, 0, 0))으로 이동시킴)
                    // (숫자 20은 타일맵의 가로/세로 크기여야 합니다.)
                    transform.Translate(Vector3.right * dirX * 2 * 20); // (2*20은 맵 타일 크기에 맞춰야 함)
                }
                // y축 거리 차이가 더 크다면 (즉, 상하로 이동 중)
                else if (diffX < diffY)
                {
                    // 플레이어의 y 이동 방향(dirY)으로 2칸 * 20 유닛만큼 순간이동
                    transform.Translate(Vector3.up * dirY * 2 * 20);
                }
                break;

            // (B) 태그가 "Enemy"일 경우 (적 유닛)
            // (참고: 이 로직은 Enemy.cs의 OnDisable/OnEnable로 대체되거나 보완될 수 있습니다.)
            case "Enemy":
                // 이 적의 콜라이더(Collider)가 활성화되어 있다면 (즉, 죽지 않았다면)
                if (coll.enabled)
                {
                    // 플레이어의 이동 방향(playerDir)으로 20 유닛만큼 따라가고,
                    // 약간의 랜덤 위치(x, y 각각 -3 ~ +3)를 더해 재배치합니다.
                    // (즉, 화면 밖으로 너무 멀리 벗어난 적이 플레이어 근처로 다시 순간이동함)
                    transform.Translate(playerDir * 20 + new Vector3(Random.Range(-3f, 3f), Random.Range(-3f, 3f), 0));
                }
                break;
        }
    }
}
