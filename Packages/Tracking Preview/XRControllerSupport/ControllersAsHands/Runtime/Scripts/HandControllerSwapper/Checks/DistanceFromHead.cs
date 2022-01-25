/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2022.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

using System.Collections;
using System.Collections.Generic;
using Leap.Unity;
using UnityEngine;

namespace Leap.Unity.Controllers
{
    /// <summary>
    /// DistanceFromHead checks to see if the distance of the InputMethodType is less than or equal to the actionThreshold
    /// if lessThan is true, or greater than or equal to the actionThreshold if lessThan is false
    /// </summary>
    public class DistanceFromHead : InputCheckBase
    {
        public Vector3 CurrentXRControllerPosition;
        public bool LessThan = false;

        private float _distance = 0;

        protected override bool IsTrueLogic()
        {
            Vector3 inputPosition;
            if (GetPosition(out inputPosition))
            {
                _distance = Vector3.Distance(inputPosition, MainCameraProvider.mainCamera.transform.position);

                if (LessThan)
                {
                    return _distance <= ActionThreshold;
                }
                else
                {
                    return _distance >= ActionThreshold;
                }
            }
            return false;
        }

        private bool GetPosition(out Vector3 inputPosition)
        {
            switch (InputMethodType)
            {
                case InputMethodType.LeapHand:
                    if (_provider.Get(Hand) != null)
                    {
                        inputPosition = _provider.Get(Hand).PalmPosition.ToVector3();
                        return true;
                    }
                    break;
                case InputMethodType.XRController:
                    if (GetController())
                    {
#if ENABLE_INPUT_SYSTEM
                        inputPosition = _xrController.devicePosition.ReadValue();
#else
                        inputPosition = CurrentXRControllerPosition;
#endif
                        return true;
                    }
                    break;
            }

            inputPosition = LessThan ? Vector3.positiveInfinity : Vector3.zero;
            return false;
        }
    }
}