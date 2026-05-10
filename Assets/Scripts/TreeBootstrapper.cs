using Index;
using Interaction;
using Model;
using UI;
using UnityEngine;

public class TreeBootstrapper : MonoBehaviour
{
    [SerializeField] private DataHandler _data;
    [SerializeField] private IndexHandler _index;
    [SerializeField] private InteractionsHandler _interactions;
    [SerializeField] private RenderingHandler _rendering;

    private void Awake()
    {
        _data.Initialize();
        _index.Initialize(_data);
        _interactions.Initialize(_index, _data);
        _rendering.Initialize(_interactions, _index, _interactions.GetValidator());
    }
}