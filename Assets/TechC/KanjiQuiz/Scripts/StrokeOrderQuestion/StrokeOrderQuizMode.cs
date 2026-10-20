using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TechC.KanjiQuiz.Data;

namespace TechC.KanjiQuiz.StrokeOrderQuestion
{
    public enum Difficulty
    {
        Easy,
        Normal,
        Hard
    }

    public class StrokeOrderQuizMode : MonoBehaviour
    {
        [Header("--- 参照 ---")]
        [SerializeField] private GameContext gameContext;
        [SerializeField] private Button nextButton;
        [SerializeField] private Button prevButton;
        [SerializeField] private GameObject ConfirmationScreen;

        [Header("--- プレハブと生成設定 ---")]
        [SerializeField] private QuizChoiceItem choicePrefab;
        [SerializeField] private Transform container;
        [SerializeField] private ToggleGroup toggleGroup;
        [SerializeField] private int numberOfChoices = 3;

        [Header("--- 難易度設定 ---")]
        [SerializeField] private Difficulty currentDifficulty = Difficulty.Easy;

        private List<QuizChoiceItem> choiceItems = new List<QuizChoiceItem>();
        private KanjiDataParser kanjiParser;
        private int correctAnswerIndex;

        private void Awake()
        {
            kanjiParser = new KanjiDataParser();
        }

        private void Start()
        {
            nextButton.onClick.AddListener(OnNextButtonClicked);
            prevButton.onClick.AddListener(OnPrevButtonClicked);            

            for (int i = 0; i < numberOfChoices; i++)
            {
                var item = Instantiate(choicePrefab, container);
                item.Toggle.group = toggleGroup;
                choiceItems.Add(item);
            }

            LayoutRebuilder.ForceRebuildLayoutImmediate(container.GetComponent<RectTransform>());

            gameContext.ResetContext();
            SetupQuestion();
        }

        private void SetupQuestion()
        {
            prevButton.interactable = (gameContext.CurrentQuestionIndex > 0);

            string currentKanji = string.Empty;
            List<string> strokeXmls = null;

            while (gameContext.CurrentQuestionIndex < gameContext.QuestionKanjiList.Count)
            {
                currentKanji = gameContext.GetCurrentKanji();
                if (string.IsNullOrEmpty(currentKanji)) return;

                strokeXmls = kanjiParser.LoadSingleKanjiStrokes(currentKanji);
                if (strokeXmls != null && IsStrokeCountValid(strokeXmls.Count))
                {
                    break;
                }

                gameContext.CurrentQuestionIndex++;
            }

            if (gameContext.IsGameFinished() || strokeXmls == null) return;

            correctAnswerIndex = Random.Range(0, choiceItems.Count);

            for (int i = 0; i < choiceItems.Count; i++)
            {
                choiceItems[i].Toggle.group = null;
                choiceItems[i].Toggle.isOn = false;
                choiceItems[i].Toggle.group = toggleGroup;

                if (!choiceItems[i].StrokeRenderer.gameObject.activeSelf)
                {
                    choiceItems[i].StrokeRenderer.gameObject.SetActive(true);
                }

                if (i == correctAnswerIndex)
                {
                    List<StrokeData> correctPoints = kanjiParser.GenerateStrokePoints(strokeXmls, true);
                    choiceItems[i].StrokeRenderer.DrawStrokeData(correctPoints);
                }
                else
                {
                    List<string> shuffledXmls = ApplyDifficultyShuffle(strokeXmls);
                    List<StrokeData> incorrectPoints = kanjiParser.GenerateStrokePoints(shuffledXmls, true);
                    choiceItems[i].StrokeRenderer.DrawStrokeData(incorrectPoints);
                }
            }
        }

        private bool IsStrokeCountValid(int count)
        {
            switch (currentDifficulty)
            {
                case Difficulty.Easy: return count >= 1;
                case Difficulty.Normal: return count >= 3;
                case Difficulty.Hard: return count >= 5;
                default: return true;
            }
        }


        /// <summary>
        /// 難易度に応じて、正解のストローク順をシャッフルするメソッド
        /// </summary>
        /// <param name="original"></param>
        /// <returns></returns>
        private List<string> ApplyDifficultyShuffle(List<string> original)
        {
            List<string> shuffled = new List<string>(original);

            switch (currentDifficulty)
            {
                case Difficulty.Easy:
                    for (int i = shuffled.Count - 1; i > 0; i--)
                    {
                        int k = Random.Range(0, i + 1);
                        string temp = shuffled[k];
                        shuffled[k] = shuffled[i];
                        shuffled[i] = temp;
                    }
                    break;

                case Difficulty.Normal:
                    if (shuffled.Count >= 2)
                    {
                        int shuffleCount = Mathf.Min(Random.Range(2, 4), shuffled.Count);
                        List<int> indices = new List<int>();
                        while (indices.Count < shuffleCount)
                        {
                            int idx = Random.Range(0, shuffled.Count);
                            if (!indices.Contains(idx)) indices.Add(idx);
                        }

                        List<string> subList = new List<string>();
                        foreach (int idx in indices) subList.Add(shuffled[idx]);

                        for (int i = subList.Count - 1; i > 0; i--)
                        {
                            int k = Random.Range(0, i + 1);
                            string temp = subList[k];
                            subList[k] = subList[i];
                            subList[i] = temp;
                        }

                        for (int i = 0; i < indices.Count; i++)
                        {
                            shuffled[indices[i]] = subList[i];
                        }
                    }
                    break;

                case Difficulty.Hard:
                    if (shuffled.Count >= 2)
                    {
                        int idx1 = Random.Range(0, shuffled.Count);
                        int idx2 = Random.Range(0, shuffled.Count);
                        while (idx1 == idx2) idx2 = Random.Range(0, shuffled.Count);

                        string temp = shuffled[idx1];
                        shuffled[idx1] = shuffled[idx2];
                        shuffled[idx2] = temp;
                    }
                    break;
            }
            return shuffled;
        }

        private void OnNextButtonClicked()
        {
            nextButton.interactable = false;

            int selectedIndex = -1;
            for (int i = 0; i < choiceItems.Count; i++)
            {
                if (choiceItems[i].Toggle.isOn)
                {
                    selectedIndex = i;
                    break;
                }
            }

            if (selectedIndex == -1) 
            {
                nextButton.interactable = true;
                return;
            }

            bool isCorrect = selectedIndex == correctAnswerIndex;
            Debug.Log ("[StrokeOrderQuizMode] 問題番号 " + gameContext.CurrentQuestionIndex + " の回答は " + (isCorrect ? "正解" : "不正解") + " です。");

            gameContext.RecordAnswer(isCorrect);
            gameContext.CurrentQuestionIndex++;

            if (gameContext.IsGameFinished()) 
            {
                nextButton.interactable = false;
                ConfirmationScreen.SetActive(true);
            }
            else
            {
                SetupQuestion();
                nextButton.interactable = true;
            }
        }

        private void OnPrevButtonClicked()
        {
            if (gameContext.CurrentQuestionIndex > 0)
            {
                gameContext.CurrentQuestionIndex--;
                SetupQuestion();
            }
        }

    }
}