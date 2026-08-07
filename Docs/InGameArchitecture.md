# In-Game Architecture

## 1. 결론

이 프로젝트에는 엄격한 MVC보다 다음 조합이 적합하다.

- 기능 단위 폴더 구조
- `State - Controller - View` 책임 분리
- 씬 의존성을 한 곳에서 연결하는 Composition Root
- 서버 코드를 게임 코드로부터 숨기는 Gateway
- 유닛은 과도하게 분해하지 않는 실용적인 Component 구조

핵심 의존 방향은 다음과 같다.

```text
Unity Input / Server Packet
            |
            v
         Gateway
            |
            v
     MatchFlowController
       /             \
      v               v
Phase Controllers   Runtime State
      |               |
      +-------> Views <+
```

`View`는 게임 규칙을 결정하지 않는다. `State`는 UI와 네트워크를 모른다.
`Controller`만 여러 객체를 조합해 하나의 유스케이스를 수행한다.

## 2. 명명 규칙

| 접미사 | 의미 | 허용되는 책임 | 금지되는 책임 |
|---|---|---|---|
| `Installer` | 씬 조립 지점 | 참조 검사, 의존성 연결, 초기화 순서 | 게임 규칙, 타이머, 패킷 생성 |
| `Controller` | 유스케이스와 흐름 | 상태 변경, 입력 처리, 다른 객체 호출 | UI 세부 배치, 패킷 직렬화 |
| `State` | 런타임 데이터 | 데이터 보관, 작은 불변식 검사, 변경 이벤트 | MonoBehaviour 탐색, UI 제어, 송신 |
| `View` | 표시와 사용자 입력 | 텍스트/이미지 갱신, 애니메이션, 입력 이벤트 | 코스트 차감, 카드 제거, 승패 판정 |
| `Gateway` | 외부 시스템 경계 | 패킷 변환, 송수신, 연결 상태 | 게임 규칙, UI 제어 |
| `Factory` | 런타임 객체 생성 | 생성과 초기 설정 | 라운드 진행, 승패 판정 |
| `Registry` | 런타임 객체 목록 | 등록, 해제, 검색 | 객체 생성, 게임 단계 전환 |
| `Database` | 정적 원본 데이터 | ID로 ScriptableObject 조회 | 플레이어 손패 상태 |

`Manager`는 책임을 읽을 수 없으므로 새 코드에서는 사용하지 않는다.
전역 생명주기를 가진 `GameManager`, `NetworkManager`만 예외로 둘 수 있다.

## 3. 씬의 최상위 구조

```text
InGameScene
|
+-- InGameSceneInstaller
|   +-- 모든 씬 컴포넌트 참조 연결
|   +-- 초기화 순서 보장
|   +-- 참조 누락 검사
|
+-- MatchFlowController
|   +-- Opening -> Preparation -> Waiting -> Combat -> Result
|   +-- 라운드 전환과 승패 결정
|
+-- PreparationPhaseController
|   +-- 준비 시간
|   +-- 카드 사용
|   +-- 수비 배치 완료
|   +-- 준비 완료 요청
|
+-- CombatPhaseController
|   +-- 누적 공격 카드 스폰
|   +-- 전투 시작/종료
|   +-- 공격 유닛이 모두 사라졌는지 확인
|
+-- PlayerCardState
|   +-- 덱, 손패, 버림 더미
|   +-- 선택된 공격/수비 카드
|
+-- MatchState
|   +-- 현재 Phase, Round, Cost
|   +-- 양쪽 Life
|
+-- InGameNetworkGateway
|   +-- 게임 명령을 C_ 패킷으로 변환
|   +-- S_ 패킷을 타입 이벤트로 변환
|
+-- Views
    +-- OpeningSequenceView
    +-- BattleHudView
    +-- HandView
    +-- UsedCardListView
    +-- CardConfirmPopupView
    +-- ObjectiveHealthView
```

