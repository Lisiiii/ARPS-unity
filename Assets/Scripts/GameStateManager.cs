using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using radar.state;

public class GameStateManager : MonoBehaviour
{
    public Team enemyTeam;
    private StateManager _stateManager;
    // Start is called before the first frame update
    void Start()
    {
        _stateManager = StateManager.Instance();
        _stateManager._enemyTeam = enemyTeam;
    }

    // Update is called once per frame
    void Update()
    {
        _stateManager.update();
    }
}
