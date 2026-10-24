using UnityEngine;

public class TestScript : MonoBehaviour
{
    [SerializeField] private Transform _dummyObject;
    [SerializeField] private Transform _targetTransform;

    void Update()
    {
        _targetTransform.localRotation = _dummyObject.localRotation;
    }
}