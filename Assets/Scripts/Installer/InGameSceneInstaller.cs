using System.Collections.Generic;
using UnityEngine;

public class InGameSceneInstaller : MonoBehaviour
{
    [SerializeField] private GameObject _inGameUIManager;
    [SerializeField] private GameObject _inGameCardManager;   

    private void Awake()
    {

        // 게임 시작 시 필요한 초기화 작업 수행
        Debug.Log("InGame scene initialized.");
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        //IReadOnlyList<short> deckIds = GameManager.Instance.PlayerDeckToCarryOver;

        //_playerCardState.InitializeDeck(deckIds);
        //_handView.Initialize();
        //_matchFlowController.Initialize(...);

        //_openingSequenceView.Play(_matchFlowController.StartFirstRound);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
