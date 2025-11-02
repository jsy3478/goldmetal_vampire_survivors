using UnityEngine;

/// <summary>
/// [적 스포너]
/// 'spawnPoint'들의 위치를 기준으로, 주기적으로 적 유닛을 스폰(Spawn)합니다.
/// GameManager(PoolManager)에 의존합니다.
/// </summary>
public class Spawner : MonoBehaviour
{
    /// <summary>
    /// 적이 스폰될 위치(Transform)들의 배열.
    /// (Awake에서 이 오브젝트의 모든 자식(Children) 오브젝트들로 자동 설정됩니다.)
    /// </summary>
    public Transform[] spawnPoint;

    /// <summary>스폰 주기를 계산하기 위한 내부 타이머</summary>
    float timer;

    /// <summary>
    /// [Unity 이벤트] Awake() - 스크립트가 로드될 때 1회 호출
    /// </summary>
    void Awake()
    {
        // GetComponentsInChildren<Transform>()은
        // '나 자신'과 '나의 모든 자식'들의 Transform 컴포넌트를 배열로 가져옵니다.
        // (Hierarchy 뷰에서 이 Spawner 오브젝트 밑에 빈 오브젝트들을 넣어두면,
        //  그 위치들이 자동으로 spawnPoint 배열에 등록됩니다.)
        spawnPoint = GetComponentsInChildren<Transform>();
    }

    /// <summary>
    /// [Unity 이벤트] Update() - 매 프레임마다 호출
    /// </summary>
    void Update()
    {
        // 1. 타이머 시간에 프레임 시간을 더합니다.
        timer += Time.deltaTime;

        // 2. 타이머가 4초(4.0f)를 넘어가면 (스폰 주기)
        if (timer > 4.0f)
        {
            Spawn();   // 3. 적을 스폰합니다.
            timer = 0; // 4. 타이머를 0으로 초기화합니다.
        }
    }

    /// <summary>
    /// PoolManager에서 적 유닛을 가져와 스폰합니다.
    /// </summary>
    void Spawn()
    {
        // 1. [핵심] GameManager의 인스턴스(instance)를 통해 PoolManager(Pool)에 접근,
        //    Get() 함수를 호출하여 오브젝트를 가져옵니다.
        //    (Random.Range(0, 2) = PoolManager의 prefabs 배열 0번 또는 1번을 랜덤으로 가져옴)
        GameObject enemy = GameManager.instance.Pool.Get(Random.Range(0, 2));
        //instance하는김에 데이터도 받아오기 

        // 2. 스폰 위치를 랜덤으로 설정합니다.
        //    (spawnPoint[0]은 '나 자신'의 Transform이므로, 
        //     '1'부터 'spawnPoint.Length' 전까지의 자식들 중에서 랜덤으로 고릅니다.)
        enemy.transform.position = spawnPoint[Random.Range(1, spawnPoint.Length)].position;
        //자기자신도 컴포넌트에 포함되서 1부터
    }
}
