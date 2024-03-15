/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2024.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

using Leap.Unity;
using Leap.Unity.Interaction;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using Leap.Unity.PhysicalHands;
using System.Linq;

namespace Leap.Unity.Examples
{
    /// <summary>
    /// PullCordHandle keeps track of the handle's position and moves it according to hand attraction and pinching.
    /// </summary>

    public class PullCordHandle : MonoBehaviour, IPhysicalHandHover, IPhysicalHandGrab
    {
        public enum PullCordState
        {
            Default,
            Hovered,
            Pinched,
            Inactive
        }

        private PullCordState _state;
        /// <summary>
        /// The current state of the pull cord (eg. Default, Hovered, Pinched or Inactive)
        /// </summary>
		public PullCordState State => _state;

        /// <summary>
        /// OnStateChanged is invoked when the pull cord handle state changes.
        /// </summary>
		public UnityEvent<PullCordState> OnStateChanged;
        /// <summary>
        /// OnPinchEnd is invoked when the pull cord handle was pinched, but has been let go off this frame.
        /// </summary>
		public UnityEvent OnPinchEnd;

        [SerializeField, Tooltip("Max hover distance of hand where the handle pos is still attracted to it")]
        private float _distanceToHandThreshold = 0.09f;
        [SerializeField, Tooltip("Distance that the handle can be pulled away from its resting position")]
        private float _stretchThreshold = 0.6f;


        private Vector3 _restingPos;
        /// <summary>
        /// The position that the handle is going to jump to if released
        /// </summary>
        [HideInInspector] public Vector3 RestingPos { get { return _restingPos; } set { _restingPos = value; } }

        private InteractionBehaviour _intBeh;
        private Vector3 _handlePositionTarget;

        private float _springSpeed = 100f;
        private Vector3 _vposition;

        private float _pinchActivateDistance = 0.03f;
        private float _pinchDeactivateDistance = 0.04f;
        private bool _isPinching = false;

        private bool _wasHovered = false;

        private List<Hand> _relevanthands = new List<Hand>();
        private Hand _mostRelevantHand = null;
        private bool _isHovered = false;
        private bool _isGrabbed = false;


        private void OnEnable()
        {
            _intBeh = GetComponent<InteractionBehaviour>();
            StopPinching();

            OnStateChanged.AddListener((x) => _state = x);
        }

        private void Update()
        {
            _handlePositionTarget = RestingPos;

            if ((_intBeh && _intBeh.isPrimaryHovered) || _relevanthands.Count() != 0)
            {
                Hand hand;
                if (_intBeh)
                {
                    hand = _intBeh.primaryHoveringHand;
                }
                else
                {
                    hand = _mostRelevantHand;
                }
                

                if (!_wasHovered)
                {
                    _wasHovered = true;
                    if (OnStateChanged != null) OnStateChanged.Invoke(PullCordState.Hovered);
                }
                Vector3 midpoint = Vector3.zero;

                midpoint = Midpoint(hand);
                float distance = Vector3.Distance(midpoint, RestingPos);
                UpdatePinching(hand, distance);

                if (_isPinching || distance < _distanceToHandThreshold)
                {
                    _handlePositionTarget = midpoint;
                }
            }
            else if (_isPinching)
            {
                StopPinching();
            }
            else if (_wasHovered)
            {
                _wasHovered = false;
                if (OnStateChanged != null) OnStateChanged.Invoke(PullCordState.Default);
            }
            
            if(!_isPinching)
                transform.position = SpringPosition(transform.position, _handlePositionTarget);
        }

        private Vector3 SpringPosition(Vector3 current, Vector3 target)
        {
            _vposition = Step(_vposition, Vector3.zero, _springSpeed * 0.35f);
            _vposition += (target - current) * (_springSpeed * 0.1f);
            return current + _vposition * Time.deltaTime;
        }

        private Vector3 Step(Vector3 current, Vector3 target, float omega)
        {
            var exp = Mathf.Exp(-omega * Time.deltaTime);
            return Vector3.Lerp(target, current, exp);
        }

        private Vector3 Midpoint(Leap.Hand hand)
        {
            return (hand.GetIndex().TipPosition + hand.GetThumb().TipPosition) / 2f;
        }

        private void UpdatePinching(Leap.Hand hand, float distanceToRestingPos)
        {
            float distance = Vector3.Distance(hand.GetIndex().TipPosition, hand.GetThumb().TipPosition);

            if (_isPinching && (distance > _pinchDeactivateDistance || distanceToRestingPos > _stretchThreshold))
            {
                StopPinching();
            }

            // only start pinching if the midpoint is close enough to the resting position
            else if (distanceToRestingPos < _distanceToHandThreshold)
            {
                if (!_isPinching && distance < _pinchActivateDistance)
                {
                    StartPinching();
                }
            }

        }

        private void StartPinching()
        {
            if (OnStateChanged != null) OnStateChanged.Invoke(PullCordState.Pinched);

            _isPinching = true;
        }

        private void StopPinching()
        {
            bool hovered;
            if (_intBeh)
            {
                hovered = _intBeh.isPrimaryHovered;
            }
            else
            {
                hovered = _mostRelevantHand != null;
            }
            if (OnStateChanged != null) OnStateChanged.Invoke(hovered ? PullCordState.Hovered : PullCordState.Default);

            if (OnPinchEnd != null) OnPinchEnd.Invoke();

            _isPinching = false;
        }

        void IPhysicalHandHover.OnHandHover(ContactHand hand)
        {
            if(!_relevanthands.Contains(hand.GetDataHand()))
            {
                _relevanthands.Add(hand.GetDataHand());
            }
            _mostRelevantHand = GetClosestHand();
        }

        void IPhysicalHandHover.OnHandHoverExit(ContactHand hand)
        {
            _relevanthands.Remove(hand.GetDataHand());
            _mostRelevantHand = GetClosestHand();
        }

        void IPhysicalHandGrab.OnHandGrab(ContactHand hand)
        {
            _mostRelevantHand = hand.GetDataHand();
        }

        void IPhysicalHandGrab.OnHandGrabExit(ContactHand hand)
        {
            _mostRelevantHand = GetClosestHand();
        }

        Hand GetClosestHand()
        {
            Hand closestHand = null;
            float closestDist = float.PositiveInfinity;
            if (_relevanthands.Count > 0)
            {
                foreach (var hand in _relevanthands)
                {
                    float distanceFromHand = Vector3.Distance(hand.PalmPosition, this.transform.position);
                    if (closestHand == null || distanceFromHand < closestDist)
                    {
                        closestHand = hand;
                        closestDist = distanceFromHand;
                    }
                }
                return closestHand;
            }
            else
            {
                return null;
            }
        }
    }
}