using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using Unity.VisualScripting;

public class UIBuildingObjects : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private TextMeshProUGUI _buildingName;
    [SerializeField] private Image _buildingIcon;

    private BuildingManager _buildingManager;

    private Vector3 _originalScale;
    [SerializeField] private float _scaleFactor = 1.5f;
    [SerializeField] private float _scaleDuration = 0.5f;
    private bool _isMouseOver = false;

    [SerializeField] private string _buildType;
    [SerializeField] private int _buildIndex;

    private void Start()
    {
        _buildingManager = FindAnyObjectByType<BuildingManager>();
        _originalScale = transform.localScale;
    }

    private void Update()
    {
        if (_isMouseOver)
        {
            transform.localScale = Vector3.Lerp(transform.localScale, _originalScale * _scaleFactor, Time.deltaTime / _scaleDuration);
            UIManager.Instance.ChangeBuildInfo(_buildingName, _buildingIcon);

            if (Input.GetMouseButton(0))
            {
                BuildType();
                BuildingIndex();
            }
        }
        else if (!_isMouseOver)
        {
            transform.localScale = Vector3.Lerp(transform.localScale, _originalScale, Time.deltaTime / _scaleDuration);
        }
    }

    public string GetBuildingName()
    {
        return _buildingName.text;
    }
    public Image GetBuildingIcon()
    {
        return _buildingIcon;
    }


    public void OnPointerEnter(PointerEventData eventData)
    {
        _isMouseOver = true;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        _isMouseOver = false;
    }


    public void BuildType()
    {
        _buildingManager.changeBuildTypeButton(_buildType);
    } 
    public void BuildingIndex()
    {
        _buildingManager.startBuildingButton(_buildIndex);
    }
}
