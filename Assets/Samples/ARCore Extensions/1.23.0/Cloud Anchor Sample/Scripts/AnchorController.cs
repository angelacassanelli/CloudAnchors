//-----------------------------------------------------------------------
// <copyright file="AnchorController.cs" company="Google LLC">
//
// Copyright 2019 Google LLC
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//
// </copyright>
//-----------------------------------------------------------------------

namespace Google.XR.ARCoreExtensions.Samples.CloudAnchors
{
    using Google.XR.ARCoreExtensions;
    using UnityEngine;
    using UnityEngine.Networking;
    using UnityEngine.XR.ARFoundation;
    
    using Firebase;
    using Firebase.Database;
    using Firebase.Extensions;
    using Firebase.Analytics;
    
    using System;
    using System.Collections;
    using System.Collections.Generic;

    /// <summary>
    /// A Controller for the Anchor object that handles hosting and resolving the
    /// <see cref="ARCloudAnchor"/>.
    /// </summary>
#pragma warning disable 618
    public class AnchorController : NetworkBehaviour
#pragma warning restore 618
    {
        /// <summary>
        /// The customized timeout duration for resolving request to prevent retrying to resolve
        /// indefinitely.
        /// </summary>
        private const float _resolvingTimeout = 10.0f;

        /// <summary>
        /// The Cloud Anchor ID for the hosted anchor's <see cref="ARCloudAnchor"/>.
        /// This variable will be synchronized over all clients.
        /// </summary>
#pragma warning disable 618
        [SyncVar(hook = "OnChangeId")]
#pragma warning restore 618
        private string _clouAnchorId = string.Empty;
        
        private static string _hostedCloudAnchorId = string.Empty;

        /// <summary>
        /// Indicates whether this script is running in the Host.
        /// </summary>
        private bool _isHost = false;

        /// <summary>
        /// Indicates whether an attempt to resolve the Cloud Anchor should be made.
        /// </summary>
        private bool _shouldResolve = false;

        /// <summary>
        /// Indicates whether to chekc Cloud Anchor state and update the anchor.
        /// </summary>
        private bool _shouldUpdatePoint = false;

        /// <summary>
        /// Record the time since resolving started. If the timeout has passed, display
        /// additional instructions.
        /// </summary>
        private float _timeSinceStartResolving = 0.0f;

        /// <summary>
        /// Indicates whether passes the resolving timeout duration or the anchor has been
        /// successfully resolved.
        /// </summary>
        private bool _passedResolvingTimeout = false;

        /// <summary>
        /// The anchor mesh object.
        /// In order to avoid placing the Anchor on identity pose, the mesh object should
        /// be disabled by default and enabled after hosted or resolved.
        /// </summary>
        private GameObject _anchorMesh;

        /// <summary>
        /// The Cloud Anchor created locally which is used to moniter whether the
        /// hosting or resolving process finished.
        /// </summary>
        private ARCloudAnchor _cloudAnchor;

        /// <summary>
        /// The Cloud Anchors example controller.
        /// </summary>
        private CloudAnchorsExampleController _cloudAnchorsExampleController;

        /// <summary>
        /// The AR Anchor Manager in the scene, used to host or resolve a Cloud Anchor.
        /// </summary>
        private ARAnchorManager _anchorManager;

        
        /// <summary>
        /// The Unity Awake() method.
        /// </summary>
        public void Awake()
        {
            _cloudAnchorsExampleController =
                GameObject.Find("CloudAnchorsExampleController")
                .GetComponent<CloudAnchorsExampleController>();
            _anchorManager = _cloudAnchorsExampleController.AnchorManager;
            _anchorMesh = transform.Find("AnchorMesh").gameObject;
            _anchorMesh.SetActive(false);
        }

        
        /// <summary>
        /// The Unity OnStartClient() method.
        /// </summary>
        public override void OnStartClient()
        {
            if (_clouAnchorId != string.Empty)
            {
                _shouldResolve = true;
            }
        }

        
        /// <summary>
        /// The Unity Update() method.
        /// </summary>
        public void Update()
        {
            if (_isHost)
            {
                if (_shouldUpdatePoint)
                {
                    UpdateHostedCloudAnchor();
                }
            }
            else
            {
                if (_shouldResolve)
                {
                    setHostedCloudAnchorId();
                    
                    if (!_cloudAnchorsExampleController.IsResolvingPrepareTimePassed())
                    {
                        return;
                    }

                    if (!_passedResolvingTimeout)
                    {
                        _timeSinceStartResolving += Time.deltaTime;

                        if (_timeSinceStartResolving > _resolvingTimeout)
                        {
                            _passedResolvingTimeout = true;
                            _cloudAnchorsExampleController.OnResolvingTimeoutPassed();
                        }
                    }

                    //if (!string.IsNullOrEmpty("_hostedCloudAnchorId") && _cloudAnchor == null)
                    if(1==1)
                    {
                        Debug.Log("Ciao sono nell'update e l'id è " + _hostedCloudAnchorId);
                        ResolveCloudAnchorId(_hostedCloudAnchorId);
                    }
                }

                if (_shouldUpdatePoint)
                {
                    UpdateResolvedCloudAnchor();
                }
            }
        }

        
        /// <summary>
        /// Command run on the server to set the Cloud Anchor Id.
        /// </summary>
        /// <param name="cloudAnchorId">The new Cloud Anchor Id.</param>
#pragma warning disable 618
        [Command]
#pragma warning restore 618
        public void CmdSetCloudAnchorId(string cloudAnchorId)
        {
            Debug.Log("Update Cloud Anchor Id with: " + cloudAnchorId);
            _clouAnchorId = cloudAnchorId;
        }

        
        /// <summary>
        /// Hosts the user placed cloud anchor and associates the resulting Id with this object.
        /// </summary>
        /// <param name="anchor">The last placed anchor.</param>
        public void HostAnchor(ARAnchor anchor)
        {
            _isHost = true;
            _shouldResolve = false;
            transform.SetParent(anchor.transform);
            _anchorMesh.SetActive(true);

            _cloudAnchor = _anchorManager.HostCloudAnchor(anchor);

            if (_cloudAnchor == null)
            {
                Debug.LogError("Failed to add Cloud Anchor.");
                _cloudAnchorsExampleController.OnAnchorHosted(
                    false, "Failed to add Cloud Anchor.");
                _shouldUpdatePoint = false;
            }
            else
            {
                _shouldUpdatePoint = true;
            }
        }
        
            
        // A realtime database transaction receives MutableData which can be modified
        // and returns a TransactionResult which is either TransactionResult.Success(data) with
        // modified data or TransactionResult.Abort() which stops the transaction with no changes.
        TransactionResult AddAnchorTransaction(MutableData mutableData) {
            
            NetworkManagerUIController _networkManagerUIController = new NetworkManagerUIController();            
            string _currentRoomNumber = _networkManagerUIController.getCurrentRoomNumber();           
            
            List<object> anchors = mutableData.Value as List<object>;

            if (anchors == null) {
                anchors = new List<object>();
            } 
                
            Dictionary<string, string> newAnchorMap = new Dictionary<string, string>();

            // Now we add a new entry that contains the room number address and the cloud anchor Id.
            newAnchorMap["Room Number"] = _currentRoomNumber;
            newAnchorMap["Anchor Id"] = _clouAnchorId;                                        
                                                                    
            anchors.Add(newAnchorMap);

            // You must set the Value to indicate data at that location has changed.
            mutableData.Value = anchors;
            return TransactionResult.Success(mutableData);
        }

        
        public void AddAnchorToDB() {
            if (string.IsNullOrEmpty("_clouAnchorId")) {
                Debug.Log("invalid id.");
                return;
            }
            Debug.Log(String.Format("Attempting to add id {0}",
                _clouAnchorId));            
            
            DatabaseReference reference = FirebaseDatabase.DefaultInstance.GetReference("anchors");

            Debug.Log("Adding anchor...");
            // Use a transaction to ensure that we do not encounter issues with
            // simultaneous updates that otherwise might create more than MaxScores top scores.
            reference.RunTransaction(AddAnchorTransaction)
                .ContinueWithOnMainThread(task => {
                    if (task.Exception != null) {
                        Debug.Log(task.Exception.ToString());
                    } else if (task.IsCompleted) {
                        Debug.Log("Transaction complete.");
                    }
                });
        }
        
        
        private void setHostedCloudAnchorId()
        {
            Debug.Log(String.Format("Attempting to retrieve cloud anchor Id..."));

            DatabaseReference reference = FirebaseDatabase.DefaultInstance.GetReference("anchors");
            
            reference.GetValueAsync().ContinueWith(task => {
                if (task.IsFaulted)
                {
                    Debug.Log("Task is faulted");
                    return;
                }
                else if (task.IsCompleted) {
                    Debug.Log("Task is completed");
                    DataSnapshot snapshot = task.Result;
                    Debug.Log("Entries number = " + snapshot.ChildrenCount);
                    
                    // Do something with snapshot...                    
                    
                    NetworkManagerUIController _networkManagerUIController = new NetworkManagerUIController();            
                    string _currentRoomNumber = _networkManagerUIController.getCurrentRoomNumber();

                    int i = 0;
                    foreach (DataSnapshot anchor_snapshot in snapshot.Children)
                    {
                        Debug.Log("foreach - iterazione numero " + i++);
                        IDictionary anchormap = (IDictionary)anchor_snapshot.Value;
                        Debug.Log("anchor map = " + anchormap);
                        Debug.Log("ROOM NUMBER = " + anchormap["Room Number"]);
                        Debug.Log("ANCHOR ID = " + anchormap["Anchor Id"]);
                        if (anchormap["Room Number"] == _currentRoomNumber)
                        {
                            Debug.Log("ora sono nell'if del foreach");
                            _hostedCloudAnchorId = anchormap["Anchor Id"].ToString();
                            return;
                        }
                        else
                        {
                            Debug.Log("ora sono nell'else del foreach");
                        }
                        Debug.Log("No current room number found.");
                    }
                }
            });
        }
        
                
        /// <summary>
        /// Resolves the Cloud Anchor Id and instantiate a Cloud Anchor on it.
        /// </summary>       
        // <param name="cloudAnchorId">The Cloud Anchor Id to be resolved.</param>
        private void ResolveCloudAnchorId(string _hostedCloudAnchorId)
        {            
            _cloudAnchorsExampleController.OnAnchorInstantiated(false);
            Debug.Log("Resolving HostedCloudAnchorId:" + _hostedCloudAnchorId);
            _cloudAnchor = _anchorManager.ResolveCloudAnchorId(_hostedCloudAnchorId);
            if (_cloudAnchor == null)
            {
                Debug.LogErrorFormat("Client could not resolve Cloud Anchor {0}.", _hostedCloudAnchorId);
                _cloudAnchorsExampleController.OnAnchorResolved(
                    false, "Client could not resolve Cloud Anchor.");
                _shouldResolve = true;
                _shouldUpdatePoint = false;
            }
            else
            {
                _shouldResolve = false;
                _shouldUpdatePoint = true;
            }
        }

        
        /// <summary>
        /// Update the anchor if hosting Cloud Anchor is success.
        /// </summary>
        private void UpdateHostedCloudAnchor()
        {
            if (_cloudAnchor == null)
            {
                Debug.LogError("No Cloud Anchor.");
                return;
            }

            CloudAnchorState cloudAnchorState = _cloudAnchor.cloudAnchorState;
            if (cloudAnchorState == CloudAnchorState.Success)
            {
                CmdSetCloudAnchorId(_cloudAnchor.cloudAnchorId);

                AddAnchorToDB();
                
                _cloudAnchorsExampleController.OnAnchorHosted(
                    true, "Successfully hosted Cloud Anchor.");
                _shouldUpdatePoint = false;
            }
            else if (cloudAnchorState != CloudAnchorState.TaskInProgress)
            {
                _cloudAnchorsExampleController.OnAnchorHosted(false,
                    "Fail to host Cloud Anchor with state: " + cloudAnchorState);
                _shouldUpdatePoint = false;
            }
        }

        
        /// <summary>
        /// Update the anchor if resolving Cloud Anchor is success.
        /// </summary>
        private void UpdateResolvedCloudAnchor()
        {
            if (_cloudAnchor == null)
            {
                Debug.LogError("No Cloud Anchor.");
                return;
            }

            CloudAnchorState cloudAnchorState = _cloudAnchor.cloudAnchorState;
            if (cloudAnchorState == CloudAnchorState.Success)
            {
                transform.SetParent(_cloudAnchor.transform, false);
                _cloudAnchorsExampleController.OnAnchorResolved(
                    true,
                    "Successfully resolved Cloud Anchor.");
                _cloudAnchorsExampleController.WorldOrigin = transform;
                _anchorMesh.SetActive(true);

                // Mark resolving timeout passed so it won't fire OnResolvingTimeoutPassed event.
                _passedResolvingTimeout = true;
                _shouldUpdatePoint = false;
            }
            else if (cloudAnchorState != CloudAnchorState.TaskInProgress)
            {
                _cloudAnchorsExampleController.OnAnchorResolved(
                    false, "Fail to resolve Cloud Anchor with state: " + cloudAnchorState);
                _shouldUpdatePoint = false;
            }
        }
        
        
        /// <summary>
        /// Callback invoked once the Cloud Anchor Id changes.
        /// </summary>
        /// <param name="newId">New Cloud Anchor Id.</param>
        private void OnChangeId(string newId)
        {
            if (!_isHost && newId != string.Empty)
            {
                _clouAnchorId = newId;
                Debug.Log("Update id with " + _clouAnchorId);
                _shouldResolve = true;
                _cloudAnchor = null;
            }
        }
    }
}