이 구조는 상속 트리가 아니다. `InGameSceneInstaller`가 객체를 소유하는 것도 아니다.
씬에 존재하는 컴포넌트의 참조를 연결하는 Composition Root다.

## 4. 클래스별 계약

### InGameSceneInstaller

**역할**

- 인게임 씬에서 유일한 조립 지점
- Inspector에 연결된 참조가 모두 존재하는지 검증
- State, View, Controller, Gateway의 초기화 순서 결정
- 서버 패킷 이벤트 구독과 해제를 한 위치에서 관리

**입력**

- `GameManager`가 전달한 플레이어 덱 ID
- Inspector의 씬 컴포넌트 참조

**출력**

- 각 객체에 명시적인 참조 전달
- 최초 오프닝 시작

**하지 않는 일**

- 코스트 차감
- 카드 사용 가능 여부 판단
- 전투 종료 판정
- UI 텍스트 직접 수정

### MatchState

**보관 정보**

- `Phase`
- `CurrentRound`, `MaxRound`
- `CurrentCost`, `MaxCost`
- `PlayerLife`, `OpponentLife`
- 게임 종료 여부

**제공 기능**

- `StartRound(round)`
- `TrySpendCost(amount)`
- `ApplyObjectiveDamage(owner, amount)`
- `EnterPhase(phase)`

**발행 이벤트**

- `PhaseChanged`
- `RoundChanged`
- `CostChanged`
- `LifeChanged`

이 클래스는 가능한 한 순수 C# 클래스로 둔다. Unity API가 필요하지 않기 때문이다.

### MatchFlowController

현재 `GameTurnManager`가 담당하는 여러 책임 중 전체 경기 흐름만 가져온다.

**제공 기능**

- `StartMatch()`
- `HandleServerTurnStarted(round, seconds)`
- `BeginCombat()`
- `HandleAttackUnitReachedDestination(owner)`
- `HandleCombatFinished()`
- `FinishMatch(result)`

**받는 정보**

- 서버의 턴 시작/종료 이벤트
- `CombatPhaseController`의 목적지 도달 및 전투 완료 이벤트

**보내는 정보**

- `MatchState` 변경 명령
- 준비/전투 Controller 시작 명령
- 결과 View 표시 명령

**하지 않는 일**

- 손패 리스트 직접 변경
- 유닛 생성
- 패킷 생성
- 텍스트 변경

### PlayerCardState

현재 `InGameCardManager`의 목표 이름이다.

**보관 정보**

- draw pile
- hand
- discard pile
- 누적된 공격 카드
- 배치가 완료된 수비 카드

**제공 기능**

- `InitializeDeck(cardIds)`
- `DrawInitialHand(count)`
- `DrawCards(count)`
- `ContainsInHand(card)`
- `TryRemoveFromHand(card)`
- `RegisterAttackCard(card)`
- `RegisterDefenseCard(card)`
- `PrepareNextDrawCycle()`

**발행 이벤트**

- `HandChanged(IReadOnlyList<CardData>)`
- 필요하다면 `CardDrawn(CardData)`

외부에는 수정 가능한 `List<CardData>`를 반환하지 않고 `IReadOnlyList<CardData>`를 반환한다.

### PreparationPhaseController

현재 `BattlePreparationController`의 목표 이름이다.

**보관 정보**

- 준비 단계가 진행 중인지
- 남은 준비 시간
- 턴 종료 요청 여부
- 수비 배치 완료 대기 여부
- 상대가 누적한 공격 카드

**제공 기능**

- `Begin(round, duration)`
- `RequestReady()`
- `TryUseCard(card)`
- `HandleOpponentCardSelected(cardId)`
- `HandleOpponentDefensePlaced(cardId, position)`

**받는 정보**

- `HandView.CardUseRequested`
- 턴 종료 버튼 입력
- Gateway의 상대 카드/배치 이벤트

**보내는 정보**

- `PlayerCardState`에 카드 제거/등록
- `MatchState`에 코스트 차감
- `DefensePlacementController`에 배치 시작 요청
- `InGameNetworkGateway`에 카드 선택/배치/준비 완료 명령
- View에 상호작용 잠금 및 표시 갱신

