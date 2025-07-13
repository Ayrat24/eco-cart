using System;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;

public class PlayerClickMovement : MonoBehaviour
{
    [SerializeField] private Camera mainCamera;
    [SerializeField] private LayerMask groundMask;

    private NavMeshAgent agent;
    private InputSystem_Actions inputActions;
    private bool isHoldingRightClick = false;
    
    public static Action OnLeftClicked;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();

        if (mainCamera == null)
            mainCamera = Camera.main;

        inputActions = new InputSystem_Actions();
    }

    private void OnEnable()
    {
        inputActions.Player.RightClick.started += OnRMCStart;
        inputActions.Player.RightClick.canceled += OnRMCEnd;
        
        inputActions.Player.LeftClick.performed += OnLeftClick;
        
        inputActions.Enable();
    }

    private void OnDisable()
    {
        inputActions.Player.RightClick.started -= OnRMCStart;
        inputActions.Player.RightClick.canceled -= OnRMCEnd;
        
        inputActions.Player.LeftClick.performed -= OnLeftClick;

        inputActions.Disable();
    }

    private void OnLeftClick(InputAction.CallbackContext obj)
    {
        OnLeftClicked?.Invoke();
    }
    
    private void OnRMCStart(InputAction.CallbackContext obj)
    {
        isHoldingRightClick = true;
    }
    
    private void OnRMCEnd(InputAction.CallbackContext obj)
    {
        isHoldingRightClick = false;
    }
    
    private void Update()
    {
        if (isHoldingRightClick)
        {
            Ray ray = mainCamera.ScreenPointToRay(Mouse.current.position.ReadValue());

            if (Physics.Raycast(ray, out RaycastHit hit, 100f, groundMask))
            {
                agent.SetDestination(hit.point);
            }
        }
    }
}