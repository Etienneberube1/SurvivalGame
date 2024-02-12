using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class BuildingManager : MonoBehaviour
{
    [Header("Build Objects")]
    [SerializeField] private List<GameObject> floorObjects = new List<GameObject>();
    [SerializeField] private List<GameObject> wallObjects = new List<GameObject>();

    [Header("Build Settings")]
    [SerializeField] private SelectedBuildType currentBuildType;
    [SerializeField] private LayerMask connectorLayer;

    [Header("Destroy Settings")]
    [SerializeField] private bool isDestroying = false;
    private Transform lastHitDestroyTransform;
    private List<Material> LastHitMaterials = new List<Material>();

    [Header("Ghost Settings")]
    [SerializeField] private Material ghostMaterialValid;
    [SerializeField] private Material ghostMaterialInvalid;
    [SerializeField] private float connectorOverlapRadius = 1;
    [SerializeField] private float maxGroundAngle = 45f;

    [Header("Internal State")]
    [SerializeField] private bool isBuilding = false;
    [SerializeField] private int currentBuildingIndex;
    private GameObject ghostBuildGameobject;
    private bool isGhostInValidPosition = false;
    private Transform ModelParent = null;

    [Header("UI")]
    [SerializeField] private GameObject buildingUI;
    [SerializeField] private TMP_Text destroyText;

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            toggleBuildingUI(!buildingUI.activeInHierarchy);
        }

        if (isBuilding && !isDestroying)
        {
            ghostBuild();

            if (Input.GetMouseButtonDown(0))
                placeBuild();
        }
        else if (ghostBuildGameobject)
        {
            Destroy(ghostBuildGameobject);
            ghostBuildGameobject = null;
        }

        if (isDestroying)
        {
            ghostDestroy();

            if (Input.GetMouseButtonDown(0))
                destroyBuild();
        }
    }

    private void ghostBuild()
    {
        GameObject currentBuild = getCurrentBuild();
        createGhostPrefab(currentBuild);

        moveGhostPrefabToRaycast();
        checkBuildValidity();
    }

    private void createGhostPrefab(GameObject currentBuild)
    {
        if (ghostBuildGameobject == null)
        {
            ghostBuildGameobject = Instantiate(currentBuild);

            ModelParent = ghostBuildGameobject.transform.GetChild(0);

            ghostifyModel(ModelParent, ghostMaterialValid);
            ghostifyModel(ghostBuildGameobject.transform);
        }
    }

    private void moveGhostPrefabToRaycast()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit))
        {
            ghostBuildGameobject.transform.position = hit.point;
        }
    }

    private void checkBuildValidity()
    {
        Collider[] colliders = Physics.OverlapSphere(ghostBuildGameobject.transform.position, connectorOverlapRadius, connectorLayer);
        if (colliders.Length > 0)
        {
            ghostConnectBuild(colliders);
        }
        else
        {
            ghostSeperateBuild();

            if (isGhostInValidPosition)
            {
                Collider[] overlapColliders = Physics.OverlapBox(ghostBuildGameobject.transform.position, new Vector3(2f, 2f, 2f), ghostBuildGameobject.transform.rotation);
                foreach (Collider overlapCollider in overlapColliders)
                {
                    if (overlapCollider.gameObject != ghostBuildGameobject && overlapCollider.transform.root.CompareTag("Buildables"))
                    {
                        ghostifyModel(ModelParent, ghostMaterialInvalid);
                        isGhostInValidPosition = false;
                        return;
                    }
                }
            }
        }
    }

    private void ghostConnectBuild(Collider[] colliders)
    {
        Connector bestConnector = null;

        foreach (Collider collider in colliders)
        {
            Connector connector = collider.GetComponent<Connector>();

            if (connector.canConnectTo)
            {
                bestConnector = connector;
                break;
            }
        }

        if (bestConnector == null || currentBuildType == SelectedBuildType.floor && bestConnector.isConnectedToFloor || currentBuildType == SelectedBuildType.wall && bestConnector.isConnectedToWall)
        {
            ghostifyModel(ModelParent, ghostMaterialInvalid);
            isGhostInValidPosition = false;
            return;
        }

        snapGhostPrefabToConnector(bestConnector);
    }

    private void snapGhostPrefabToConnector(Connector connector)
    {
        Transform ghostConnector = findSnapConnector(connector.transform, ghostBuildGameobject.transform.GetChild(1));
        ghostBuildGameobject.transform.position = connector.transform.position - (ghostConnector.position - ghostBuildGameobject.transform.position);

        if (currentBuildType == SelectedBuildType.wall)
        {
            Quaternion newRotation = ghostBuildGameobject.transform.rotation;
            newRotation.eulerAngles = new Vector3(newRotation.eulerAngles.x, connector.transform.rotation.eulerAngles.y, newRotation.eulerAngles.z);
            ghostBuildGameobject.transform.rotation = newRotation;
        }

        ghostifyModel(ModelParent, ghostMaterialValid);
        isGhostInValidPosition = true;
    }

    private void ghostSeperateBuild()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit))
        {
            if (currentBuildType == SelectedBuildType.wall)
            {
                ghostifyModel(ModelParent, ghostMaterialInvalid);
                isGhostInValidPosition = false;
                return;
            }

            if (Vector3.Angle(hit.normal, Vector3.up) < maxGroundAngle)
            {
                ghostifyModel(ModelParent, ghostMaterialValid);
                isGhostInValidPosition = true;
            }
            else
            {
                ghostifyModel(ModelParent, ghostMaterialInvalid);
                isGhostInValidPosition = false;
            }
        }
    }

    private Transform findSnapConnector(Transform snapConnector, Transform ghostConnectorParent)
    {
        ConnectorPosition OppositeConnectorTag = getOppositePosition(snapConnector.GetComponent<Connector>());

        foreach (Connector connector in ghostConnectorParent.GetComponentsInChildren<Connector>())
        {
            if (connector.connectorPosition == OppositeConnectorTag)
                return connector.transform;
        }

        return null;
    }

    private ConnectorPosition getOppositePosition(Connector connector)
    {
        ConnectorPosition position = connector.connectorPosition;

        if (currentBuildType == SelectedBuildType.wall && connector.connectorParentType == SelectedBuildType.floor)
            return ConnectorPosition.bottom;

        if (currentBuildType == SelectedBuildType.floor && connector.connectorParentType == SelectedBuildType.wall && connector.connectorPosition == ConnectorPosition.top)
        {
            if (connector.transform.root.rotation.y == 0)
            {
                return getConnectorClosestToPlayer(true);
            }
            else
            {
                return getConnectorClosestToPlayer(false);
            }
        }

        switch (position)
        {
            case ConnectorPosition.left:
                return ConnectorPosition.right;
            case ConnectorPosition.right:
                return ConnectorPosition.left;
            case ConnectorPosition.bottom:
                return ConnectorPosition.top;
            case ConnectorPosition.top:
                return ConnectorPosition.bottom;
            default:
                return ConnectorPosition.bottom;
        }
    }

    private ConnectorPosition getConnectorClosestToPlayer(bool topBottom)
    {
        Transform cameraTransform = Camera.main.transform;

        if (topBottom)
            return cameraTransform.position.z >= ghostBuildGameobject.transform.position.z ? ConnectorPosition.bottom : ConnectorPosition.top;
        else
            return cameraTransform.position.x >= ghostBuildGameobject.transform.position.x ? ConnectorPosition.left : ConnectorPosition.right;
    }

    private void ghostifyModel(Transform modelParent, Material ghostMaterial = null)
    {
        if (ghostMaterial != null)
        {
            foreach (MeshRenderer meshRenderer in modelParent.GetComponentsInChildren<MeshRenderer>())
            {
                meshRenderer.material = ghostMaterial;
            }
        }
        else
        {
            foreach (Collider modelColliders in modelParent.GetComponentsInChildren<Collider>())
            {
                modelColliders.enabled = false;
            }
        }
    }

    private GameObject getCurrentBuild()
    {
        switch (currentBuildType)
        {
            case SelectedBuildType.floor:
                return floorObjects[currentBuildingIndex];
            case SelectedBuildType.wall:
                return wallObjects[currentBuildingIndex];
        }

        return null;
    }

    private void placeBuild()
    {
        if (ghostBuildGameobject != null & isGhostInValidPosition)
        {
            GameObject newBuild = Instantiate(getCurrentBuild(), ghostBuildGameobject.transform.position, ghostBuildGameobject.transform.rotation);

            Destroy(ghostBuildGameobject);
            ghostBuildGameobject = null;

            //isBuilding = false;

            foreach (Connector connector in newBuild.GetComponentsInChildren<Connector>())
            {
                connector.updateConnectors(true);
            }
        }
    }

    private void ghostDestroy()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit))
        {
            if (hit.transform.root.CompareTag("Buildables"))
            {
                if (!lastHitDestroyTransform)
                {
                    lastHitDestroyTransform = hit.transform.root;

                    LastHitMaterials.Clear();
                    foreach (MeshRenderer lastHitMeshRenderers in lastHitDestroyTransform.GetComponentsInChildren<MeshRenderer>())
                    {
                        LastHitMaterials.Add(lastHitMeshRenderers.material);
                    }

                    ghostifyModel(lastHitDestroyTransform.GetChild(0), ghostMaterialInvalid);
                }
                else if (hit.transform.root != lastHitDestroyTransform)
                {
                    resetLastHitDestroyTransform();
                }
            }
            else if (lastHitDestroyTransform)
            {
                resetLastHitDestroyTransform();
            }
        }
    }

    private void resetLastHitDestroyTransform()
    {
        int counter = 0;
        foreach (MeshRenderer lastHitMeshRenderers in lastHitDestroyTransform.GetComponentsInChildren<MeshRenderer>())
        {
            lastHitMeshRenderers.material = LastHitMaterials[counter];
            counter++;
        }

        lastHitDestroyTransform = null;
    }

    private void destroyBuild()
    {
        if (lastHitDestroyTransform)
        {
            foreach (Connector connector in lastHitDestroyTransform.GetComponentsInChildren<Connector>())
            {
                connector.gameObject.SetActive(false);
                connector.updateConnectors(true);
            }

            Destroy(lastHitDestroyTransform.gameObject);

            destroyBuildingToggle(true);
            lastHitDestroyTransform = null;
        }
    }

    public void toggleBuildingUI(bool active)
    {
        isBuilding = false;

        buildingUI.SetActive(active);

        // Disable your cameras sensitivity.
        

        Cursor.visible = active;
        Cursor.lockState = active ? CursorLockMode.None : CursorLockMode.Locked;
    }

    public void destroyBuildingToggle(bool fromScript = false)
    {
        if (fromScript)
        {
            isDestroying = false;
            destroyText.text = "Destroy Off";
            destroyText.color = Color.green;
        }
        else
        {
            isDestroying = !isDestroying;
            destroyText.text = isDestroying ? "Destroy On" : "Destroy Off";
            destroyText.color = isDestroying ? Color.red : Color.green;
            toggleBuildingUI(false);
        }
    }

    public void changeBuildTypeButton(string selectedBuildType)
    {
        if (System.Enum.TryParse(selectedBuildType, out SelectedBuildType result))
        {
            currentBuildType = result;
        }
        else
        {
            Debug.Log("Build type doesnt exist");
        }
    }

    public void startBuildingButton(int buildIndex)
    {
        currentBuildingIndex = buildIndex;
        toggleBuildingUI(false);

        isBuilding = true;
    }
}

[System.Serializable]
public enum SelectedBuildType
{
    floor,
    wall,
}