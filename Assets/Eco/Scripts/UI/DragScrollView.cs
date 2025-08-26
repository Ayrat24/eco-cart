using UnityEngine;
using UnityEngine.UIElements;

namespace Eco.Scripts.UI
{
    [UxmlElement]
    public partial class DragScrollView : ScrollView
    {
        public bool Interactable = true;
        public bool ContainsMouse { get; private set; } = false;
        public bool MouseDown { get; private set; } = false;
        public Vector2 ScrollRootOffset { get; private set; }
        public Vector2 MouseDownLocation { get; private set; }

        public DragScrollView()
        {
        }


        public void Init()
        {
            DoRegisterCallbacks();
        }

        VisualElement MouseOwner => this;

        protected virtual void DoRegisterCallbacks()
        {
            MouseOwner.RegisterCallback<MouseUpEvent>(OnMouseUp);
            MouseOwner.RegisterCallback<MouseDownEvent>(OnMouseDown);
            MouseOwner.RegisterCallback<MouseMoveEvent>(OnMouseMove);
        }

        void HandleDrag(IMouseEvent e)
        {
            Vector2 deltaPos = e.mousePosition - MouseDownLocation;
            scrollOffset = ScrollRootOffset - deltaPos;
        }

        protected virtual void OnMouseMove(MouseMoveEvent e)
        {
            if (MouseDown && Interactable)
            {
                if (MouseCaptureController.HasMouseCapture(MouseOwner))
                {
                    HandleDrag(e);
                }
            }

            e.StopPropagation();
        }

        protected virtual void OnMouseUp(MouseUpEvent e)
        {
            MouseCaptureController.ReleaseMouse(MouseOwner);
            MouseDown = false;
            e.StopPropagation();
        }

        protected virtual void OnMouseDown(MouseDownEvent e)
        {
            if (!worldBound.Contains(e.mousePosition))
            {
                MouseCaptureController.ReleaseMouse(MouseOwner);
            }
            else if (Interactable)
            {
                MouseOwner.CaptureMouse();
                MouseDownLocation = e.mousePosition;
                ScrollRootOffset = scrollOffset;
                MouseDown = true;
                e.StopPropagation();
            }
        }
    }
}