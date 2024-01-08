using BepInEx.Logging;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace RemoteOpenDoor.Plugins
{


    [HarmonyPatch(typeof(RemoteProp))]
    internal class RemotePropPatch
    {
        public static ManualLogSource Logger;

        public const string PLUGIN_ID = "example.RemoteOpenDoor";
        public const string PLUGIN_NAME = "Remote Open Door";
        public const string PLUGIN_VERSION = "1.0.0";
        public const string PLUGIN_GUID = "com.Giddy.RemoteOpenDoor";

        // Add the MonoBehaviour if you need to perform operations that require a MonoBehaviour context.
        public class RemotePropPatchMB : MonoBehaviour
        {
        }

        private static RemotePropPatchMB remotePropPatchMB;

        // Threshold distance within which the remote can trigger the door
        public static float doorActivationDistance = 15f;

        [HarmonyPatch(typeof(RemoteProp), "ItemActivate")]
        [HarmonyPrefix]
        public static bool ItemActivatePatch(ref RemoteProp __instance, ref AudioSource ___remoteAudio, bool used, bool buttonDown = true)
        {
            if ((UnityEngine.Object)(object)remotePropPatchMB == (UnityEngine.Object)null)
            {
                GameObject gameObject = new GameObject("RemotePropMB");
                remotePropPatchMB = gameObject.AddComponent<RemotePropPatchMB>();
            }

            Transform playerTransform = ((Component)__instance).transform;
            AudioSource audioSource = ___remoteAudio;
            if (audioSource != null)
            {
                audioSource.PlayOneShot(___remoteAudio.clip);
            }
            else
            {
                Logger?.LogWarning("AudioSource is null in RemoteProp");
            }

            // Find and interact with doors
            TerminalAccessibleObject[] doors = UnityEngine.Object.FindObjectsOfType<TerminalAccessibleObject>();
            if (doors.Length == 0)
            {
                Logger?.LogInfo("No doors found in the scene to interact with.");
            }

            foreach (var door in doors)
            {
                float distanceToDoor = Vector3.Distance(playerTransform.position, door.transform.position);
                if (distanceToDoor <= doorActivationDistance)
                {
                    Logger?.LogInfo($"Activating door {door.gameObject.name} at distance {distanceToDoor}");
                    door.SetDoorToggleLocalClient();
                }
                else
                {
                    Logger?.LogInfo($"Door {door.gameObject.name} is too far to activate (Distance: {distanceToDoor})");
                }
            }

            return false; // To prevent the original method from executing
        }
    }
}