**하지 않는 일**

- 패킷 클래스 직접 생성
- `NetworkManager.Instance.Send` 직접 호출
- 다른 씬 컴포넌트를 `Find`하거나 싱글턴으로 조회

### CombatPhaseController

현재 `CombatRoundManager`의 목표 이름이다.

**제공 기능**

- `Begin(playerCards, opponentCards)`
- 각 진영 공격 유닛 순차 스폰
- 활성 공격 유닛이 0이 되면 완료 이벤트 발행

**협력 객체**

- `AttackUnitFactory`
- `AttackUnitRegistry`
- 양쪽 `WaypointPath`

공격 카드의 소유자, 경로, 목적지 도달 이벤트를 `AttackUnitFactory`에 전달한다.

### DefensePlacementController

현재 `DefensePlacementManager`의 목표 이름이다.

**제공 기능**

- `Begin(card)`
- 마우스 위치의 배치 가능 여부 표시
- 클릭 시 배치 확정
- 원격 수비 유닛 배치

**협력 객체**

- `PlacementArea`
- `DefenseUnitFactory`
- `HandView`가 아닌 `PreparationPhaseController`

배치 가능 영역 판정은 이 클래스에 남겨도 된다. 생성 코드가 커지면
`DefenseUnitFactory`만 별도로 추출한다.

### InGameNetworkGateway

**송신 API**

- `SendTurnStartReady()`
- `SendTurnEnd()`
- `SendAttackCardSelected(cardId)`
- `SendDefenseUnitPlaced(cardId, position)`

**수신 이벤트**

- `TurnStarted(round, duration)`
- `OpponentAttackCardSelected(cardId)`
- `BothPlayersReadyForCombat`
- `OpponentDefenseUnitPlaced(cardId, position)`
- 향후 `LifeUpdated`, `GameFinished`

게임 Controller는 `C_`, `S_`, `Serialize()`를 알지 않는다.
`PacketHandler`도 씬의 Controller를 직접 호출하지 않고 Gateway의 수신 경계에 전달한다.

### HandView

현재 `InGameHandUI`의 목표 이름이다.

**표시 기능**

- `ShowCards(cards, availableCost)`
- `HideImmediately()`
- `SetInteractionEnabled(enabled)`
- `SetAvailableCost(cost)`
- 카드 풀링과 부채꼴 애니메이션

**입력 이벤트**

- `CardUseRequested(CardData, CardViewHandle)`

손패가 실제로 존재하는지, 코스트를 차감할지는 판단하지 않는다.
Controller가 성공/실패를 알려주면 카드 UI를 제거하거나 원위치로 복구한다.

### BattleHudView

현재 `InGameUIManager`에서 HUD에 해당하는 부분이다.

**표시 기능**

- `SetPreparationTime(seconds)`
- `SetCost(cost)`
- `SetRound(round)`
- `SetLives(player, opponent)`

버튼 입력은 `ReadyRequested` 이벤트로 내보낸다.

### UsedCardListView

현재 `InGameUIManager.AddUsedCardToInfoPanel` 계열의 목표 위치다.

**표시 기능**

- `AddAttackCard(card)`
- `AddDefenseCard(card)`
- `Clear()`

카드 사용 성공 여부는 판단하지 않는다.

### CardUI

이름을 `CardView`로 바꾸는 것이 최종 목표지만 로비에서도 공유하므로 나중에 바꿔도 된다.

**역할**

- 카드 한 장의 텍스트와 이미지 표시
- hover/drag 이벤트 전달
- 사용 가능 시각 효과 표시

`HandView.Instance`를 호출하지 않는다. 생성 또는 바인딩 시 입력 수신자를 전달받는다.

### AttackUnit / DefenseUnit

포트폴리오 범위에서는 하나의 MonoBehaviour 안에 이동, 전투 상태, 간단한 시각 애니메이션을
함께 두어도 된다. 유닛을 Model/View/FSM 세 파일로 나누면 현재 규모에서는 탐색 비용이 더 크다.

