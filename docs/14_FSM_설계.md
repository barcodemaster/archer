FSM 설계 방법 - 종합 가이드

step 1 : 이 엔티티가 할 수 있는 행동이 뭐지?
- 먼저 엔티티의 모든 행동을 나열합니다. 예를 들어 몬스터라면: 가만히 서있기, 순찰하기, 추적하기, 공격하기, 피격당하기, 죽기, 도망가기

step 2 : 동시에 할 수 있는 행동이 있나?
- 동시에 할 수 없는 행동들이 FSM의 상태 후보입니다.
- 공격하면서 이동? -> 동시에 안 됨 -> 별도 상태
- 이동하면서 피격? -> 동시에 가능하게 할 건지 결정 필요
- 만약 이동하면서 공격이 필요하다면, FSM만으로는 부족하고 병렬 FSM 또는 Behaviour Tree를 고려합니다.

step 3 : 언제 상태가 바뀌지?
- 각 상태 사이의 전환 조건을 정리합니다. 이걸 전환 테이블로 그립니다.

현재 상태		조건				다음상태
Idle			적 감지됨			Chase
Chase		사거리 안에 들어옴	Attack
Chase		적 사라짐			Idle
Attack		공격 완료			Chase
Attack 		적 사라짐			Idle
[Any] 		HP <= 0			Die

- 이 테이블을 그리면 빠진 전환을 발견할 수 있습니다. 공격 중에 적이 사라지면? 같은 엣지 케이스를 미리 잡습니다.

step 4 : 상태가 몇 개인지에 따라 구현 방식 선택

상태 수 	복잡도		추천방식
2~3개	단순			Enum + Switch
4~8개	중간			State Pattern
8개+		복잡			HFSM 또는 Behaviour Tree
데이터 주도 가변적		전환 테이블 기반

### 구현 방식 A : Enum + Switch (가장 단순)

```csharp
public class SimpleMonster : MonoBehaviour
{
  enum State { Idle, Chase,Attack,Die};
  State _state = State.Idle;

  void Update()
  {
    switch(_state)
    {
      case State.Idle:
        //순찰 로직
        if(DetectEnemy())
            ChangeState(State.Chase);
        break;

        case State.Chase:
          MoveToTarget();
          if(InAttackRange())
            ChangeState(State.Attack);
          else if(!HasTarget())
            ChangeState(State.Idle);
          break;

        case State.Attack:
          if(attackTimer <= 0)
          {
            DealDamage();
            attackTimer = attackCooldown;
            ChangeState(State.Chase);
          }
          break;
        
        case State.Die:
          //아무것도 안함
          break;
    }
  }

  void ChangeState(State newState)
  {
    switch(_state)
    {
      case State.Attack:
        ResetAttackAnimation();
        break;
    }

    _state = newState;

    switch(newState)
    {
      case State.Chase:
        PlayRunAnimation();
        break;
      case State.Die:
        PlayDeathAnimation();
        break;
    }
  }
}
```

장점: 한 파일에서 전체 흐름이 보임, 빠르게 프로토 타입 가능
단점 : 상태가 5개 넘으면 switch가 거대해짐, 상태 추가 시 기존 코드 수정 필요 (OCP 위반)

내 생각: 게임잼이나 프로토타입에서는 이걸로 시작합니다. "일단 돌아가게 만들고, 복잡해지면 리팩토링"이 실전적입니다.

---
구현 방식 B : State Pattern (현재 프로젝트 방식)

IMonsterState(인터페이스)
- MonsterIdleState
- MonsterChaseState
- MonsterAttackState
- MonsterDieState

MonsterBase (Context) -- 가지고있음 --> IMonsterState _currentState

이 방식의 핵심 설계 결정들 :

결정 1 : 상태 인스턴스르 어떻게 관리할 것인가?

방법 a) Singleton (현재 프로젝트)

```csharp
public class MonsterIdleState : IMonsterState
{
  pulbic static readonly MonsterIdleState Instance = new();
  // 상태 자체에 인스턴스 변수 없음
}
```

// 사용
monster.TransitionTo(MonsterIdleState.Instance);
- 모든 몬스터가 같은 상태 객체 공유
- 상태에 개별 몬스터 데이터를 저장할 수 없음
- 메모리 효율적

방법 b) 몬스터마다 상태 인스턴스 생성
```csharp
public class Monster : MonoBehaviour
{
  private IdleState _idle;
  private ChaseState _chase;

  void Awake()
  {
    _idle = new IdleState(this); // 각 몬스터가 자기만의 상태 보유
    _chase = new ChaseState(this);
  }
}

- 상태에 타이머, 카운터 등 개별 데이터 저장 가능
- 메모리 더 사용하지만 유연함
```

방법 c) Dictionary 캐싱
```csharp
public class StateMachine
{
  Dictionary<EState, IMonsterState> _states = new();

  public void RegisterState(EState key, IMonsterState state)
  {
    _states[key] = state;
  }

  public void TransitionTo(EState key)
  {
    _currentState?.Exit();
    _currentState = _states[key];
    _currentState.Enter();
  }
}
-Enum으로 상태를 참조하므로 타입 안정성
- 상태 등록/ 해제가 동적으로 가능

내 생각 : 상태에 데이터가 필요 없으면 Singleton, 상태별로 타이머나 누적값이 필요하면 인스턴스 방식을 씁니다.
```

결정 2 : 전환 로직을 어디에 둘 것인가?

방법 a) 상태 내부 (Push 방식, 현재 프로젝트)
```csharp
// MonsterChaseState.cs
public void Update(MonsterBase monster)
{
  if(monster.InAttackRange())
    monster.TransitionTo(MonsterAttackState.Instance); // 상태가 직접 전환
}
```
방법 b) Context(MonsterBase) 외부 (Pull 방식)
```csharp
// MonsterBase.cs
void Update()
{
  EState next = _currentState.Evaluate(this); // 상태가 "다음 상태"를 반환
  if(next != _currentEState)
    TransitionTo(next); // Context가 전환 결정
}
