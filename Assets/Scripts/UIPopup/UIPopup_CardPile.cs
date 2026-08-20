using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public enum CardPileViewType
{
    DrawPile,
    DiscardPile
}

public sealed class UIPopup_CardPile : UIPopup
{
    private readonly List<GameObject> _cardRoots = new List<GameObject>();
    private readonly Dictionary<CardType, Button> _filterButtons = new Dictionary<CardType, Button>();

    [SerializeField] private CanvasGroup _canvasGroup;
    [SerializeField] private RectTransform _content;
    [SerializeField] private TextMeshProUGUI _titleText;
    [SerializeField] private TextMeshProUGUI _countText;
    [SerializeField] private TextMeshProUGUI _emptyText;
    [SerializeField] private GameObject _cardPrefab;
    [SerializeField] private Button _btnAllCards;
    [SerializeField] private Button _btnAttackCards;
    [SerializeField] private Button _btnDefenseCards;


    private IReadOnlyList<CardData> _cards;
    private CardType _filter = CardType.UnDefined;
    private bool _reverseSourceOrder;

    protected override void Awake()
    {
        // Runtime UI is configured immediately after this component is added.
        _btnClose.onClick.AddListener(ClosePopup);
        for (CardType type = CardType.UnDefined; type <= CardType.Defense; type++)
        {
            Button button = type switch
            {
                CardType.UnDefined => _btnAllCards,
                CardType.Attack => _btnAttackCards,
                CardType.Defense => _btnDefenseCards,
                _ => null
            };
            if (button != null) AddFilterButton(button, type);
        }
    }

    public void Open(CardPileViewType viewType, IReadOnlyList<CardData> cards)
    {
        _cards = cards ?? new List<CardData>();
        _filter = CardType.UnDefined;
        _reverseSourceOrder = viewType == CardPileViewType.DiscardPile;
        _titleText.text = viewType == CardPileViewType.DrawPile
            ? "남은 덱"
            : "버린 카드 더미";

        gameObject.SetActive(true);
        transform.SetAsLastSibling();
        _canvasGroup.DOKill();
        _canvasGroup.alpha = 0f;
        _canvasGroup.blocksRaycasts = true;
        _canvasGroup.interactable = true;
        _canvasGroup.DOFade(1f, 0.18f).SetUpdate(true);
        RefreshCards();
    }

    protected override void ClosePopup()
    {
        _canvasGroup.DOKill();
        _canvasGroup.blocksRaycasts = false;
        _canvasGroup.interactable = false;
        _canvasGroup.DOFade(0f, 0.15f)
            .SetUpdate(true)
            .OnComplete(() => gameObject.SetActive(false));
    }

    private void AddFilterButton(Button button, CardType type)
    {
        button.onClick.AddListener(() =>
        {
            _filter = type;
            RefreshCards();
        });
        _filterButtons.Add(type, button);
    }

    private void RefreshCards()
    {
        ClearCards();

        IEnumerable<CardData> source = _reverseSourceOrder ? _cards.Reverse() : _cards;
        if (_filter != CardType.UnDefined)
        {
            source = source.Where(card => card != null && card.cardType == _filter);
        }

        List<(CardData Card, int Count)> groups = source
            .Where(card => card != null)
            .GroupBy(card => card.cardId)
            .Select(group => (group.First(), group.Count()))
            .ToList();

        foreach ((CardData card, int count) in groups)
        {
            CreateCard(card, count);
        }

        int totalCount = source.Count(card => card != null);
        _countText.text = $"총 {totalCount}장 · {groups.Count}종";
        _emptyText.gameObject.SetActive(groups.Count == 0);
        UpdateFilterVisuals();
        Canvas.ForceUpdateCanvases();
        _content.anchoredPosition = Vector2.zero;
    }

    private void CreateCard(CardData card, int count)
    {
        GameObject cardRoot = Instantiate(_cardPrefab, _content, false);
        cardRoot.name = $"PileCard_{card.cardName}";
        cardRoot.SetActive(true);

        CardUI cardUI = cardRoot.GetComponent<CardUI>() ?? cardRoot.GetComponentInChildren<CardUI>(true);
        if (cardUI != null)
        {
            cardUI.InitializeDisplay(card);
            cardUI.UpdateView(false);
            cardUI.BindHandView(null);
            cardUI.SetCountBadgeActive(count > 1);
        }

        RectTransform cardRect = cardRoot.GetComponent<RectTransform>();
        if (cardRect != null)
        {
            cardRect.localScale = Vector3.one;
            cardRect.anchoredPosition = Vector2.zero;
        }

        _cardRoots.Add(cardRoot);
    }

    private void ClearCards()
    {
        foreach (GameObject cardRoot in _cardRoots)
        {
            if (cardRoot != null) Destroy(cardRoot);
        }
        _cardRoots.Clear();
    }

    private void UpdateFilterVisuals()
    {
        foreach ((CardType type, Button button) in _filterButtons)
        {
            Image image = button.targetGraphic as Image;
            if (image == null) continue;

            Color color = type switch
            {
                CardType.Attack => new Color(0.68f, 0.16f, 0.17f, 1f),
                CardType.Defense => new Color(0.12f, 0.32f, 0.68f, 1f),
                _ => new Color(0.25f, 0.27f, 0.31f, 1f)
            };
            image.color = type == _filter ? Color.Lerp(color, Color.white, 0.18f) : color;
        }
    }
}
