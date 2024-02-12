using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
public class UIBuildingObjects : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI _buildingName;
    [SerializeField] private Image _buildingIcon;

    private BuildingManager _buildingManager;

    private void Start()
    {
        _buildingManager = FindAnyObjectByType<BuildingManager>();
    }
    public string GetBuildingName()
    {
        return _buildingName.text;
    }
    public Image GetBuildingIcon()
    {
        return _buildingIcon;
    }

    public void BuildType(string buildtype)
    {
        _buildingManager.changeBuildTypeButton(buildtype);
    } 
    public void BuildingIndex(int index)
    {
        _buildingManager.startBuildingButton(index);
    }
}
