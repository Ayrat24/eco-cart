using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using Eco.Scripts.Scrap;
using Eco.Scripts.Upgrades;
using UnityEngine;
using UnityEngine.UIElements;

namespace Eco.Scripts.ItemCollecting
{
    public class ItemRecycler : MonoBehaviour
    {
        [SerializeField] UIDocument uiDocument;

        private CurrencyManager _currencyManager;
        private GainedScoreText _gainedScoreText;
        private bool _initialized;

        private ScoreStats _scoreStats;

        public void Init(CurrencyManager currencyManager, ScoreStats scoreStats)
        {
            _currencyManager = currencyManager;
            _scoreStats = scoreStats;
            _gainedScoreText = new GainedScoreText();
            _gainedScoreText.Init(uiDocument);
            _initialized = true;
        }

        /// <summary>Dissolves the items one by one, paying out for each; onRecycled fires as each one goes.</summary>
        public async UniTask EmptyAsync(List<ICartItem> cartItems, Action<ICartItem> onRecycled, CancellationToken token)
        {
            _gainedScoreText.StartNewRecycle();
            
            double scoreMultiplier = _scoreStats.GetComboMultiplier();
            Dictionary<ScrapType, double> scoreMultipliers = new();
            
            var listItems = cartItems.ToList();
            for (var i = listItems.Count - 1; i >= 0; i--)
            {
                var item = listItems[i];
                await UniTask.WaitForSeconds(0.15f, cancellationToken: token);
                item.Dissolve();
                onRecycled?.Invoke(item);

                if (item is ScrapItem scrap)
                {
                    var baseScore = _scoreStats.GetScoreForScrap(scrap.ScrapType);
                    var score = baseScore * scrap.GetWeight();

                    if (scoreMultipliers.TryGetValue(scrap.ScrapType, out var currentMultiplier))
                    {
                        score = currentMultiplier * score;
                        scoreMultipliers[scrap.ScrapType] += scoreMultiplier;
                    }
                    else
                    {
                        currentMultiplier = 1;
                        scoreMultipliers[scrap.ScrapType] = currentMultiplier + scoreMultiplier;
                    }
                    
                    _currencyManager.AddMoney(score);
                    var color = _scoreStats.GetColor(scrap.ScrapType);
                    _gainedScoreText.SpawnGainLabel($"+<b><color=green>{score}</color>$</b> Combo:X<color=yellow><b>{currentMultiplier:F1}</b></color> Type:<b>{scrap.ScrapType}</b>", color);
                    _gainedScoreText.UpdateTotalLabel(score);
                }
            }

            await UniTask.WaitForSeconds(0.6f, cancellationToken: token);
            _gainedScoreText.HideLabels();
        }

        private void OnDestroy()
        {
            _gainedScoreText?.Clear();
        }
    }
}