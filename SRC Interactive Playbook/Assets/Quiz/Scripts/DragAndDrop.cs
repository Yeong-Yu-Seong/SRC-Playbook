using System;
using System.Collections.Generic;
using System.Linq;
using RedCross.Playbook.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DragAndDrop : MonoBehaviour
{
    [Header("UI References")]
    public GameObject gamePanel;
    public Button submitButton;

    [Header("Matching Board Elements")]
    [Tooltip("The layout group where the Poor Feedback rows will be spawned.")]
    public Transform rowsContainer;
    [Tooltip("Prefab containing the DNDRowUI script, the Text, and the Drop Slot.")]
    public GameObject matchRowPrefab;

    [Tooltip("The layout group at the bottom where draggable options start.")]
    public Transform optionsContainer;
    [Tooltip("Prefab containing DraggableOption and a TextMeshProUGUI.")]
    public GameObject draggablePrefab;

    // ── Runtime ────────────────────────────────────────────────
    private PlaybookQuiz _quizData;
    private int _correctCount = 0;
    private Dictionary<string, string> _capturedAnswers = new();
    private Action<int, int, int> _onCompleteCallback;
    private List<GameObject> _spawnedObjects = new();

    private void Awake()
    {
        if (gamePanel != null) gamePanel.SetActive(false);
        if (submitButton != null) submitButton.onClick.AddListener(OnSubmitAnswer);
    }

    public void StartGame(PlaybookQuiz runtimeQuiz, Action<int, int, int> onComplete)
    {
        if (runtimeQuiz == null || runtimeQuiz.questions == null || runtimeQuiz.questions.Count == 0)
        {
            Debug.LogError("[DragAndDrop] Invalid quiz data.");
            return;
        }

        _onCompleteCallback = onComplete;
        _quizData = runtimeQuiz;
        _correctCount = 0;
        _capturedAnswers.Clear();

        gameObject.SetActive(true);
        if (gamePanel != null) gamePanel.SetActive(true);

        BuildMatchingBoard();
    }


    private void BuildMatchingBoard()
    {
        ClearBoard();
        Dictionary<string, QuizChoice> choicePool = new Dictionary<string, QuizChoice>();

        foreach (var q in _quizData.questions)
        {
            GameObject newRow = Instantiate(matchRowPrefab, rowsContainer);
            _spawnedObjects.Add(newRow);

            DNDRowUI rowUI = newRow.GetComponent<DNDRowUI>();
            if (rowUI != null)
            {
                rowUI.poorFeedbackText.text = q.prompt;

                rowUI.dropSlot.questionId = q.id;
            }

            foreach (var choice in q.choices)
            {
                if (!choicePool.ContainsKey(choice.id))
                {
                    choicePool.Add(choice.id, choice);
                }
            }
        }

        var shuffledChoices = choicePool.Values.OrderBy(x => Guid.NewGuid()).ToList();

        foreach (var choice in shuffledChoices)
        {
            GameObject newOption = Instantiate(draggablePrefab, optionsContainer);
            _spawnedObjects.Add(newOption);

            DraggableOption dragScript = newOption.GetComponentInChildren<DraggableOption>();
            if (dragScript != null)
            {
                dragScript.choiceId = choice.id;
            }

            TextMeshProUGUI optionText = newOption.GetComponentInChildren<TextMeshProUGUI>();
            if (optionText != null) optionText.text = choice.text;
        }
    }

    private void ClearBoard()
    {
        foreach (var obj in _spawnedObjects) { if (obj != null) Destroy(obj); }
        _spawnedObjects.Clear();

        foreach (Transform child in rowsContainer) Destroy(child.gameObject);
        foreach (Transform child in optionsContainer) Destroy(child.gameObject);
    }

    private void OnSubmitAnswer()
    {
        if (submitButton != null) submitButton.interactable = false;
        _correctCount = 0;

        foreach (Transform row in rowsContainer)
        {
            DNDRowUI rowUI = row.GetComponent<DNDRowUI>();
            if (rowUI == null) continue;

            string questionId = rowUI.dropSlot != null ? rowUI.dropSlot.questionId : null;
            if (string.IsNullOrEmpty(questionId))
            {
                Debug.LogWarning($"[DragAndDrop] Skipping row '{row.name}' — no questionId assigned (stray/template row?).");
                continue;
            }

            var q = _quizData.questions.Find(x => x.id == questionId);

            string selectedChoiceId = "";
            DraggableOption droppedOption = rowUI.dropSlot.GetComponentInChildren<DraggableOption>();

            if (droppedOption != null)
            {
                selectedChoiceId = droppedOption.choiceId;
            }

            _capturedAnswers[questionId] = selectedChoiceId;
            bool isCorrect = (q != null && selectedChoiceId == q.correctAnswerId);

            if (isCorrect) _correctCount++;

            // Visual Validation
            Image slotImage = rowUI.dropSlot.GetComponent<Image>();
            if (slotImage != null)
            {
                slotImage.color = isCorrect
                    ? new Color(0.369f, 0.788f, 0.498f)
                    : new Color(0.906f, 0.255f, 0.259f);
            }
        }

        StartCoroutine(ShowFeedbackAndEnd());
    }

    private System.Collections.IEnumerator ShowFeedbackAndEnd()
    {
        yield return new WaitForSeconds(2f);
        EndGame();
    }

    private void EndGame()
    {
        foreach (var kvp in _capturedAnswers)
        {
            Debug.Log($"[DragAndDrop] key='{kvp.Key}' (len={kvp.Key.Length})  value='{kvp.Value}' (len={kvp.Value?.Length ?? 0})");

            if (kvp.Key.IndexOfAny(new[] { '.', '#', '$', '[', ']', '/' }) >= 0)
                Debug.LogError($"[DragAndDrop] ILLEGAL Firebase key character in questionId: '{kvp.Key}'");

            if (kvp.Value != null && kvp.Value.IndexOfAny(new[] { '.', '#', '$', '[', ']', '/' }) >= 0)
                Debug.LogWarning($"[DragAndDrop] Suspicious characters in choiceId value: '{kvp.Value}'");
        }
        ScoreManager.Instance.SubmitDragDropScore(
            _quizData.id, _correctCount, _quizData.questions.Count, _capturedAnswers,
            onSuccess: () =>
            {
                int ptsEach = ScoreManager.Instance.pointsPerCorrectDragDrop;
                bool perfect = _correctCount == _quizData.questions.Count;
                int pointsEarned = (_correctCount * ptsEach) + (perfect ? ScoreManager.Instance.perfectScoreBonus : 0);

                gameObject.SetActive(false);
                _onCompleteCallback?.Invoke(_correctCount, _quizData.questions.Count, pointsEarned);
                Debug.Log("[DragAndDrop] Firebase write CONFIRMED successful despite any console noise.");
            },
            onError: err =>
            {
                Debug.LogError($"[DragAndDrop] Score save failed: {err}");
                gameObject.SetActive(false);
                _onCompleteCallback?.Invoke(_correctCount, _quizData.questions.Count, 0);
            }
        );
    }
}