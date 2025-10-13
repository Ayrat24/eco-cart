using System;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;

public class PlayerClickMovement : MonoBehaviour
{
    [SerializeField] private Camera mainCamera;
    [SerializeField] private LayerMask groundMask;

    private NavMeshAgent _agent;
    private InputSystem_Actions _inputActions;
    private bool _isHoldingRightClick = false;
    
    public static Action OnLeftClicked;

    private void Awake()
    {
        _agent = GetComponent<NavMeshAgent>();

        if (mainCamera == null)
            mainCamera = Camera.main;

        _inputActions = new InputSystem_Actions();
    }

    private void OnEnable()
    {
        _inputActions.Player.RightClick.started += OnRMCStart;
        _inputActions.Player.RightClick.canceled += OnRMCEnd;
        
        _inputActions.Player.LeftClick.performed += OnLeftClick;
        
        _inputActions.Enable();
    }

    private void OnDisable()
    {
        _inputActions.Player.RightClick.started -= OnRMCStart;
        _inputActions.Player.RightClick.canceled -= OnRMCEnd;
        
        _inputActions.Player.LeftClick.performed -= OnLeftClick;

        _inputActions.Disable();
    }

    private void OnLeftClick(InputAction.CallbackContext obj)
    {
        OnLeftClicked?.Invoke();
    }
    
    private void OnRMCStart(InputAction.CallbackContext obj)
    {
        _isHoldingRightClick = true;
    }
    
    private void OnRMCEnd(InputAction.CallbackContext obj)
    {
        _isHoldingRightClick = false;
    }
    
    private void Update()
    {
        if (_isHoldingRightClick)
        {
            Ray ray = mainCamera.ScreenPointToRay(Mouse.current.position.ReadValue());

            if (Physics.Raycast(ray, out RaycastHit hit, 100f, groundMask))
            {
                _agent.SetDestination(hit.point);
            }
        }
    }
}