- `AttackUnit`: 경로 이동, 체력, 피해, 목적지 도달
- `DefenseUnit`: 타겟 선택, 사격, 추격, 복귀
- `AttackUnitRegistry`: 활성 공격 유닛 등록과 검색
- `WaypointPath`: 경로점 제공
- `WorldHealthBar`: 월드 HP 표시

단, `DefenseUnit`이 `GameTurnManager.Instance`나
`DefensePlacementManager.Instance`를 조회하지 않도록 초기화 시 필요한 함수 또는 참조를 받는다.

## 5. 한 라운드의 데이터 흐름

### 서버가 라운드를 시작할 때

```text
S_TurnStart
-> PacketHandler
-> InGameNetworkGateway.TurnStarted
-> MatchFlowController
-> MatchState.StartRound
-> PlayerCardState.DrawCards
-> HandView.ShowCards
-> PreparationPhaseController.Begin
```

### 공격 카드를 사용할 때

```text
CardUI drag
-> HandView.CardUseRequested
-> PreparationPhaseController.TryUseCard
-> MatchState.TrySpendCost
-> PlayerCardState.TryRemoveFromHand
-> PlayerCardState.RegisterAttackCard
-> UsedCardListView.AddAttackCard
-> InGameNetworkGateway.SendAttackCardSelected
-> HandView.CommitCardUse
```

### 수비 카드를 사용할 때

```text
CardUI drag
-> PreparationPhaseController.TryUseCard
-> DefensePlacementController.Begin
-> 배치 위치 클릭
-> DefenseUnitFactory.Create
-> PlayerCardState.RegisterDefenseCard
-> UsedCardListView.AddDefenseCard
-> InGameNetworkGateway.SendDefenseUnitPlaced
```

### 전투가 끝날 때

```text
AttackUnitRegistry.ActiveCount == 0
-> CombatPhaseController.CombatFinished
-> MatchFlowController
-> 승패 조건 검사
-> InGameNetworkGateway.SendTurnStartReady
-> 다음 S_TurnStart 대기
```

## 6. 싱글턴 기준

### 유지 가능

- `GameManager`: 씬 사이에서 덱과 매칭 결과를 전달하는 앱 전역 객체
- `NetworkManager`: 연결과 세션이 씬보다 오래 유지되는 앱 전역 객체
- 패킷 생성 코드의 `PacketManager`: 네트워크 라이브러리 내부 전역 레지스트리

### 제거 대상

- `GameTurnManager`
- `InGameCardManager`
- `InGameUIManager`
- `InGameHandUI`
- `BattlePreparationController`
- `DefensePlacementManager`
- `CombatRoundManager`

씬 안에 하나뿐이라는 이유만으로 싱글턴일 필요는 없다. 이 객체들은
`InGameSceneInstaller`가 Inspector 참조로 연결한다.

## 7. 현재 코드의 우선 수정 순서

1. `InGameSceneInstaller`를 만들고 런타임 `AddComponent`를 제거한다.
2. `HandView`와 팝업에서 `InGameHandUI.Instance` 접근을 제거한다.
3. `PreparationPhaseController`에 State, View, Placement, Gateway를 주입한다.
4. `MatchState`를 만들고 `GameTurnManager`의 숫자 상태와 판정을 이동한다.
5. `MatchFlowController`에서 다른 씬 싱글턴 접근을 제거한다.
6. `PacketHandler -> Gateway -> Controller` 수신 경계를 만든다.
7. 이름을 책임에 맞게 변경하고 씬 오브젝트 이름도 맞춘다.
8. 마지막에 서버 패킷을 추가한다.

각 단계가 끝날 때마다 Unity 컴파일과 한 라운드 수동 테스트를 수행한다.
모든 클래스를 한 번에 옮기면 문제 발생 시 어느 이동에서 깨졌는지 찾기 어렵다.
