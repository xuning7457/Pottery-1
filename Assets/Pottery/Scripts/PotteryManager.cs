﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;

using Leap;
using System;

public class PotteryManager : MonoBehaviour
{
    public HandController leaphandController;
    public Lathe latheController;
    public int handMovementScaling;
    public int ClayResolution;
    public float ClayHeight, ClayRadius, ClayVariance;
    public float effectStrength, effectArea;

    [Header("Debug")]
    public LineRenderer lineRenderer;
    public GameObject fingerTipSphere;

    private Spline spline;
    private Controller m_leapController;

    enum GESTURE
    {
        PUSH,
        PULL,
        SMOOTH
    }

    // Use this for initialization
    void Start()
    {
        m_leapController = new Controller();
        // initiate the Spline 
        spline = new Spline(ClayRadius, ClayHeight, ClayResolution, ClayVariance);

        // generate initial clay-object
        latheController.init(spline.getSplineList());

    }

    // Update is called once per frame
    void Update()
    {
        Frame frame = m_leapController.Frame();

        //check if hands are found:
        if (frame.Hands.Count > 0)
        {
            // Check if Hand touches clay
            Vector3 tipPosition = frame.Hands[0].Fingers.FingerType(Finger.FingerType.TYPE_INDEX)[0].TipPosition.ToUnityScaled(false);
            tipPosition *= handMovementScaling;
            
            // [Debug] moves white sphere to tip Position
            fingerTipSphere.transform.position = tipPosition - new Vector3(0f,0.2f,0f);

            float splineDistToPoint = spline.DistanceToPoint(tipPosition);
            if (splineDistToPoint <= 0)
            {
                // get current gesture
                switch (getCurrentGesture(frame.Hands))
                {
                    case GESTURE.PUSH:
                        {
                            spline.PushAtPosition(tipPosition, splineDistToPoint, effectStrength, effectArea);
                        }
                        break;

                    case GESTURE.PULL:
                        {
                            spline.PullAtPosition(tipPosition, effectStrength, effectArea);
                        }
                        break;
                    case GESTURE.SMOOTH:
                        {
                            spline.SmoothAtPosition(tipPosition, effectStrength, effectArea);
                        }
                        break;
                    default:
                        break;
                }

                //get List of deformed Spline
                List<Vector3> currentSpline = spline.getSplineList();

                //generate new Mesh
                latheController.updateMesh(currentSpline);
            }
        }
    }

    /// <summary>
    /// Checks current Hand gesture
    /// </summary>
    /// <param name="hand"></param>
    /// <returns>current Gesture: Pull,Push or Smooth</returns>
    private GESTURE getCurrentGesture(HandList hands)
    {
        if(hands.Count > 1)
        {
            return GESTURE.SMOOTH;
        }
        //calculate pinch-angle
        Vector3 indexTipPosition = handMovementScaling * hands[0].Fingers.FingerType(Finger.FingerType.TYPE_INDEX)[0].TipPosition.ToUnityScaled(false);
        Vector3 thumbTipPosition = handMovementScaling * hands[0].Fingers.FingerType(Finger.FingerType.TYPE_THUMB)[0].TipPosition.ToUnityScaled(false);
        Vector3 palmPosition = handMovementScaling * hands[0].PalmPosition.ToUnityScaled(false);

        Vector3 v1 = palmPosition - indexTipPosition;
        Vector3 v2 = palmPosition - thumbTipPosition;
        v1.Normalize();
        v2.Normalize();
        float dotValue = Vector3.Dot(v1, v2);
        if (dotValue > 1.0f)
            dotValue = 1.0f;
        else if (dotValue < -1.0f)
            dotValue = -1.0f;
        //skalarprodukt
        //angle between thumb, indexfinger and Palm
        float angle = Mathf.Acos(dotValue / (v1.sqrMagnitude * v2.sqrMagnitude));

        //if angle is bigger than 1.1-> hand is pinching
        //1.1 is approximated value, possibly not best value for everyone
        if (angle > 1.1f) {
            return GESTURE.PULL;
        } else {
            return GESTURE.PUSH;
        }
        
    }
}
