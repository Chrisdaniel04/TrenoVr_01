using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.XR.Content.Interaction;
using UnityEngine.XR.Interaction.Toolkit;

public class TrainGameController : NetworkBehaviour
{
    #region Inspector

    [Header("Tags")]
    [SerializeField] private string doorTag = "Door";
    [SerializeField] private string doorMachinistTag = "DoorMachinist";
    [SerializeField] private string stairMachinistTag = "StairMachinist";
    [SerializeField] private string roomInfoTag = "RoomInfo";
    [SerializeField] private string trainTag = "Train";
    [SerializeField] private string transporterTag = "Transporter";
    [SerializeField] private string playerTag = "Player";

    [Header("References")]
    [SerializeField] private XRKnob xrKnob;

    [Header("Fuel")]
    [SerializeField] private float fuelMeters = 0f;

    [Header("RoomInfo behavior")]
    [SerializeField] private bool disableRoomInfoColliders = true;
    [SerializeField] private bool disableRoomInfoCanvases = true;
    [SerializeField] private bool disableRoomInfoGameObjects = false;

    [Header("Train Movement")]
    [SerializeField] private float maxTrainSpeed = 3f;
    [SerializeField] private float stopThreshold = 0.3f;
    [SerializeField] private float fuelConsumptionPerMeter = 0.01f;

    [Header("Train Passenger Trigger")]
    [SerializeField] private Collider passengerTrigger;

    #endregion

    #region Private Fields

    private Transform train;
    private bool trainEnabled = false;
    private readonly HashSet<Transform> passengers = new HashSet<Transform>();
    private bool endGameReached = false;
    private Vector3 lastTrainPos;
    private Quaternion lastTrainRot;

    private readonly NetworkVariable<bool> stateApplied =
        new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    private readonly NetworkVariable<float> sharedFuelMeters =
        new NetworkVariable<float>(0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    private float currentSpeed = 0f;
    private float leverValue = 0f;

    #endregion

    #region Unity Lifecycle

    private void Start()
    {
        GameObject trainObj = GameObject.FindGameObjectWithTag(trainTag);
        if (trainObj != null)
        {
            train = trainObj.transform;
            lastTrainPos = train.position;
            lastTrainRot = train.rotation;
        }

        if (passengerTrigger == null && train != null)
        {
            foreach (var col in train.GetComponentsInChildren<Collider>(true))
            {
                if (col.isTrigger)
                {
                    passengerTrigger = col;
                    break;
                }
            }
        }
    }

    private void Update()
    {
        if (!trainEnabled || train == null || xrKnob == null || endGameReached)
        {
            currentSpeed = 0f;
            return;
        }

        fuelMeters = sharedFuelMeters.Value;

        leverValue = ReadLeverNormalized();

        float targetSpeed = leverValue * maxTrainSpeed;

        if (leverValue <= stopThreshold)
        {
            targetSpeed = 0f;
        }

        if (fuelMeters <= 0f)
        {
            targetSpeed = 0f;
        }

        currentSpeed = targetSpeed;

        float distanceThisFrame = currentSpeed * Time.deltaTime;
        float fuelToConsume = distanceThisFrame * fuelConsumptionPerMeter;

        if (fuelToConsume > 0f)
        {
            ConsumeFuelRpc(fuelToConsume);
        }

        train.position -= train.forward * currentSpeed * Time.deltaTime;

        CarryPassengers();
    }

    #endregion

    #region Network Lifecycle

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        sharedFuelMeters.OnValueChanged += OnSharedFuelChanged;

        fuelMeters = sharedFuelMeters.Value;
    }

    public override void OnNetworkDespawn()
    {
        sharedFuelMeters.OnValueChanged -= OnSharedFuelChanged;

        base.OnNetworkDespawn();
    }

    private void OnSharedFuelChanged(float previousValue, float newValue)
    {
        fuelMeters = newValue;
    }

    #endregion

    #region State Management

    public void ApplyStateNetworked()
    {
        ApplyStateRpc();
    }

    [Rpc(SendTo.Server)]
    private void ApplyStateRpc(RpcParams rpcParams = default)
    {
        stateApplied.Value = true;
        sharedFuelMeters.Value = fuelMeters;
        ApplyStateOnEveryoneRpc();
    }

