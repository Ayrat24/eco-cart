using System.Collections;
using PrimeTween;
using TMPro;
using UnityEngine;

public class MoneyCounter : MonoBehaviour
{
    private const float AnimationDuration = .9f;

    [SerializeField] protected TextMeshProUGUI countLabel;
    [SerializeField] private RectTransform selfRect;

    private Coroutine _coroutine;
    private Tween _tween;

    private float _visualValue = -1;

    private void Start()
    {
        MoneyController.OnMoneyChangedEvent += UpdateValue;
    }

    private void OnDestroy()
    {
        MoneyController.OnMoneyChangedEvent -= UpdateValue;
    }

    public void UpdateValue(uint value)
    {
        countLabel.text = value.ToString();
        return;
        
        if (_coroutine != null)
        {
            StopCoroutine(_coroutine);
        }

        _coroutine = null;

        _coroutine = StartCoroutine(InterpolateValue(_visualValue, value));
        _visualValue = value;
    }

    private IEnumerator InterpolateValue(float from, float to)
    {
        float current;
        var elapsedTime = 0f;

        while (elapsedTime < AnimationDuration)
        {
            current = Mathf.Lerp(from, to, (elapsedTime += Time.deltaTime) / AnimationDuration);
            countLabel.text = Mathf.FloorToInt(current).ToString();
            TriggerAnimationIfReady();

            yield return null;
        }

        current = to;
        countLabel.text = Mathf.FloorToInt(current).ToString();
    }

    private void TriggerAnimationIfReady()
    {
        if (_tween.isAlive)
        {
            return;
        }

        selfRect.localScale = Vector3.one * 1.05f;

        _tween.Stop();

        _tween = Tween.Scale(selfRect, Vector3.one, 0.4f, Ease.OutQuad);
    }
}