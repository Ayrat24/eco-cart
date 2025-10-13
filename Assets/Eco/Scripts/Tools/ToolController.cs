using System;
using Cysharp.Threading.Tasks;
using Eco.Scripts.UI;
using R3;
using UnityEngine;

namespace Eco.Scripts.Tools
{
    public class ToolController : MonoBehaviour
    {
        [SerializeField] private Tool[] tools;

        private Tool _currentTool;
        private Tool _queuedNextTool;
        private bool _changingTool;
        private IDisposable _subscription;

        public void Init()
        {
            _subscription = ToolSelector.OnToolSelected.Subscribe(OnToolSelected);
            LoadActiveTool();
        }

        private void LoadActiveTool()
        {
            int activeIndex = 0;
            for (int i = 0; i < tools.Length; i++)
            {
                var tool = tools[i];
                if (i == activeIndex)
                {
                    _currentTool = tool;
                    tool.Enable();
                    continue;
                }

                tool.Disable();
            }
        }

        private void OnToolSelected(int index)
        {
            if (index >= tools.Length)
            {
                Debug.LogError($"Selected tool {index} is out of range.");
                return;
            }

            ChangeTool(tools[index]);
        }

        private void ChangeTool(Tool tool)
        {
            if (_changingTool)
            {
                _queuedNextTool = tool;
                return;
            }

            ChangeToolAsync(tool).Forget();
        }

        private async UniTask ChangeToolAsync(Tool tool)
        {
            _changingTool = true;
            if (_currentTool != null)
            {
                await _currentTool.Disable();
            }

            _currentTool = tool;
            await _currentTool.Enable();

            if (_queuedNextTool != null && _queuedNextTool != tool)
            {
                ChangeToolAsync(_queuedNextTool).Forget();
            }
            else
            {
                _changingTool = false;
            }
        }

        public void Clear()
        {
            _subscription?.Dispose();
        }
    }
}