    [Rpc(SendTo.Everyone)]
    private void ApplyStateOnEveryoneRpc(RpcParams rpcParams = default)
    {
        EnableDoors();
        HideRoomInfo();
        EnableTransport();

        trainEnabled = true;
    }

    #endregion

    #region Train Movement

    private float ReadLeverNormalized()
    {
        if (xrKnob == null)
        {
            return 0f;
        }

        return Mathf.Clamp01(xrKnob.value);
    }

    public float GetCurrentSpeed()
    {
        return currentSpeed;
    }

    private void CarryPassengers()
    {
        if (train == null)
        {
            return;
        }

        Vector3 newPos = train.position;
        Quaternion newRot = train.rotation;

        Vector3 deltaPos = newPos - lastTrainPos;
        Quaternion deltaRot = newRot * Quaternion.Inverse(lastTrainRot);

        if (deltaPos != Vector3.zero || deltaRot != Quaternion.identity)
        {
            Vector3 pivot = train.position;

            foreach (var p in passengers)
            {
                if (p == null)
                {
                    continue;
                }

                Vector3 fromPivot = p.position - pivot;
                fromPivot = deltaRot * fromPivot;
                p.position = pivot + fromPivot + deltaPos;
            }
        }

        lastTrainPos = newPos;
        lastTrainRot = newRot;
    }

    public void LimitSpeed(float speed, float penalty)
    {
        if (currentSpeed > speed)
        {
            sharedFuelMeters.Value = Mathf.Max(0f, sharedFuelMeters.Value - penalty);
        }
    }

    #endregion

    #region Fuel

    public float GetFuelMeters()
    {
        return sharedFuelMeters.Value;
    }

    public void AddFuel(float amount)
    {
        AddFuelRpc(amount);
    }

    [Rpc(SendTo.Server)]
    private void AddFuelRpc(float amount, RpcParams rpcParams = default)
    {
        sharedFuelMeters.Value += amount;
    }

    [Rpc(SendTo.Server)]
    private void ConsumeFuelRpc(float amount, RpcParams rpcParams = default)
    {
        sharedFuelMeters.Value = Mathf.Max(0f, sharedFuelMeters.Value - amount);
    }

    #endregion

    #region Passenger Handling

    public void PassengerEnter(Collider other)
    {
        if (!other.CompareTag(playerTag))
            return;

        Transform root = other.transform.root;
        passengers.Add(root);
    }

    public void PassengerExit(Collider other)
    {
        if (!other.CompareTag(playerTag))
            return;

        Transform root = other.transform.root;
        passengers.Remove(root);
    }

    #endregion

    #region Environment Control

    private void EnableDoors()
    {
        var doors = GameObject.FindGameObjectsWithTag(doorTag);

        foreach (var go in doors)
        {
            foreach (var r in go.GetComponentsInChildren<Renderer>(true))
                r.enabled = true;

            foreach (var c in go.GetComponentsInChildren<Collider>(true))
                c.enabled = true;
        }
    }

    private void HideRoomInfo()
    {
        var infos = GameObject.FindGameObjectsWithTag(roomInfoTag);

        foreach (var go in infos)
        {
            foreach (var r in go.GetComponentsInChildren<Renderer>(true))
                r.enabled = false;

            if (disableRoomInfoCanvases)
            {
                foreach (var canvas in go.GetComponentsInChildren<Canvas>(true))
                    canvas.enabled = false;
            }

            if (disableRoomInfoColliders)
            {
                foreach (var c in go.GetComponentsInChildren<Collider>(true))
                    c.enabled = false;
            }

            if (disableRoomInfoGameObjects)
            {
                go.SetActive(false);
                continue;
            }
        }
    }

    public void EnableDoorMachinist()
    {
        var doors = GameObject.FindGameObjectsWithTag(doorMachinistTag);
        var stairs = GameObject.FindGameObjectsWithTag(stairMachinistTag);

        foreach (var go in doors)
        {
            foreach (var r in go.GetComponentsInChildren<Renderer>(true))
                r.enabled = true;

            foreach (var c in go.GetComponentsInChildren<Collider>(true))
                c.enabled = true;
        }

        foreach (var go in stairs)
        {
            foreach (var r in go.GetComponentsInChildren<Renderer>(true))
                r.enabled = false;

            foreach (var c in go.GetComponentsInChildren<Collider>(true))
                c.enabled = false;
        }
    }

