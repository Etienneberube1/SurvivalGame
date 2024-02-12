using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System;
public class UIManager : Singleton<UIManager>
{
    [Header("Radial Menu")]
    [SerializeField] private float _radius = 300;
    [SerializeField] private List<GameObject> _uiBuildingObjects = new List<GameObject>();

    List<GameObject> _activeBuildingObject = new List<GameObject>();

    public void AddBuildingObjects(int index)
    { 
        GameObject buildingObject = Instantiate(_uiBuildingObjects[index], transform);

        _activeBuildingObject.Add(buildingObject);
    }

    public void OpenMenu()
    {
        for (int i = 0; i < _uiBuildingObjects.Count; i++)
        {
            AddBuildingObjects(i);
        }
        ArrangeUIObject();
    }
    public void CloseMenu()
    {
        foreach (GameObject go in _activeBuildingObject)
        {
            Destroy(go);
        }
        _activeBuildingObject.Clear();
    }


    private void ArrangeUIObject()
    { 
        float radiansSeparation = (Mathf.PI * 2) / _activeBuildingObject.Count;
        for (int i = 0; i < _activeBuildingObject.Count; i++)
        {
            float x = Mathf.Sin(radiansSeparation * i) * _radius;
            float y = Mathf.Cos(radiansSeparation * i) * _radius;

            _activeBuildingObject[i].GetComponent<RectTransform>().anchoredPosition = new Vector3(x, y, 0);
        }
    }
}

