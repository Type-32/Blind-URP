using System;
using System.Collections.Generic;
using UnityEngine;

public enum State
{
    none,
    walk,
    stay,
    attack
}

public class ai : MonoBehaviour
{

    public List<GameObject> WalkPath;

    private int walkIndex = -1;
    private GameObject walkTarget;

    private bool isAdd = true;
    
    private State currentState = State.none;

    private float walkSpeed = 1f;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        ChangeState(State.walk);
    }


    void ChangeState(State state)
    {
        currentState = state;
        switch(state)
        {
            case State.walk:
                Walk();
                break;
            case State.stay:
                break;
            case State.attack:
                AtackPlayer();
                break;
        }
    }

    

    void Walk()
    {
        FindNext();
    }

    void FindNext()
    {
        if (isAdd)
        {
            if (walkIndex == -1)
            {
                walkIndex = walkIndex + 1;
                walkTarget = WalkPath[walkIndex];
            }
            else if(walkIndex==WalkPath.Count-1)
            {
                isAdd = false;
                walkIndex = WalkPath.Count - 2;
                walkTarget = WalkPath[walkIndex];
            }
            else
            {
                walkIndex = walkIndex + 1;
                walkTarget = WalkPath[walkIndex];
            }
        }
        else
        {
            if (walkIndex == 0)
            {
                isAdd = true;
                walkIndex = walkIndex + 1;
                walkTarget = WalkPath[walkIndex];
            }
            else
            {
                walkIndex = walkIndex -1;
                walkTarget = WalkPath[walkIndex];
            }
          
        }

        
    }


    void AtackPlayer()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (currentState == State.walk)
        {
            if (Vector3.Distance(transform.position, walkTarget.transform.position) < 0.1f)
            {
                FindNext();
            }
            else
            {
                gameObject.transform.Translate((transform.position-walkTarget.transform.position).normalized*walkSpeed,Space.World);
            }
        }

       
    }

    private void OnCollisionEnter(Collision other)
    {
        if (other.gameObject.tag == "Player")
        {
            ChangeState(State.attack);
        }
    }
}