    public void DisableDoorMachinist()
    {
        var doors = GameObject.FindGameObjectsWithTag(doorMachinistTag);
        var stairs = GameObject.FindGameObjectsWithTag(stairMachinistTag);

        foreach (var go in doors)
        {
            foreach (var r in go.GetComponentsInChildren<Renderer>(true))
                r.enabled = false;

            foreach (var c in go.GetComponentsInChildren<Collider>(true))
                c.enabled = false;
        }

        foreach (var go in stairs)
        {
            foreach (var r in go.GetComponentsInChildren<Renderer>(true))
                r.enabled = true;

            foreach (var c in go.GetComponentsInChildren<Collider>(true))
                c.enabled = true;
        }
    }

    private void EnableTransport()
    {
        var transporters = GameObject.FindGameObjectsWithTag(transporterTag);

        foreach (var go in transporters)
        {
            foreach (var r in go.GetComponentsInChildren<Renderer>(true))
                r.enabled = true;

            foreach (var c in go.GetComponentsInChildren<Collider>(true))
                c.enabled = true;
        }
    }

    #endregion

    #region Card Handling

    public void CardEnter(Collider other)
    {
        TrainCardCarrier card = other.GetComponentInParent<TrainCardCarrier>();
        if (card == null)
            return;

        card.AttachToTrain(train);
    }

    public void CardExit(Collider other)
    {
        TrainCardCarrier card = other.GetComponentInParent<TrainCardCarrier>();
        if (card == null)
            return;

        card.DetachFromTrain();
    }

    #endregion

    #region End Game

    public void ApplyEndGameFromTrigger()
    {
        endGameReached = true;
        trainEnabled = false;
        currentSpeed = 0f;
        leverValue = 0f;

        var doors = GameObject.FindGameObjectsWithTag(doorTag);

        foreach (var go in doors)
        {
            foreach (var r in go.GetComponentsInChildren<Renderer>(true))
                r.enabled = false;

            foreach (var c in go.GetComponentsInChildren<Collider>(true))
                c.enabled = false;
        }

        var transporters = GameObject.FindGameObjectsWithTag(transporterTag);

        foreach (var go in transporters)
        {
            foreach (var r in go.GetComponentsInChildren<Renderer>(true))
                r.enabled = false;

            foreach (var c in go.GetComponentsInChildren<Collider>(true))
                c.enabled = false;
        }

        RequestStopTrainServerRpc();
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void RequestStopTrainServerRpc(RpcParams rpcParams = default)
    {
        if (endGameReached)
            return;

        endGameReached = true;
        trainEnabled = false;
        currentSpeed = 0f;
        leverValue = 0f;

        StopTrainClientRpc(
            train != null ? train.position : Vector3.zero,
            train != null ? train.rotation : Quaternion.identity
        );
    }

    [Rpc(SendTo.Everyone)]
    private void StopTrainClientRpc(Vector3 finalPos, Quaternion finalRot, RpcParams rpcParams = default)
    {
        endGameReached = true;
        trainEnabled = false;
        currentSpeed = 0f;
        leverValue = 0f;

        if (train != null)
        {
            train.position = finalPos;
            train.rotation = finalRot;
            lastTrainPos = finalPos;
            lastTrainRot = finalRot;
        }

        var doors = GameObject.FindGameObjectsWithTag(doorTag);

        foreach (var go in doors)
        {
            foreach (var r in go.GetComponentsInChildren<Renderer>(true))
                r.enabled = false;

            foreach (var c in go.GetComponentsInChildren<Collider>(true))
                c.enabled = false;
        }

        var transporters = GameObject.FindGameObjectsWithTag(transporterTag);

        foreach (var go in transporters)
        {
            foreach (var r in go.GetComponentsInChildren<Renderer>(true))
                r.enabled = false;

            foreach (var c in go.GetComponentsInChildren<Collider>(true))
                c.enabled = false;
        }
    }

    #endregion
}