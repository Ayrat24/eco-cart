using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using Eco.Scripts.Trash;
using Eco.Scripts.Upgrades;
using UnityEngine;
using UnityEngine.UIElements;

namespace Eco.Scripts.ItemCollecting
{
    public class ItemRecycler : MonoBehaviour
    {
        [SerializeField] UIDocument uiDocument;

        private CurrencyManager _currencyManager;
        private UpgradesCollection _upgrades;
        private GainedScoreText _gainedScoreText;
        private bool _initialized;

        public void Init(CurrencyManager currencyManager, UpgradesCollection upgrades)
        {
            _currencyManager = currencyManager;
            _upgrades = upgrades;
            _gainedScoreText = new GainedScoreText();
            _gainedScoreText.Init(uiDocument);
            _initialized = true;
        }

        private void LateUpdate()
        {
            if (!_initialized)
            {
                return;
            }

            _gainedScoreText.FaceCamera(transform);
        }

        public async UniTask EmptyAsync(List<ICartItem> cartItems, CancellationToken token)
        {
            _gainedScoreText.StartNewRecycle();
            
            double scoreMultiplier = _upgrades.GetUpgradeType<ComboMultiplierUpgrade>().Multiplier;
            Dictionary<TrashType, double> scoreMultipliers = new();
            
            var listItems = cartItems.ToList();
            for (var i = listItems.Count - 1; i >= 0; i--)
            {
                var item = listItems[i];
                await UniTask.WaitForSeconds(0.15f, cancellationToken: token);
                item.Recycle();

                if (item is TrashItem trash)
                {
                    var baseScore = _upgrades.TrashScoreUpgrades[trash.TrashType].ScoreForCurrentUpgrade;
                    var score = baseScore;

                    if (scoreMultipliers.TryGetValue(trash.TrashType, out var currentMultiplier))
                    {
                        score = currentMultiplier * score;
                        scoreMultipliers[trash.TrashType] += scoreMultiplier;
                    }
                    else
                    {
                        currentMultiplier = 1;
                        scoreMultipliers[trash.TrashType] = currentMultiplier + scoreMultiplier;
                    }
                    
                    _currencyManager.AddMoney(score);
                    _gainedScoreText.SpawnGainLabel($"+<b><color=green>{score}</color>$</b> Combo:X<color=yellow><b>{currentMultiplier:F1}</b></color> Type:<b>{trash.TrashType}</b>", _upgrades.TrashScoreUpgrades[trash.TrashType].Color);
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